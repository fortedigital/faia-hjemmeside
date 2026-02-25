using System.Text.Json;
using Microsoft.Extensions.Options;
using FaiaChat.Api.Models;

namespace FaiaChat.Api.Services;

public class NotionContentService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly NotionConfig _config;

    private string? _cachedContent;
    private string? _staleContent;
    private DateTime _cacheExpiry = DateTime.MinValue;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public NotionContentService(IOptions<NotionConfig> config, IHttpClientFactory httpClientFactory)
    {
        _config = config.Value;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string> GetContentAsync()
    {
        if (_cachedContent is not null && DateTime.UtcNow < _cacheExpiry)
        {
            return _cachedContent;
        }

        await _lock.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (_cachedContent is not null && DateTime.UtcNow < _cacheExpiry)
            {
                return _cachedContent;
            }

            try
            {
                var pageContents = new List<string>();

                foreach (var pageId in _config.PageIds)
                {
                    var content = await FetchPageContentAsync(pageId);
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        pageContents.Add(content);
                    }
                }

                var result = string.Join("\n\n---\n\n", pageContents);

                _cachedContent = result;
                _staleContent = result;
                _cacheExpiry = DateTime.UtcNow.AddMinutes(_config.CacheTtlMinutes);

                return result;
            }
            catch
            {
                if (_staleContent is not null)
                {
                    return _staleContent;
                }

                throw;
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<string> FetchPageContentAsync(string pageId)
    {
        var client = _httpClientFactory.CreateClient("notion");
        client.BaseAddress = new Uri("https://api.notion.com/");
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");
        client.DefaultRequestHeaders.Add("Notion-Version", "2022-06-28");

        var response = await client.GetAsync($"v1/blocks/{pageId}/children?page_size=100");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        var lines = new List<string>();

        foreach (var block in doc.RootElement.GetProperty("results").EnumerateArray())
        {
            var type = block.GetProperty("type").GetString();
            var text = ExtractText(block, type);

            if (!string.IsNullOrWhiteSpace(text))
            {
                lines.Add(text);
            }
        }

        return string.Join("\n", lines);
    }

    private static string? ExtractText(JsonElement block, string? type)
    {
        if (type is null) return null;

        var supportedTypes = new[]
        {
            "paragraph", "heading_1", "heading_2", "heading_3",
            "bulleted_list_item", "numbered_list_item"
        };

        if (!supportedTypes.Contains(type)) return null;

        if (!block.TryGetProperty(type, out var typeData)) return null;
        if (!typeData.TryGetProperty("rich_text", out var richText)) return null;

        var parts = new List<string>();
        foreach (var segment in richText.EnumerateArray())
        {
            if (segment.TryGetProperty("plain_text", out var plainText))
            {
                parts.Add(plainText.GetString() ?? "");
            }
        }

        return string.Join("", parts);
    }
}
