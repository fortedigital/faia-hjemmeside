using Anthropic.SDK;
using FaiaChat.Api.Models;
using FaiaChat.Api.Services;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<NotionConfig>(builder.Configuration.GetSection("Notion"));
builder.Services.AddHttpClient("notion");
builder.Services.AddSingleton<NotionContentService>();
builder.Services.AddSingleton<SystemPromptBuilder>();

// Register Anthropic's IChatClient via Semantic Kernel
builder.Services.AddSingleton<IChatCompletionService>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var apiKey = config["Anthropic:ApiKey"]
        ?? throw new InvalidOperationException("Anthropic:ApiKey is not configured");
    var anthropicClient = new AnthropicClient(new APIAuthentication(apiKey));
    IChatClient chatClient = anthropicClient.Messages;
    return chatClient.AsChatCompletionService();
});

var allowedOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
    ?? new[] { "http://localhost:5173" };

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

app.MapPost("/api/chat", async (ChatRequest request, SystemPromptBuilder promptBuilder, IChatCompletionService chatService, HttpContext context) =>
{
    // 1. Validate
    if (request.Messages is null || request.Messages.Count == 0)
        return Results.BadRequest(new { error = "Messages required" });

    var userMessageCount = request.Messages.Count(m => m.Role == "user");
    if (userMessageCount > 20)
        return Results.BadRequest(new { error = "Message limit exceeded" });

    // 2. Build system prompt
    var systemPrompt = await promptBuilder.BuildAsync();

    // 3. Build ChatHistory with system prompt and conversation messages
    var chatHistory = new ChatHistory();
    chatHistory.AddSystemMessage(systemPrompt);

    foreach (var msg in request.Messages)
    {
        if (msg.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
            chatHistory.AddAssistantMessage(msg.Content);
        else
            chatHistory.AddUserMessage(msg.Content);
    }

    // 4. Configure execution settings
    var executionSettings = new PromptExecutionSettings
    {
        ModelId = "claude-sonnet-4-20250514",
        ExtensionData = new Dictionary<string, object>
        {
            ["max_tokens"] = 1024
        }
    };

    // 5. Set up SSE response
    context.Response.ContentType = "text/event-stream";
    context.Response.Headers.CacheControl = "no-cache";
    context.Response.Headers.Connection = "keep-alive";

    // 6. Stream response from Claude via Semantic Kernel
    try
    {
        await foreach (var chunk in chatService.GetStreamingChatMessageContentsAsync(chatHistory, executionSettings, cancellationToken: context.RequestAborted))
        {
            if (chunk.Content is { } text && text.Length > 0)
            {
                var sseText = text.Replace("\n", "\ndata: ");
                await context.Response.WriteAsync($"data: {sseText}\n\n", context.RequestAborted);
                await context.Response.Body.FlushAsync(context.RequestAborted);
            }
        }
    }
    catch (OperationCanceledException)
    {
        // Client disconnected — this is expected
        return Results.Empty;
    }

    // 7. Send [DONE] marker
    await context.Response.WriteAsync("data: [DONE]\n\n");
    await context.Response.Body.FlushAsync();

    return Results.Empty;
}).RequireRateLimiting("chat");

app.Run();
