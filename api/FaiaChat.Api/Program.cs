using FaiaChat.Api.Models;
using FaiaChat.Api.Services;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Threading.RateLimiting;
using System.ClientModel;
using zborek.Langfuse;
using zborek.Langfuse.OpenTelemetry;
using zborek.Langfuse.OpenTelemetry.Models;
using zborek.Langfuse.OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<SystemPromptBuilder>();

// Register Azure OpenAI via Semantic Kernel
var azureOpenAI = builder.Configuration.GetSection("AzureOpenAI");
var endpoint = azureOpenAI["Endpoint"]
    ?? throw new InvalidOperationException("AzureOpenAI:Endpoint is not configured");
var apiKey = azureOpenAI["ApiKey"]
    ?? throw new InvalidOperationException("AzureOpenAI:ApiKey is not configured");
var deploymentName = azureOpenAI["DeploymentName"]
    ?? throw new InvalidOperationException("AzureOpenAI:DeploymentName is not configured");

builder.Services.AddAzureOpenAIChatCompletion(deploymentName, endpoint, apiKey);

// Register Langfuse tracing
var langfuseSection = builder.Configuration.GetSection("Langfuse");
var langfusePublicKey = langfuseSection["PublicKey"] ?? "";
var langfuseSecretKey = langfuseSection["SecretKey"] ?? "";
var langfuseEnabled = !string.IsNullOrEmpty(langfusePublicKey) && !string.IsNullOrEmpty(langfuseSecretKey);

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddSource("Langfuse");
        if (langfuseEnabled)
        {
            tracing.AddLangfuseExporter(options =>
            {
                options.PublicKey = langfusePublicKey;
                options.SecretKey = langfuseSecretKey;
                options.Url = langfuseSection["Url"] ?? "http://localhost:3000";
                options.OnlyGenAiActivities = false;
            });
        }
    });
builder.Services.AddLangfuseTracing();
builder.Services.AddLangfuse(builder.Configuration);
LangfuseOtlpExtensions.UseLangfuseActivityListener();

var allowedOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
    ?? new[] { "http://localhost:5173", "http://localhost:5174", "http://localhost:5175" };

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("chat", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Environment.IsDevelopment() ? 60 : 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    options.RejectionStatusCode = 429;
});

var app = builder.Build();
app.UseCors();
app.UseRateLimiter();

app.MapGet("/health", () => "OK");

app.MapPost("/api/chat", async (ChatRequest request, IChatCompletionService chatService, SystemPromptBuilder promptBuilder, IOtelLangfuseTrace langfuseTrace, HttpContext context) =>
{
    // 1. Validate
    if (request.Messages is null || request.Messages.Count == 0)
        return Results.BadRequest(new { error = "Messages required" });

    if (request.Messages.Count > 40)
        return Results.BadRequest(new { error = "Message limit exceeded" });

    if (request.Messages.Count % 2 == 0)
        return Results.BadRequest(new { error = "Last message must be from user" });

    const int maxMessageLength = 2000;
    if (request.Messages.Any(m => m.Content.Length > maxMessageLength))
        return Results.BadRequest(new { error = $"Message too long (max {maxMessageLength} characters)" });

    // 2. Start Langfuse trace
    langfuseTrace.StartTrace("faia-chat",
        sessionId: request.SessionId ?? Guid.NewGuid().ToString(),
        input: new { messageCount = request.Messages.Count });

    // 3. Build system prompt
    var systemPrompt = await promptBuilder.BuildAsync();

    // 4. Build ChatHistory — enforce roles server-side, ignore client role field
    var chatHistory = new ChatHistory();
    chatHistory.AddSystemMessage(systemPrompt);

    for (var i = 0; i < request.Messages.Count; i++)
    {
        var content = request.Messages[i].Content;
        if (i % 2 == 0)
            chatHistory.AddUserMessage(content);
        else
            chatHistory.AddAssistantMessage(content);
    }

    // 5. Execution settings
    var executionSettings = new PromptExecutionSettings
    {
        ExtensionData = new Dictionary<string, object>
        {
            ["temperature"] = 0.7,
            ["max_tokens"] = 400,
            ["top_p"] = 0.9
        }
    };

    // 6. Create Langfuse generation
    using var generation = langfuseTrace.CreateGeneration("chat-completion",
        model: deploymentName,
        provider: "azure-openai",
        input: new { messages = request.Messages });

    // 7. Set up SSE response
    context.Response.ContentType = "text/event-stream";
    context.Response.Headers.CacheControl = "no-cache";
    context.Response.Headers.Connection = "keep-alive";

    // 8. Stream response via Semantic Kernel
    var fullResponse = new System.Text.StringBuilder();
    var finishReason = "stop";
    try
    {
        await foreach (var chunk in chatService.GetStreamingChatMessageContentsAsync(chatHistory, executionSettings, kernel: null, context.RequestAborted))
        {
            if (chunk.Content is { } text && text.Length > 0)
            {
                fullResponse.Append(text);
                var sseText = text.Replace("\n", "\ndata: ");
                await context.Response.WriteAsync($"data: {sseText}\n\n", context.RequestAborted);
                await context.Response.Body.FlushAsync(context.RequestAborted);
            }
        }
    }
    catch (OperationCanceledException)
    {
        return Results.Empty;
    }
    catch (Exception ex) when (ex.InnerException is ClientResultException
        || ex is ClientResultException
        || ex.Message.Contains("content_filter", StringComparison.OrdinalIgnoreCase))
    {
        // Azure OpenAI content filter rejection
        finishReason = "content_filter";
        var fallback = "Beklager, jeg kan ikke svare på det. Kan jeg hjelpe deg med noe annet?";
        fullResponse.Clear();
        fullResponse.Append(fallback);
        await context.Response.WriteAsync($"data: {fallback}\n\n");
        await context.Response.Body.FlushAsync();
    }

    // 9. Complete Langfuse generation with output
    generation.SetResponse(new GenAiResponse
    {
        Model = deploymentName,
        Completion = fullResponse.ToString(),
        FinishReasons = [finishReason]
    });

    langfuseTrace.SetOutput(new { response = fullResponse.ToString() });

    // 10. Send [DONE] marker
    await context.Response.WriteAsync("data: [DONE]\n\n");
    await context.Response.Body.FlushAsync();

    return Results.Empty;
}).RequireRateLimiting("chat");

app.Run();
