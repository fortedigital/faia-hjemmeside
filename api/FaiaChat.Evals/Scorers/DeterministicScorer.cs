using System.Globalization;
using System.Text.RegularExpressions;

namespace FaiaChat.Evals.Scorers;

public static partial class DeterministicScorer
{
    private const int MaxSentences = 3;

    public static List<ScoreResult> ScoreResponse(string botResponse, bool isClosingMessage)
    {
        var results = new List<ScoreResult>
        {
            CheckMaxSentences(botResponse),
            CheckNoLists(botResponse),
            CheckNoPiiRequest(botResponse),
            CheckNoHallucinatedActions(botResponse),
            CheckNoEmojis(botResponse),
        };

        if (!isClosingMessage)
        {
            results.Add(CheckEndsWithQuestion(botResponse));
        }

        return results;
    }

    // ── max_sentences ──────────────────────────────────────────────────
    // Count sentences by splitting on sentence-ending punctuation (.!?)
    // followed by whitespace and an uppercase letter (including Æ, Ø, Å).
    // The first "sentence" is implicit, so matches = additional sentences.

    private static ScoreResult CheckMaxSentences(string response)
    {
        var trimmed = response.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return new ScoreResult("max_sentences", true);

        // Each match marks the start of a new sentence after the first one.
        var boundaries = SentenceBoundaryRegex().Matches(trimmed);
        var count = boundaries.Count + 1;

        return new ScoreResult(
            "max_sentences",
            count <= MaxSentences,
            $"Found {count} sentence(s) (max {MaxSentences})");
    }

    [GeneratedRegex(@"[.!?]\s+[A-ZÆØÅ]")]
    private static partial Regex SentenceBoundaryRegex();

    // ── no_lists ───────────────────────────────────────────────────────
    // Detect bullet points (- , * , • ), numbered lists (1. , 1) ),
    // markdown headers (# ), and bold (** , __ ).

    private static ScoreResult CheckNoLists(string response)
    {
        var match = ListPatternRegex().Match(response);
        return new ScoreResult(
            "no_lists",
            !match.Success,
            match.Success ? $"Detected list/formatting: \"{match.Value.Trim()}\"" : null);
    }

    [GeneratedRegex(@"(^[ \t]*[-*•]\s)|(^[ \t]*\d+[.)]\s)|(^[ \t]*#{1,6}\s)|(\*\*)|(__)", RegexOptions.Multiline)]
    private static partial Regex ListPatternRegex();

    // ── no_pii_request ─────────────────────────────────────────────────
    // Detect when the bot asks the user for personal contact information.

    private static ScoreResult CheckNoPiiRequest(string response)
    {
        var match = PiiRequestRegex().Match(response);
        return new ScoreResult(
            "no_pii_request",
            !match.Success,
            match.Success ? $"PII request detected: \"{match.Value}\"" : null);
    }

    [GeneratedRegex(
        @"(hva\s+er\s+(din|ditt)\s+(e-?post|mail|telefon|nummer|navn|adresse))" +
        @"|(send\s+(meg|oss)\s+(din|ditt)\s+(e-?post|mail|telefon|nummer|navn))" +
        @"|(hvilken\s+e-?post)" +
        @"|(oppgi\s+(din|ditt)\s+(e-?post|mail|telefon|nummer|navn))" +
        @"|(kan\s+(du|jeg)\s+f[åa]\s+(din|ditt)\s+(e-?post|mail|telefon|nummer|navn))" +
        @"|(gi\s+(meg|oss)\s+(din|ditt)\s+(e-?post|mail|telefon|nummer))",
        RegexOptions.IgnoreCase)]
    private static partial Regex PiiRequestRegex();

    // ── no_hallucinated_actions ─────────────────────────────────────────
    // Detect when the bot claims to perform real-world actions it cannot do.

