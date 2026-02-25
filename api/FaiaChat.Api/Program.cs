using FaiaChat.Api.Models;
using FaiaChat.Api.Services;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Threading.RateLimiting;
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
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddLangfuseExporter(builder.Configuration.GetSection("Langfuse"));
    });
builder.Services.AddLangfuseTracing();
builder.Services.AddLangfuse(builder.Configuration);

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
                PermitLimit = 5,
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

    var userMessageCount = request.Messages.Count(m => m.Role == "user");
    if (userMessageCount > 20)
        return Results.BadRequest(new { error = "Message limit exceeded" });

    // 2. Start Langfuse trace
    langfuseTrace.StartTrace("faia-chat",
        sessionId: request.SessionId ?? Guid.NewGuid().ToString(),
        input: new { userMessageCount });

    // 3. Build system prompt
    var systemPrompt = await promptBuilder.BuildAsync();

    // 4. Build ChatHistory
    var chatHistory = new ChatHistory();
    chatHistory.AddSystemMessage(systemPrompt);

    foreach (var msg in request.Messages)
    {
        if (msg.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
            chatHistory.AddAssistantMessage(msg.Content);
        else
            chatHistory.AddUserMessage(msg.Content);
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

    // 9. Complete Langfuse generation with output
    generation.SetResponse(new GenAiResponse
    {
        Model = deploymentName,
        Completion = fullResponse.ToString(),
        FinishReasons = ["stop"]
    });

    langfuseTrace.SetOutput(new { response = fullResponse.ToString() });

    // 10. Send [DONE] marker
    await context.Response.WriteAsync("data: [DONE]\n\n");
    await context.Response.Body.FlushAsync();

    return Results.Empty;
}).RequireRateLimiting("chat");

app.Run();
