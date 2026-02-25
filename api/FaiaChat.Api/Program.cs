using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using FaiaChat.Api.Models;
using FaiaChat.Api.Services;

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

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173") // Vite dev server
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();
app.UseCors();

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
    }

    // 7. Send [DONE] marker
    await context.Response.WriteAsync("data: [DONE]\n\n");
    await context.Response.Body.FlushAsync();

    return Results.Empty;
});

app.Run();
