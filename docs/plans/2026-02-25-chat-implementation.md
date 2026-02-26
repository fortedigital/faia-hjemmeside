# AI Accelerator Chat — Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace the mocked keyword-matching chat with a Claude-powered LLM chat backed by Notion content, using ASP.NET Core + Semantic Kernel as backend and upgrading the existing React frontend.

**Architecture:** React frontend sends conversation history to a .NET API endpoint. The API uses Semantic Kernel to orchestrate Claude, injecting cached Notion content as system prompt context. Responses are streamed back to the client.

**Tech Stack:** React 19 (Vite), ASP.NET Core 8 Minimal API, Semantic Kernel, Anthropic Claude API, Notion API, SCSS Modules

---

## Task 1: Scaffold .NET API project

**Files:**
- Create: `api/FaiaChat.Api/FaiaChat.Api.csproj`
- Create: `api/FaiaChat.Api/Program.cs`
- Create: `api/FaiaChat.Api/appsettings.json`
- Create: `api/FaiaChat.Api/appsettings.Development.json`
- Create: `api/.gitignore`

**Step 1: Create the project**

```bash
cd /Users/edvard.unsvag/faia-hjemmeside
mkdir -p api
cd api
dotnet new webapi -n FaiaChat.Api --use-minimal-apis
```

**Step 2: Add NuGet packages**

```bash
cd FaiaChat.Api
dotnet add package Microsoft.SemanticKernel
dotnet add package Anthropic.SDK
dotnet add package Microsoft.AspNetCore.RateLimiting
```

Note: Check if a Semantic Kernel connector for Anthropic/Claude exists. If not, use `Microsoft.SemanticKernel` with a custom `IChatCompletionService` wrapper around `Anthropic.SDK`. Alternatively, check for `Microsoft.SemanticKernel.Connectors.Anthropic` or similar community package.

**Step 3: Set up minimal Program.cs with CORS**

```csharp
var builder = WebApplication.CreateBuilder(args);

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
```

**Step 4: Configure appsettings.Development.json**

```json
{
  "Anthropic": {
    "ApiKey": "YOUR_KEY_HERE"
  },
  "Notion": {
    "ApiKey": "YOUR_KEY_HERE",
    "PageIds": []
  }
}
```

Add to `.gitignore`:
```
appsettings.Development.json
```

**Step 5: Verify it runs**

```bash
dotnet run
# Visit http://localhost:5000/health → "OK"
```

**Step 6: Commit**

```bash
git add api/
git commit -m "feat(api): scaffold .NET API project with Semantic Kernel"
```

---

## Task 2: Notion content fetcher with caching

**Files:**
- Create: `api/FaiaChat.Api/Services/NotionContentService.cs`
- Create: `api/FaiaChat.Api/Models/NotionConfig.cs`
- Modify: `api/FaiaChat.Api/Program.cs`

**Step 1: Create config model**

```csharp
// Models/NotionConfig.cs
namespace FaiaChat.Api.Models;

public class NotionConfig
{
    public string ApiKey { get; set; } = "";
    public List<string> PageIds { get; set; } = new();
    public int CacheTtlMinutes { get; set; } = 60;
}
```

**Step 2: Create the Notion content service**

