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
