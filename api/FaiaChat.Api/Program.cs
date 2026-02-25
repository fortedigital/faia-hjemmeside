using FaiaChat.Api.Models;
using FaiaChat.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<NotionConfig>(builder.Configuration.GetSection("Notion"));
builder.Services.AddHttpClient("notion");
builder.Services.AddSingleton<NotionContentService>();
builder.Services.AddSingleton<SystemPromptBuilder>();

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

app.Run();