```csharp
// Services/NotionContentService.cs
namespace FaiaChat.Api.Services;

public class NotionContentService
{
    private readonly NotionConfig _config;
    private readonly HttpClient _httpClient;
    private string? _cachedContent;
    private DateTime _cacheExpiry = DateTime.MinValue;
    private string? _staleCache; // fallback if Notion is down

    public NotionContentService(IOptions<NotionConfig> config, HttpClient httpClient)
    {
        _config = config.Value;
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");
        _httpClient.DefaultRequestHeaders.Add("Notion-Version", "2022-06-28");
    }

    public async Task<string> GetContentAsync()
    {
        if (_cachedContent != null && DateTime.UtcNow < _cacheExpiry)
            return _cachedContent;

        try
        {
            var contents = new List<string>();
            foreach (var pageId in _config.PageIds)
            {
                var blocks = await FetchPageBlocksAsync(pageId);
                contents.Add(blocks);
            }

            _cachedContent = string.Join("\n\n---\n\n", contents);
            _staleCache = _cachedContent;
            _cacheExpiry = DateTime.UtcNow.AddMinutes(_config.CacheTtlMinutes);
            return _cachedContent;
        }
        catch
        {
            if (_staleCache != null)
                return _staleCache;
            throw;
        }
    }

    private async Task<string> FetchPageBlocksAsync(string pageId)
    {
        // Fetch block children from Notion API and extract text content
        var response = await _httpClient.GetAsync(
            $"https://api.notion.com/v1/blocks/{pageId}/children?page_size=100");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        // Parse blocks and extract text — implement based on Notion block types
        // For now, return raw text extraction from paragraph, heading, bulleted_list blocks
        return ExtractTextFromBlocks(json);
    }

    private string ExtractTextFromBlocks(string json)
    {
        // Parse JSON and extract text from supported block types
        // Implementation depends on which block types the Notion pages use
        // Start with: paragraph, heading_1/2/3, bulleted_list_item, numbered_list_item
        using var doc = JsonDocument.Parse(json);
        var results = doc.RootElement.GetProperty("results");
        var lines = new List<string>();

        foreach (var block in results.EnumerateArray())
        {
            var type = block.GetProperty("type").GetString();
            if (type != null && block.TryGetProperty(type, out var content))
            {
                if (content.TryGetProperty("rich_text", out var richText))
                {
                    var text = string.Join("", richText.EnumerateArray()
                        .Select(rt => rt.GetProperty("plain_text").GetString()));

                    var prefix = type switch
                    {
                        "heading_1" => "# ",
                        "heading_2" => "## ",
                        "heading_3" => "### ",
                        "bulleted_list_item" => "- ",
                        "numbered_list_item" => "1. ",
                        _ => ""
                    };

                    lines.Add($"{prefix}{text}");
                }
            }
        }

        return string.Join("\n", lines);
    }
}
```

**Step 3: Register in Program.cs**

```csharp
builder.Services.Configure<NotionConfig>(builder.Configuration.GetSection("Notion"));
builder.Services.AddHttpClient<NotionContentService>();
builder.Services.AddSingleton<NotionContentService>();
```

**Step 4: Verify it compiles**

```bash
dotnet build
```

**Step 5: Commit**

```bash
git add api/
git commit -m "feat(api): add Notion content fetcher with TTL caching"
```

---

## Task 3: System prompt builder

**Files:**
- Create: `api/FaiaChat.Api/Services/SystemPromptBuilder.cs`

**Step 1: Create the prompt builder**

```csharp
// Services/SystemPromptBuilder.cs
namespace FaiaChat.Api.Services;

public class SystemPromptBuilder
{
    private readonly NotionContentService _notionService;

    public SystemPromptBuilder(NotionContentService notionService)
    {
        _notionService = notionService;
    }

    public async Task<string> BuildAsync()
    {
        var notionContent = await _notionService.GetContentAsync();

        return $"""
            Du er FAIA-assistenten, en profesjonell og saklig rådgiver for Forte AI Accelerator.

            Instruksjoner:
            - Svar kun basert på innholdet nedenfor. Ikke spekuler eller finn på informasjon.
            - Hvis spørsmålet er utenfor det du kan svare på, si det ærlig og oppfordre brukeren til å ta kontakt med FAIA direkte på e-post: kontakt@faia.no
            - Svar på norsk.
            - Vær kort og konsist. Profesjonell tone.
            - Ikke bruk emojier.

            Her er informasjonen du har tilgjengelig:

            {notionContent}
            """;
    }
}
```

**Step 2: Register in Program.cs**

```csharp
builder.Services.AddSingleton<SystemPromptBuilder>();
```

**Step 3: Verify it compiles**

```bash
dotnet build
```

**Step 4: Commit**

```bash
git add api/
git commit -m "feat(api): add system prompt builder with Notion content"
```

---

## Task 4: Chat endpoint with streaming

**Files:**
- Create: `api/FaiaChat.Api/Models/ChatRequest.cs`
- Modify: `api/FaiaChat.Api/Program.cs`

**Step 1: Create request model**

```csharp
// Models/ChatRequest.cs
namespace FaiaChat.Api.Models;

public class ChatRequest
{
    public List<ChatMessage> Messages { get; set; } = new();
}

public class ChatMessage
{
    public string Role { get; set; } = "";
    public string Content { get; set; } = "";
}
```

**Step 2: Set up Semantic Kernel in Program.cs**

