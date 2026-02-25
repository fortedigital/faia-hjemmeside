namespace FaiaChat.Api.Models;

public class NotionConfig
{
    public string ApiKey { get; set; } = "";
    public List<string> PageIds { get; set; } = new();
    public int CacheTtlMinutes { get; set; } = 60;
}
