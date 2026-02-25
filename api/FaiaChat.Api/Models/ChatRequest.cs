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