Note: The exact Semantic Kernel connector for Claude/Anthropic may vary. Check the latest available package. If no official connector exists, use `Anthropic.SDK` directly for the streaming call and wrap it. The code below assumes a direct Anthropic SDK approach if no SK connector is available:

```csharp
// In Program.cs — add the chat endpoint

app.MapPost("/api/chat", async (ChatRequest request, SystemPromptBuilder promptBuilder, HttpContext context) =>
{
    // Validate
    if (request.Messages == null || request.Messages.Count == 0)
        return Results.BadRequest("Messages required");

    var userMessageCount = request.Messages.Count(m => m.Role == "user");
    if (userMessageCount > 20)
        return Results.BadRequest("Message limit exceeded");

    // Build system prompt
    var systemPrompt = await promptBuilder.BuildAsync();

    // Set up streaming response
    context.Response.ContentType = "text/event-stream";
    context.Response.Headers.CacheControl = "no-cache";
    context.Response.Headers.Connection = "keep-alive";

    // Call Claude API with streaming
    var apiKey = builder.Configuration["Anthropic:ApiKey"];
    using var client = new Anthropic.AnthropicClient(apiKey);

    var messages = request.Messages.Select(m => new { role = m.Role, content = m.Content }).ToList();

    // Stream using Anthropic SDK
    // Exact API depends on SDK version — adapt as needed
    await foreach (var token in StreamClaudeResponseAsync(client, systemPrompt, request.Messages))
    {
        await context.Response.WriteAsync($"data: {token}\n\n");
        await context.Response.Body.FlushAsync();
    }

    await context.Response.WriteAsync("data: [DONE]\n\n");
    await context.Response.Body.FlushAsync();

    return Results.Empty;
});
```

The `StreamClaudeResponseAsync` method will need to be implemented using whichever SDK/connector is available. This is the core integration point that should be verified against the latest Anthropic .NET SDK docs.

**Step 3: Verify it compiles**

```bash
dotnet build
```

**Step 4: Test manually**

```bash
dotnet run
# In another terminal:
curl -X POST http://localhost:5000/api/chat \
  -H "Content-Type: application/json" \
  -d '{"messages":[{"role":"user","content":"Hva er AI Accelerator?"}]}'
```

**Step 5: Commit**

```bash
git add api/
git commit -m "feat(api): add streaming chat endpoint with Claude integration"
```

---

## Task 5: Rate limiting

**Files:**
- Modify: `api/FaiaChat.Api/Program.cs`

**Step 1: Add rate limiting middleware**

```csharp
using System.Threading.RateLimiting;

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

// After app.UseCors():
app.UseRateLimiter();

// Update the endpoint:
app.MapPost("/api/chat", ...).RequireRateLimiting("chat");
```

**Step 2: Verify it compiles and runs**

```bash
dotnet build && dotnet run
```

**Step 3: Commit**

```bash
git add api/
git commit -m "feat(api): add rate limiting to chat endpoint"
```

---

## Task 6: Frontend — API integration with streaming

**Files:**
- Modify: `src/components/Chat/Chat.jsx`
- Delete content from: `src/data/chatResponses.js` (keep file, export only `welcomeMessage`)

**Step 1: Update chatResponses.js — keep only the welcome message**

```js
// src/data/chatResponses.js
export const welcomeMessage =
  'Hei! Jeg er FAIA-assistenten. Fortell meg om utfordringen din, så hjelper jeg deg å finne ut hvordan AI Accelerator kan hjelpe.'
```

**Step 2: Rewrite Chat.jsx to use streaming API**

Replace the `handleSend` function and add streaming logic:

