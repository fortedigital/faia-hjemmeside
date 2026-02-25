namespace FaiaChat.Evals.Personas;

public record PersonaDefinition(
    string Name,
    string Description,
    string ExpectedTrack,       // "A", "B", "C", "D", "any", or "redirect"
    bool IsFullConversation,    // true = 10-15 messages, false = 3-5
    List<string> Messages       // Pre-scripted user messages
);
