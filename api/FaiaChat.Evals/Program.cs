using FaiaChat.Evals;
using FaiaChat.Evals.Personas;
using FaiaChat.Evals.Scorers;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;

// ── Load configuration ──────────────────────────────────────────────────────

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var apiUrl = configuration["ApiUrl"] ?? "http://localhost:5000";
var azureEndpoint = configuration["AzureOpenAI:Endpoint"]
    ?? throw new InvalidOperationException("AzureOpenAI:Endpoint is required in appsettings.json");
var azureApiKey = configuration["AzureOpenAI:ApiKey"]
    ?? throw new InvalidOperationException("AzureOpenAI:ApiKey is required in appsettings.json");
var azureDeployment = configuration["AzureOpenAI:DeploymentName"]
    ?? throw new InvalidOperationException("AzureOpenAI:DeploymentName is required in appsettings.json");

// ── Set up components ───────────────────────────────────────────────────────

var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
var conversationRunner = new ConversationRunner(httpClient, apiUrl);

var kernel = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion(azureDeployment, azureEndpoint, azureApiKey)
    .Build();

var chatService = kernel.GetRequiredService<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService>();
var llmJudge = new LlmJudgeScorer(chatService);

// ── Run all personas ────────────────────────────────────────────────────────

var personas = AllPersonas.All;
var allResults = new List<(string Persona, List<ScoreResult> Scores)>();

Console.WriteLine("FAIA Chat Eval Suite");
Console.WriteLine("====================");
Console.WriteLine($"API URL: {apiUrl}");
Console.WriteLine($"Personas: {personas.Count}");
Console.WriteLine();

for (var i = 0; i < personas.Count; i++)
{
    var persona = personas[i];

    Console.WriteLine($"[{i + 1}/{personas.Count}] {persona.Name}");
    Console.WriteLine($"  Description: {persona.Description}");
    Console.WriteLine($"  Expected track: {persona.ExpectedTrack}");
    Console.WriteLine($"  Full conversation: {persona.IsFullConversation}");
    Console.WriteLine($"  Messages: {persona.Messages.Count}");
    Console.WriteLine();

    var personaScores = new List<ScoreResult>();

    try
    {
        // Run conversation
        Console.WriteLine("  Running conversation...");
        var conversation = await conversationRunner.RunConversationAsync(persona.Messages);

        // Print conversation (truncated)
        Console.WriteLine();
        foreach (var (role, content) in conversation)
        {
            var label = role == "user" ? "  User" : "  Bot ";
            var truncated = content.Length > 100 ? content[..100] + "..." : content;
            Console.WriteLine($"  {label}: {truncated}");
        }
        Console.WriteLine();

        // Run deterministic scoring on each bot response
        Console.WriteLine("  Deterministic scoring...");
        var botResponses = conversation.Where(m => m.Role == "assistant").ToList();

        for (var j = 0; j < botResponses.Count; j++)
        {
            var isClosingMessage = j == botResponses.Count - 1 && persona.IsFullConversation;
            var detResults = DeterministicScorer.ScoreResponse(botResponses[j].Content, isClosingMessage);
            personaScores.AddRange(detResults);
        }

        // Aggregate deterministic results: group by check name, report pass rate
        var grouped = personaScores
            .GroupBy(s => s.Name)
            .OrderBy(g => g.Key);

        foreach (var group in grouped)
        {
            var groupPassed = group.Count(s => s.Passed);
            var groupTotal = group.Count();
            var status = groupPassed == groupTotal ? "PASS" : "FAIL";
            Console.WriteLine($"    [{status}] {group.Key}: {groupPassed}/{groupTotal}");

            // Print details for failures
            foreach (var fail in group.Where(s => !s.Passed && s.Detail != null))
            {
                Console.WriteLine($"           {fail.Detail}");
            }
        }

        // LLM judge scoring for full conversations
        if (persona.IsFullConversation)
        {
            Console.WriteLine();
            Console.WriteLine("  LLM judge scoring...");

            var llmResults = await llmJudge.ScoreConversationAsync(
                persona.Name,
                persona.ExpectedTrack,
                conversation);

            personaScores.AddRange(llmResults);

            foreach (var result in llmResults)
            {
                var icon = result.Passed ? "PASS" : "FAIL";
                Console.WriteLine($"    [{icon}] {result.Name}: {result.Detail}");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  ERROR: {ex.Message}");
        if (ex.InnerException != null)
            Console.WriteLine($"         Inner: {ex.InnerException.Message}");
    }

    allResults.Add((persona.Name, personaScores));
    Console.WriteLine();
    Console.WriteLine(new string('-', 60));
    Console.WriteLine();
}

// ── Print summary ───────────────────────────────────────────────────────────

Console.WriteLine("=== SUMMARY ===");
Console.WriteLine();

var totalPassed = allResults.Sum(r => r.Scores.Count(s => s.Passed));
var totalFailed = allResults.Sum(r => r.Scores.Count(s => !s.Passed));
var total = totalPassed + totalFailed;
var percentage = total > 0 ? (double)totalPassed / total * 100 : 0;

Console.WriteLine($"Total: {totalPassed}/{total} passed ({percentage:F1}%)");
Console.WriteLine();

Console.WriteLine("Per persona:");
foreach (var (personaName, scores) in allResults)
{
    var passed = scores.Count(s => s.Passed);
    var failed = scores.Count(s => !s.Passed);
    var count = passed + failed;
    var pct = count > 0 ? (double)passed / count * 100 : 0;
    var status = failed == 0 ? "PASS" : "FAIL";
    Console.WriteLine($"  [{status}] {personaName}: {passed}/{count} ({pct:F0}%)");
}

Console.WriteLine();
Console.WriteLine("Done.");