```jsx
import { useState, useRef, useEffect } from 'react'
import { welcomeMessage } from '../../data/chatResponses'
import styles from './Chat.module.scss'

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000'

function Chat() {
  const [messages, setMessages] = useState([
    { id: 1, sender: 'bot', text: welcomeMessage },
  ])
  const [inputValue, setInputValue] = useState('')
  const [isTyping, setIsTyping] = useState(false)
  const messagesEndRef = useRef(null)
  const nextIdRef = useRef(2)

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages, isTyping])

  const handleSend = async () => {
    const trimmed = inputValue.trim()
    if (!trimmed || isTyping) return

    const userMsg = { id: nextIdRef.current++, sender: 'user', text: trimmed }
    const updatedMessages = [...messages, userMsg]
    setMessages(updatedMessages)
    setInputValue('')
    setIsTyping(true)

    // Build API message history (exclude welcome message, map to role/content)
    const apiMessages = updatedMessages
      .filter((m) => m.id !== 1)
      .map((m) => ({
        role: m.sender === 'user' ? 'user' : 'assistant',
        content: m.text,
      }))

    const botId = nextIdRef.current++

    try {
      const response = await fetch(`${API_URL}/api/chat`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ messages: apiMessages }),
      })

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`)
      }

      const reader = response.body.getReader()
      const decoder = new TextDecoder()
      let botText = ''

      setIsTyping(false)
      setMessages((prev) => [...prev, { id: botId, sender: 'bot', text: '' }])

      while (true) {
        const { done, value } = await reader.read()
        if (done) break

        const chunk = decoder.decode(value, { stream: true })
        const lines = chunk.split('\n')

        for (const line of lines) {
          if (line.startsWith('data: ')) {
            const data = line.slice(6)
            if (data === '[DONE]') break
            botText += data
            setMessages((prev) =>
              prev.map((m) => (m.id === botId ? { ...m, text: botText } : m))
            )
          }
        }
      }
    } catch (error) {
      setIsTyping(false)
      setMessages((prev) => [
        ...prev,
        {
          id: botId,
          sender: 'bot',
          text: 'Beklager, noe gikk galt. Prøv igjen.',
          isError: true,
        },
      ])
    }
  }

  const handleKeyDown = (e) => {
    if (e.key === 'Enter') handleSend()
  }

  return (
    <div className={styles.chat}>
      <div className={styles.header}>FAIA-assistenten</div>
      <div className={styles.messages}>
        {messages.map((msg) => (
          <div
            key={msg.id}
            className={`${styles.message} ${styles[msg.sender]}`}
          >
            {msg.text}
          </div>
        ))}
        {isTyping && (
          <div className={styles.typing}>
            <span className={styles.dot} />
            <span className={styles.dot} />
            <span className={styles.dot} />
          </div>
        )}
        <div ref={messagesEndRef} />
      </div>
      <div className={styles.inputArea}>
        <input
          type="text"
          className={styles.input}
          value={inputValue}
          onChange={(e) => setInputValue(e.target.value)}
          onKeyDown={handleKeyDown}
          placeholder="Fortell oss om din utfordring..."
          maxLength={500}
        />
        <button
          className={styles.sendButton}
          onClick={handleSend}
          disabled={!inputValue.trim() || isTyping}
        >
          Send
        </button>
      </div>
    </div>
  )
}

export default Chat
```

**Step 3: Verify frontend compiles**

```bash
cd /Users/edvard.unsvag/faia-hjemmeside
npm run build
```

**Step 4: Commit**

```bash
git add src/
git commit -m "feat(chat): integrate streaming Claude API, replace mocked responses"
```

---

## Task 7: Frontend — sessionStorage persistence

**Files:**
- Modify: `src/components/Chat/Chat.jsx`

**Step 1: Add sessionStorage save/restore**

Add to `Chat.jsx`:

```jsx
// Replace the useState for messages with:
const [messages, setMessages] = useState(() => {
  const saved = sessionStorage.getItem('faia-chat-messages')
  if (saved) {
    try {
      const parsed = JSON.parse(saved)
      return parsed
    } catch {
      return [{ id: 1, sender: 'bot', text: welcomeMessage }]
    }
  }
  return [{ id: 1, sender: 'bot', text: welcomeMessage }]
})

// Initialize nextIdRef based on saved messages:
const nextIdRef = useRef(() => {
  const maxId = messages.reduce((max, m) => Math.max(max, m.id), 0)
  return maxId + 1
})

// Add useEffect to persist messages:
useEffect(() => {
  sessionStorage.setItem('faia-chat-messages', JSON.stringify(messages))
}, [messages])
```

**Step 2: Verify it works**

```bash
npm run dev
# Open browser, send a message, refresh page — messages should persist
```

**Step 3: Commit**

```bash
git add src/components/Chat/Chat.jsx
git commit -m "feat(chat): persist conversation in sessionStorage"
```

---

## Task 8: Frontend — message limit

**Files:**
- Modify: `src/components/Chat/Chat.jsx`
- Modify: `src/components/Chat/Chat.module.scss`

**Step 1: Add message limit logic**

Add to `Chat.jsx`:

```jsx
const MAX_USER_MESSAGES = 20

