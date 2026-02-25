using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using FaiaChat.Api.Models;
using FaiaChat.Api.Services;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<NotionConfig>(builder.Configuration.GetSection("Notion"));
builder.Services.AddHttpClient("notion");
builder.Services.AddSingleton<NotionContentService>();
builder.Services.AddSingleton<SystemPromptBuilder>();

builder.Services.AddSingleton<AnthropicClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var apiKey = config["Anthropic:ApiKey"]
        ?? throw new InvalidOperationException("Anthropic:ApiKey is not configured");
    return new AnthropicClient(new APIAuthentication(apiKey));
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
    options.AddFixedWindowLimiter("chat", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });
    options.RejectionStatusCode = 429;
});

var app = builder.Build();
app.UseCors();
app.UseRateLimiter();

app.MapGet("/health", () => "OK");

app.MapPost("/api/chat", async (ChatRequest request, SystemPromptBuilder promptBuilder, AnthropicClient anthropic, HttpContext context) =>
{
    // 1. Validate
    if (request.Messages is null || request.Messages.Count == 0)
        return Results.BadRequest(new { error = "Messages required" });

    var userMessageCount = request.Messages.Count(m => m.Role == "user");
    if (userMessageCount > 20)
        return Results.BadRequest(new { error = "Message limit exceeded" });

    // 2. Build system prompt
    var systemPrompt = await promptBuilder.BuildAsync();

    // 3. Convert ChatMessages to Anthropic SDK Messages
    var messages = request.Messages.Select(m =>
    {
        var role = m.Role.ToLowerInvariant() == "assistant"
            ? RoleType.Assistant
            : RoleType.User;
        return new Message(role, m.Content);
    }).ToList();

    // 4. Build parameters
    var parameters = new MessageParameters
    {
        Model = "claude-sonnet-4-20250514",
        MaxTokens = 1024,
        Stream = true,
        System = new List<SystemMessage> { new SystemMessage(systemPrompt) },
        Messages = messages
    };

    // 5. Set up SSE response
    context.Response.ContentType = "text/event-stream";
    context.Response.Headers.CacheControl = "no-cache";
    context.Response.Headers.Connection = "keep-alive";

    // 6. Stream response from Claude
    try
    {
        await foreach (var messageResponse in anthropic.Messages.StreamClaudeMessageAsync(parameters, context.RequestAborted))
        {
            if (messageResponse.Delta?.Text is { } text)
            {
                await context.Response.WriteAsync($"data: {text}\n\n", context.RequestAborted);
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
