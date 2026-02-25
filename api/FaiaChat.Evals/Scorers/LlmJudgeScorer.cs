using System.Text;
using System.Text.RegularExpressions;
using Microsoft.SemanticKernel.ChatCompletion;

namespace FaiaChat.Evals.Scorers;

public partial class LlmJudgeScorer
{
    private static readonly string[] Dimensions =
    {
        "track_identification",
        "conversation_flow",
        "appropriate_closure",
        "knowledge_accuracy",
        "tone",
    };

    private readonly IChatCompletionService _chat;

    public LlmJudgeScorer(IChatCompletionService chat)
    {
        _chat = chat;
    }

    public async Task<List<ScoreResult>> ScoreConversationAsync(
        string personaName,
        string expectedTrack,
        List<(string Role, string Content)> conversation)
    {
        var prompt = BuildJudgePrompt(personaName, expectedTrack, conversation);

        var history = new ChatHistory();
        history.AddUserMessage(prompt);

        string response;
        try
        {
            var result = await _chat.GetChatMessageContentAsync(history);
            response = result.Content ?? string.Empty;
        }
        catch (Exception ex)
        {
            return Dimensions
                .Select(d => new ScoreResult(d, Passed: false, Detail: $"LLM judge call failed: {ex.Message}"))
                .ToList();
        }

        return ParseJudgeResponse(response);
    }

    private static string BuildJudgePrompt(
        string personaName,
        string expectedTrack,
        List<(string Role, string Content)> conversation)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Du er en ekspert-evaluator for en AI-chatbot som tilhører FAIA, et norsk konsulentselskap innen AI.");
        sb.AppendLine("Du skal vurdere kvaliteten på følgende samtale mellom en bruker og chatboten.");
        sb.AppendLine();
        sb.AppendLine($"Personaen som ble simulert heter \"{personaName}\".");
        sb.AppendLine($"Forventet spor (track): {expectedTrack}");
        sb.AppendLine("FAIA har fire spor:");
        sb.AppendLine("  A = AI Automatisering (automatisere repetitive prosesser)");
        sb.AppendLine("  B = Skreddersydd AI-agent (bygge tilpassede AI-verktøy)");
        sb.AppendLine("  C = AI Strategi & Rådgivning (strategisk rådgivning for ledere)");
        sb.AppendLine("  D = Opportunity Sprint (kort utforskningssprint for å finne AI-muligheter)");
        sb.AppendLine();
        sb.AppendLine("Samtalen:");
        sb.AppendLine("---");

        foreach (var (role, content) in conversation)
        {
            var label = role.Equals("assistant", StringComparison.OrdinalIgnoreCase) ? "Bot" : "Bruker";
            sb.AppendLine($"{label}: {content}");
        }

        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("Vurder samtalen på følgende fem dimensjoner med en poengsum fra 1 til 5:");
        sb.AppendLine();
        sb.AppendLine("1. track_identification: Identifiserte boten riktig spor (A/B/C/D) basert på brukerens behov?");
        sb.AppendLine("   Hvis forventet spor er \"any\", vurder om boten klarte å identifisere et passende spor i det hele tatt.");
        sb.AppendLine("   Hvis forventet spor er \"redirect\", vurder om boten korrekt avviste off-topic/manipulerende forespørsler.");
        sb.AppendLine();
        sb.AppendLine("2. conversation_flow: Var samtalen naturlig og flytende? Stilte boten gode oppfølgingsspørsmål uten å være repetitiv eller mekanisk?");
        sb.AppendLine();
        sb.AppendLine("3. appropriate_closure: Avsluttet boten med en bookinglenke når brukeren var klar? Var timingen riktig - verken for tidlig eller for sent?");
        sb.AppendLine();
        sb.AppendLine("4. knowledge_accuracy: Stemte informasjonen boten ga med FAIAs faktiske tjenester og kompetanse? Var det noen feilinformasjon?");
        sb.AppendLine();
        sb.AppendLine("5. tone: Var tonen varm, direkte og konsulentaktig? Snakket boten som en rådgiver, ikke som en maskin?");
        sb.AppendLine();
        sb.AppendLine("Svar med NØYAKTIG dette formatet, én linje per dimensjon:");
        sb.AppendLine();
        sb.AppendLine("track_identification: [1-5] [begrunnelse]");
        sb.AppendLine("conversation_flow: [1-5] [begrunnelse]");
        sb.AppendLine("appropriate_closure: [1-5] [begrunnelse]");
        sb.AppendLine("knowledge_accuracy: [1-5] [begrunnelse]");
        sb.AppendLine("tone: [1-5] [begrunnelse]");
        sb.AppendLine();
        sb.AppendLine("Viktig: Skriv KUN de fem linjene over, ingen ekstra tekst før eller etter.");

        return sb.ToString();
    }

    private static List<ScoreResult> ParseJudgeResponse(string response)
    {
        var results = new List<ScoreResult>();

        foreach (var dimension in Dimensions)
        {
            var match = Regex.Match(
                response,
                $@"^{Regex.Escape(dimension)}:\s*\[?(\d)\]?\s+(.*)",
                RegexOptions.Multiline | RegexOptions.IgnoreCase);

            if (match.Success && int.TryParse(match.Groups[1].Value, out var score) && score >= 1 && score <= 5)
            {
                var reasoning = match.Groups[2].Value.Trim();
                results.Add(new ScoreResult(
                    dimension,
                    Passed: score >= 3,
                    Detail: $"Score: {score}/5. {reasoning}"));
            }
            else
            {
                results.Add(new ScoreResult(
                    dimension,
                    Passed: false,
                    Detail: $"Failed to parse LLM judge output for \"{dimension}\". Raw response: {Truncate(response, 200)}"));
            }
        }

        return results;
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        return value.Length <= maxLength ? value : value[..maxLength] + "...";
    }
}
