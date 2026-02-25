using FaiaChat.Api.Models;
using FaiaChat.Api.Services;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<SystemPromptBuilder>();

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

app.MapPost("/api/chat", async (ChatRequest request, IChatCompletionService chatService, SystemPromptBuilder promptBuilder, HttpContext context) =>
{
    // 1. Validate
    if (request.Messages is null || request.Messages.Count == 0)
        return Results.BadRequest(new { error = "Messages required" });

    var userMessageCount = request.Messages.Count(m => m.Role == "user");
    if (userMessageCount > 20)
        return Results.BadRequest(new { error = "Message limit exceeded" });

    // 2. Build system prompt
    var systemPrompt = promptBuilder.Build();

    // 3. Build ChatHistory
    var chatHistory = new ChatHistory();
    chatHistory.AddSystemMessage(systemPrompt);

    foreach (var msg in request.Messages)
    {
        if (msg.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
            chatHistory.AddAssistantMessage(msg.Content);
        else
            chatHistory.AddUserMessage(msg.Content);
    }

    // 4. Set up SSE response
    context.Response.ContentType = "text/event-stream";
    context.Response.Headers.CacheControl = "no-cache";
    context.Response.Headers.Connection = "keep-alive";

    // 5. Stream response via Semantic Kernel
    try
    {
        await foreach (var chunk in chatService.GetStreamingChatMessageContentsAsync(chatHistory, cancellationToken: context.RequestAborted))
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
        // Client disconnected
        return Results.Empty;
    }

    // 6. Send [DONE] marker
    await context.Response.WriteAsync("data: [DONE]\n\n");
    await context.Response.Body.FlushAsync();

    return Results.Empty;
}).RequireRateLimiting("chat");

app.Run();