// Compute user message count:
const userMessageCount = messages.filter((m) => m.sender === 'user').length
const limitReached = userMessageCount >= MAX_USER_MESSAGES

// In the return JSX, replace the inputArea div:
<div className={styles.inputArea}>
  {limitReached ? (
    <div className={styles.limitMessage}>
      Du har nådd maks antall meldinger. Ta kontakt med oss for å gå videre:{' '}
      <a href="mailto:kontakt@faia.no">kontakt@faia.no</a>
    </div>
  ) : (
    <>
      <input ... />
      <button ... />
    </>
  )}
</div>
```

**Step 2: Add styles for limitMessage**

```scss
// In Chat.module.scss
.limitMessage {
  padding: 12px 16px;
  font-size: 14px;
  color: $color-black;
  text-align: center;

  a {
    color: $color-primary;
    text-decoration: underline;
  }
}
```

**Step 3: Verify it compiles**

```bash
npm run build
```

**Step 4: Commit**

```bash
git add src/components/Chat/
git commit -m "feat(chat): add 20-message limit with contact info"
```

---

## Task 9: Frontend — error handling and retry

**Files:**
- Modify: `src/components/Chat/Chat.jsx`
- Modify: `src/components/Chat/Chat.module.scss`

**Step 1: Add retry button for error messages**

In the message rendering in `Chat.jsx`, handle `isError` messages:

```jsx
{messages.map((msg) => (
  <div key={msg.id} className={`${styles.message} ${styles[msg.sender]} ${msg.isError ? styles.error : ''}`}>
    {msg.text}
    {msg.isError && (
      <button className={styles.retryButton} onClick={() => handleRetry(msg.id)}>
        Prøv igjen
      </button>
    )}
  </div>
))}
```

Add `handleRetry`:

```jsx
const handleRetry = (errorMsgId) => {
  // Remove the error message, re-send the last user message
  setMessages((prev) => prev.filter((m) => m.id !== errorMsgId))
  // Find the last user message and resend
  const lastUserMsg = messages.filter((m) => m.sender === 'user').pop()
  if (lastUserMsg) {
    setInputValue(lastUserMsg.text)
  }
}
```

**Step 2: Add timeout to fetch**

```jsx
const controller = new AbortController()
const timeout = setTimeout(() => controller.abort(), 30000)

const response = await fetch(`${API_URL}/api/chat`, {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ messages: apiMessages }),
  signal: controller.signal,
})

clearTimeout(timeout)
```

**Step 3: Add styles**

```scss
.error {
  background: #fff5f5;
  border: 1px solid #e0c0c0;
}

.retryButton {
  display: block;
  margin-top: 8px;
  padding: 4px 12px;
  font-size: 12px;
  background: $color-primary;
  color: $color-white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
}
```

**Step 4: Verify it compiles**

```bash
npm run build
```

**Step 5: Commit**

```bash
git add src/components/Chat/
git commit -m "feat(chat): add error handling with retry and request timeout"
```

---

## Task 10: CORS and environment configuration

**Files:**
- Create: `src/.env.example`
- Modify: `api/FaiaChat.Api/Program.cs` (update CORS for production)

**Step 1: Create .env.example for frontend**

```
VITE_API_URL=http://localhost:5000
```

**Step 2: Update CORS in .NET to support configurable origins**

```csharp
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
```

**Step 3: Add to appsettings.json**

```json
{
  "Cors": {
    "Origins": ["http://localhost:5173"]
  }
}
```

**Step 4: Commit**

```bash
git add .
git commit -m "feat: add CORS and environment configuration"
```

---

## Task 11: End-to-end verification

**Step 1: Start the .NET API**

```bash
cd api/FaiaChat.Api
dotnet run
```

**Step 2: Start the React dev server**

```bash
cd /Users/edvard.unsvag/faia-hjemmeside
npm run dev
```

**Step 3: Test the following scenarios**

- [ ] Send a message → get a streamed response
- [ ] Refresh the page → conversation persists
- [ ] Close tab, reopen → conversation resets
- [ ] Send 20 messages → input locks, contact info shown
- [ ] Kill the API → error message with retry button appears
- [ ] Input 500+ characters → input is capped
- [ ] Rapid-fire messages → rate limiting returns 429

**Step 4: Final commit**

```bash
git add .
git commit -m "feat: complete AI Accelerator chat integration"
```