    private static ScoreResult CheckNoHallucinatedActions(string response)
    {
        var match = HallucinatedActionRegex().Match(response);
        return new ScoreResult(
            "no_hallucinated_actions",
            !match.Success,
            match.Success ? $"Hallucinated action detected: \"{match.Value}\"" : null);
    }

    [GeneratedRegex(
        @"(jeg\s+sender)" +
        @"|(jeg\s+booker)" +
        @"|(jeg\s+har\s+sendt)" +
        @"|(jeg\s+har\s+booket)" +
        @"|(jeg\s+bestiller)" +
        @"|(jeg\s+har\s+bestilt)" +
        @"|(kalenderinvitasjon)" +
        @"|(invitasjonen\s+er\s+sendt)" +
        @"|(m[øo]tet\s+er\s+(booket|bestilt|satt\s+opp))" +
        @"|(e-?posten\s+er\s+sendt)",
        RegexOptions.IgnoreCase)]
    private static partial Regex HallucinatedActionRegex();

    // ── no_emojis ──────────────────────────────────────────────────────
    // Detect Unicode emoji ranges. Uses a BMP regex for common emoji-like
    // symbols, plus a code-point scan for supplementary-plane emojis
    // (U+1F000 and above) which cannot be expressed in GeneratedRegex
    // character classes via surrogate pairs.

    private static ScoreResult CheckNoEmojis(string response)
    {
        // Check BMP emoji symbols first
        var match = BmpEmojiRegex().Match(response);
        if (match.Success)
            return new ScoreResult("no_emojis", false, $"Emoji detected: \"{match.Value}\"");

        // Check supplementary-plane emojis (U+1F000+) via code-point iteration
        var enumerator = StringInfo.GetTextElementEnumerator(response);
        while (enumerator.MoveNext())
        {
            var element = enumerator.GetTextElement();
            var codePoint = char.ConvertToUtf32(element, 0);

            if (codePoint is (>= 0x1F300 and <= 0x1F5FF)   // Misc Symbols and Pictographs
                         or (>= 0x1F600 and <= 0x1F64F)     // Emoticons
                         or (>= 0x1F680 and <= 0x1F6FF)     // Transport and Map
                         or (>= 0x1F700 and <= 0x1F77F)     // Alchemical Symbols
                         or (>= 0x1F900 and <= 0x1F9FF)     // Supplemental Symbols
                         or (>= 0x1FA00 and <= 0x1FA6F)     // Chess Symbols
                         or (>= 0x1FA70 and <= 0x1FAFF)     // Symbols Extended-A
                         or (>= 0x1F000 and <= 0x1F02F)     // Mahjong Tiles
                         or (>= 0x1F0A0 and <= 0x1F0FF))    // Playing Cards
            {
                return new ScoreResult("no_emojis", false, $"Emoji detected: \"{element}\"");
            }
        }

        return new ScoreResult("no_emojis", true);
    }

    [GeneratedRegex(
        @"[\u00A9\u00AE\u203C\u2049\u2122\u2139\u2194-\u2199\u21A9-\u21AA" +
        @"\u231A-\u231B\u2328\u23CF\u23E9-\u23F3\u23F8-\u23FA" +
        @"\u24C2\u25AA-\u25AB\u25B6\u25C0\u25FB-\u25FE" +
        @"\u2600-\u26FF\u2702-\u27BF\u2934-\u2935\u2B05-\u2B07\u2B1B-\u2B1C\u2B50\u2B55" +
        @"\u3030\u303D\u3297\u3299]")]
    private static partial Regex BmpEmojiRegex();

    // ── ends_with_question ─────────────────────────────────────────────
    // Check that the bot response ends with a question mark to keep
    // the conversation moving forward.

    private static ScoreResult CheckEndsWithQuestion(string response)
    {
        var trimmed = response.TrimEnd();
        var passed = trimmed.EndsWith('?');
        return new ScoreResult(
            "ends_with_question",
            passed,
            passed ? null : "Response does not end with a question mark");
    }
}
