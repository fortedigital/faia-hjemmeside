using FaiaChat.Api.Models;
using FaiaChat.Api.Services;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Threading.RateLimiting;
using System.ClientModel;

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

var allowedOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
    ?? new[] { "http://localhost:5173", "http://localhost:5174", "http://localhost:5175" };

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .WithMethods("POST", "GET")
              .WithHeaders("Content-Type");
    });
});

var isDev = builder.Environment.IsDevelopment();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.GlobalLimiter = PartitionedRateLimiter.CreateChained(
        ChatRateLimiter(isDev ? 60 : 5, TimeSpan.FromMinutes(1)),
        ChatRateLimiter(isDev ? 600 : 50, TimeSpan.FromHours(1)),
        ChatRateLimiter(isDev ? 5000 : 200, TimeSpan.FromDays(1)));
});

static PartitionedRateLimiter<HttpContext> ChatRateLimiter(int permitLimit, TimeSpan window) =>
    PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        if (!context.Request.Path.StartsWithSegments("/api/chat"))
            return RateLimitPartition.GetNoLimiter("");
        return RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = window,
                QueueLimit = 0
            });
    });

var app = builder.Build();

app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self'; connect-src 'self'";
    if (!app.Environment.IsDevelopment())
        headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
    await next();
});

app.UseCors();
app.UseRateLimiter();

app.MapGet("/health", () => "OK");

app.MapPost("/api/chat", async (ChatRequest request, IChatCompletionService chatService, SystemPromptBuilder promptBuilder, HttpContext context) =>
{
    // 1. Validate
    if (request.Messages is null || request.Messages.Count == 0)
        return Results.BadRequest(new { error = "Messages required" });

    if (request.Messages.Any(m => !m.Role.Equals("user", StringComparison.OrdinalIgnoreCase)
                                  && !m.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase)))
        return Results.BadRequest(new { error = "Invalid role. Allowed: user, assistant" });

    if (request.Messages.Count > 40)
        return Results.BadRequest(new { error = "Message limit exceeded" });

    if (request.Messages.Count % 2 == 0)
        return Results.BadRequest(new { error = "Last message must be from user" });

    const int maxMessageLength = 2000;
    if (request.Messages.Any(m => m.Content.Length > maxMessageLength))
        return Results.BadRequest(new { error = $"Message too long (max {maxMessageLength} characters)" });

    // 2. Build system prompt
    var systemPrompt = await promptBuilder.BuildAsync();

    // 3. Build ChatHistory — enforce roles server-side, ignore client role field
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

    // 4. Execution settings
    var executionSettings = new PromptExecutionSettings
    {
        ExtensionData = new Dictionary<string, object>
        {
            ["temperature"] = 0.7,
            ["max_tokens"] = 400,
            ["top_p"] = 0.9
        }
    };

    // 5. Set up SSE response
    context.Response.ContentType = "text/event-stream";
    context.Response.Headers.CacheControl = "no-cache";
    context.Response.Headers.Connection = "keep-alive";

    // 6. Stream response via Semantic Kernel
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
    catch (Exception ex) when (ex.InnerException is ClientResultException
        || ex is ClientResultException
        || ex.Message.Contains("content_filter", StringComparison.OrdinalIgnoreCase))
    {
        // Azure OpenAI content filter rejection
        var fallback = "Beklager, jeg kan ikke svare på det. Kan jeg hjelpe deg med noe annet?";
        fullResponse.Clear();
        fullResponse.Append(fallback);
        await context.Response.WriteAsync($"data: {fallback}\n\n");
        await context.Response.Body.FlushAsync();
    }

    // 7. Send [DONE] marker
    await context.Response.WriteAsync("data: [DONE]\n\n");
    await context.Response.Body.FlushAsync();

    return Results.Empty;
});

app.Run();
