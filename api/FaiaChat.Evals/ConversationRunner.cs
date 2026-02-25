using System.Text;
using System.Text.Json;

namespace FaiaChat.Evals;

public class ConversationRunner
{
    private const int MaxRetries = 3;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan InterMessageDelay = TimeSpan.FromSeconds(2);

    private readonly HttpClient _httpClient;
    private readonly string _apiUrl;

    public ConversationRunner(HttpClient httpClient, string apiUrl = "http://localhost:5000")
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(60);
        _apiUrl = apiUrl.TrimEnd('/');
    }

    /// <summary>
    /// Runs a scripted conversation by sending each user message in sequence,
    /// collecting the bot's SSE-streamed responses, and returning the full
    /// conversation history as a list of (Role, Content) tuples.
    /// </summary>
    public async Task<List<(string Role, string Content)>> RunConversationAsync(List<string> userMessages)
    {
        var conversation = new List<(string Role, string Content)>();
        var apiMessages = new List<object>();

        for (var i = 0; i < userMessages.Count; i++)
        {
            var userMessage = userMessages[i];

            // Add user message to history
            conversation.Add(("user", userMessage));
            apiMessages.Add(new { role = "user", content = userMessage });

            // Send the full conversation history and get bot response
            var botResponse = await SendMessageWithRetryAsync(apiMessages);

            // Add bot response to history
            conversation.Add(("assistant", botResponse));
            apiMessages.Add(new { role = "assistant", content = botResponse });

            // Delay between messages to avoid rate limiting (skip after last message)
            if (i < userMessages.Count - 1)
            {
                await Task.Delay(InterMessageDelay);
            }
        }

        return conversation;
    }

    private async Task<string> SendMessageWithRetryAsync(List<object> messages)
    {
        for (var attempt = 0; attempt <= MaxRetries; attempt++)
        {
            try
            {
                return await SendMessageAsync(messages);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                if (attempt == MaxRetries)
                    throw new InvalidOperationException(
                        $"Rate limited after {MaxRetries + 1} attempts. Last error: {ex.Message}", ex);

                Console.WriteLine($"  Rate limited (429). Waiting {RetryDelay.TotalSeconds}s before retry {attempt + 1}/{MaxRetries}...");
                await Task.Delay(RetryDelay);
            }
        }

        // Unreachable, but the compiler needs it
        throw new InvalidOperationException("Unexpected: exhausted retries without result or exception.");
    }

    private async Task<string> SendMessageAsync(List<object> messages)
    {
        var requestBody = JsonSerializer.Serialize(new { messages });
        var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_apiUrl}/api/chat")
        {
            Content = content
        };

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            throw new HttpRequestException("Rate limited", null, System.Net.HttpStatusCode.TooManyRequests);
        }

        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream, Encoding.UTF8);

        var result = new StringBuilder();

        // Track whether the previous line was a "data:" line to detect
        // continuation lines (which represent newlines in the original text).
        //
        // The API encodes newlines via: text.Replace("\n", "\ndata: ")
        // So a chunk "foo\nbar" becomes the SSE event:
        //   data: foo
        //   data: bar
        //   (blank line)
        //
        // Meanwhile separate chunks are separate SSE events:
        //   data: hello
        //   (blank line)
        //   data: world
        //   (blank line)
        //
        // Two consecutive "data:" lines (no blank line in between) means
        // the second is a continuation = a \n was in the original text.
        var previousWasData = false;

        while (await reader.ReadLineAsync() is { } line)
        {
            if (line.StartsWith("data: "))
            {
                var data = line["data: ".Length..];

                if (data == "[DONE]")
                    break;

                if (previousWasData)
                {
                    // Continuation line within the same SSE event:
                    // the original text had a newline here.
                    result.Append('\n');
                }

                result.Append(data);
                previousWasData = true;
            }
            else
            {
                // Blank line (SSE event separator) or other line (comment, event type).
                // Reset the continuation tracking.
                previousWasData = false;
            }
        }

        return result.ToString();
    }
}
