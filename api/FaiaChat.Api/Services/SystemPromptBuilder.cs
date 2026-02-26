using zborek.Langfuse.Client;
using zborek.Langfuse.Models.Prompt;

namespace FaiaChat.Api.Services;

public class SystemPromptBuilder
{
    private readonly ILangfuseClient _langfuse;
    private static string? _cachedPrompt;
    private static DateTime _cacheExpiry;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public SystemPromptBuilder(ILangfuseClient langfuse)
    {
        _langfuse = langfuse;
    }

    public async Task<string> BuildAsync()
    {
        if (_cachedPrompt is not null && DateTime.UtcNow < _cacheExpiry)
            return _cachedPrompt;

        try
        {
            var prompt = await _langfuse.GetPromptAsync("faia-system-prompt", label: "production");
            if (prompt is TextPrompt textPrompt)
            {
                var template = textPrompt.PromptText;
                var compiled = template.Replace("{{knowledge}}", Content);
                _cachedPrompt = compiled;
                _cacheExpiry = DateTime.UtcNow.Add(CacheTtl);
                return compiled;
            }
        }
        catch
        {
            // Fallback to hardcoded prompt if Langfuse is unavailable
        }

        return BuildFallback();
    }

    private string BuildFallback()
    {
        return $"""
            Du er FAIA, en erfaren rådgiver hos Forte som hjelper potensielle kunder å forstå AI Accelerator.

            Slik oppfører du deg:
            - Du svarer ALLTID med maks 2-3 korte setninger. Aldri mer.
            - Du avslutter ALLTID svaret med ett relevant oppfølgingsspørsmål for å forstå brukerens situasjon bedre.
            - Du skriver som i en uformell chat — ingen lister, ingen overskrifter, ingen markdown-formatering, ingen punktlister, ingen nummererte punkter. Bare vanlige setninger.
            - Du er en aktiv guide som styrer samtalen. Still spørsmål for å identifisere riktig spor (A: Prosessautomatisering, B: Dataintelligens, C: Ny app, D: Oppgradering).
            - Når du har forstått brukerens behov og identifisert riktig spor, oppsummer kort og avslutt med: "Vil du ta en prat med teamet? Book et møte her: https://forte.no/kontakt"
            - Etter at du har foreslått å booke møte, si noe hyggelig som avslutning. IKKE fortsett å stille spørsmål. Samtalen er ferdig.
            - Du kan IKKE booke møter, sende e-post, sende kalenderinvitasjoner, eller gjøre noe utenfor denne chatten. Du er kun en informasjonsassistent. Aldri lat som om du kan utføre handlinger.
            - Aldri be om personlig informasjon som e-post, telefonnummer eller navn.
            - Svar kun basert på kunnskapen nedenfor. Hvis du ikke vet, si det ærlig og tilby å koble brukeren med teamet via lenken over.
            - Svar på norsk. Varm og direkte tone, som en erfaren konsulent.
            - Ikke bruk emojier.

            Kunnskap:

            {Content}
            """;
    }

    private const string Content = """
        # AI Accelerator — Hva vi bygger & hvordan vi selger det

        Konkrete eksempler på 6-ukers AI Accelerator-oppdrag, klare for kundesamtaler.

        Hver kunde er forskjellig, men problemene følger gjenkjennbare mønstre. Denne siden beskriver de vanligste typene AI Accelerator-oppdrag, viser hvordan 6 uker ser ut for hvert spor, og gir salgsklare beskrivelser.

        ## De fire sporene

        AI Accelerator er ikke én-størrelse-passer-alle. Avhengig av kundens utgangspunkt følger 6-ukersoppdraget ett av fire spor:

        - Spor A: Prosessautomatisering & AI-agenter
        - Spor B: Dataintelligens & Beslutningsstøtte
        - Spor C: Ny AI-drevet applikasjon
        - Spor D: Intelligent oppgradering

        I praksis kombinerer et enkelt oppdrag ofte elementer på tvers av spor. Sporene hjelper oss å strukturere samtalen og sette riktige forventninger.

        ## Spor A: Prosessautomatisering & AI-agenter

        «Vi har manuelle prosesser som tar for lang tid, koster for mye, eller har for mange feil.»

        Vi automatiserer en eksisterende forretningsprosess ved å bygge AI-agenter som integrerer med kundens interne systemer. Agentene håndterer klassifisering, ruting, uttrekk eller generering — og mennesket forblir i loopen der det betyr noe.

        Typiske eksempler:
        - Hendelsesrapportering — Ansatte rapporterer hendelser via et enkelt skjema eller chat. En AI-agent klassifiserer alvorlighetsgrad, trekker ut strukturerte data, ruter til riktig avdeling, og skriver et rapportutkast.
        - Fakturabehandling — Innkommende fakturaer leses automatisk, matches mot innkjøpsordrer, flagges ved avvik, og rutes for godkjenning.
        - E-posttriagering — Kundehenvendelser klassifiseres etter hensikt, hastegrad vurderes, svarutkast genereres, og riktig team varsles.
        - Intern kunnskaps-Q&A — En agent besvarer ansattes spørsmål basert på intern dokumentasjon, retningslinjer og håndbøker.

        6-ukersplan for Spor A:
        - Uke 1: Avklar & oppsett — kartlegg nåværende prosess, definer suksesskriterier, provisjoner infra, tilgang til systemer.
        - Uke 2-3: Kjerneagent — klassifiseringsagent, datauttrekk, rutingslogikk, rapportutkast, test med eksempeldata.
        - Uke 4-5: Integrasjoner & polering — koble til sakssystem, e-post/Teams varsler, menneske-i-loopen, brukertesting.
        - Uke 6: Mål & beslutning — målt tidsbesparelse, feilrate sammenligning, brukertilbakemelding, go/no-go beslutning.

        Team: Tech Lead, 1–2 utviklere. Ingen dedikert designer nødvendig.
        Nøkkelrisiko: Tilgang til interne systemer (API-er, tillatelser). Må avklares i uke 1.

        ## Spor B: Dataintelligens & Beslutningsstøtte

        «Vi har data, men bruker den ikke godt nok. For mye manuelt arbeid, for få innsikter.»

        Vi bruker kundens eksisterende data til å redusere manuelt arbeid og gi AI-drevet beslutningsstøtte. For kunder som ikke har orden på dataene sine bygger vi en fokusert mini-dataplattform.

        Typiske eksempler:
        - Anbudsanalyse — Offentlige anbud skrapes, klassifiseres etter relevans, og matches mot selskapets kapabiliteter.
        - Kundeinnsiktsdashboard — Data fra CRM, supporthenvendelser og salg kombineres. AI identifiserer trender og churn-risiko.
        - Dokumentintelligens — Store volumer av kontrakter, rapporter eller retningslinjer indekseres og gjøres søkbare med AI.
        - Operasjonell analyse — Produksjons- eller logistikkdata struktureres og analyseres for flaskehalser og optimaliseringsmuligheter.

        6-ukersplan for Spor B:
        - Uke 1: Avklar & datakartlegging — kartlegg datakilder, vurder datakvalitet, definer hva «godt» betyr, provisjoner infra.
        - Uke 2-3: Datapipeline & indeksering — skraping/innlasting, vektorlager-oppsett, strukturering & metadata, test med ekte data.
        - Uke 4-5: AI-analyse & grensesnitt — AI-matching & rangering, dashboard/søke-UI, varsling, tilbakemeldingsløkke.
        - Uke 6: Mål & beslutning — tid spart, relevansnøyaktighet, go/no-go beslutning.

        Team: Tech Lead, 1–2 utviklere. Valgfri dataingeniør hvis datalandskapet er komplekst.

        Mini-dataplattformen: For kunder med data spredt på tvers av regneark, e-post og legacy-systemer. Vi avgrenser til én use case, leverer verdi umiddelbart, og designer for å skalere.

        ## Spor C: Ny AI-drevet applikasjon

        «Vi trenger en ny applikasjon — og AI skal være i kjernen.»

        Vi bygger en ny fullstack-applikasjon fra bunnen av, med AI-kapabiliteter dypt integrert. Dette er det mest omfattende sporet.

        Designtilnærming varierer:
        - Internt verktøy: Komponentbibliotek (shadcn/ui) — ingen designer nødvendig.
        - Kundevendt med eksisterende merkevare: Designer involvert 2–3 dager for nøkkelskjermer.
        - Helt nytt produkt: Designsystem-oppsett + designer gjennom hele løpet.

        Typiske eksempler:
        - Tale-til-tekst-applikasjon — App som tar opp, transkriberer og prosesserer taleinnput med AI.
        - AI-assistert anbudssvarverktøy — Konsulenter laster opp anbudsdokumenter, AI analyserer og lager svarutkast.
        - Intelligent kundeportal — Portal med AI-drevne svar og proaktive anbefalinger.

        6-ukersplan for Spor C:
        - Uke 1: Avklar & oppsett — scope & UX-workshop, design nøkkelskjermer, tech stack & infra, CI/CD-pipeline.
        - Uke 2-3: Kjerneapp & AI — frontend med nøkkelskjermer, AI-pipeline, API-lag, første demo.
        - Uke 4-5: Integrasjoner & finpuss — backend-integrasjoner, auth & roller, feilhåndtering, brukertesting.
        - Uke 6: Mål & beslutning — brukertilfredshet, prosesseringsnøyaktighet, tid per oppgave vs. baseline, go/no-go.

        Tech stack: React frontend, .NET backend, Azure OpenAI, Azure Container Apps, PostgreSQL.
        Team: Tech Lead, 1–2 utviklere, UX-designer.
        Nøkkelrisiko: Scope creep. MVP betyr MVP.

        ## Spor D: Intelligent oppgradering

        «Vi har en eksisterende applikasjon eller et system, og vi vil gjøre det smartere.»

        Vi tar en eksisterende applikasjon og legger til AI-kapabiliteter — intelligent søk, automatisert klassifisering, samtalebasert grensesnitt, eller AI-drevne anbefalinger.

        Typiske eksempler:
        - Legacy rapporteringsverktøy + AI — Naturlig språk-spørringer over eksisterende rapporteringssystem.
        - CRM + intelligente forslag — AI-drevne neste-beste-handling-forslag og churn-prediksjoner.
        - Intranett + AI-søk — Samtalebasert søk som forstår kontekst og returnerer svar.
        - Eksisterende app + moderne UX — Redesign med moderne grensesnitt og AI-funksjoner.

        6-ukersplan for Spor D:
        - Uke 1: Kartlegging & vurdering — revider eksisterende system, identifiser smertepunkter, definer omfang, baselinemål.
        - Uke 2-3: AI-kjerne & nytt grensesnitt — indekser data, bygg AI-søk/chatlag, redesign nøkkelskjermer, første demo.
        - Uke 4-5: Integrasjon & migrering — koble til eksisterende auth & data, migrer nøkkelflyter, side-ved-side testing.
        - Uke 6: Mål & beslutning — søkenøyaktighet, brukeradopsjon, oppgaver per minutt vs. baseline, go/no-go.

        Team: Tech Lead, 1–2 utviklere, UX-designer (deltid).

        ## Hvordan velge riktig spor

        - Kunden sier «Folkene våre bruker timer på X hver dag» → Spor A (Prosessautomatisering)
        - Kunden sier «Vi har masse data, men får ikke innsikt» → Spor B (Dataintelligens)
        - Kunden sier «Vi trenger et nytt verktøy/app for Y» → Spor C (Ny applikasjon)
        - Kunden sier «Nåværende system er utdatert» → Spor D (Intelligent oppgradering)
        - Kunden sier «Vi vet AI er viktig, men vet ikke hvor vi skal begynne» → Start med en Opportunity Sprint (2 uker)

        ## Salgsklare beskrivelser

        Heispitchen (30 sekunder):
        AI Accelerator gjør et reelt forretningsproblem om til en fungerende AI-løsning på 6 uker. Vi bygger i deres miljø, med deres data, og måler reell effekt. Etter 6 uker har dere en validert MVP og en tydelig beslutning: skalere, videreutvikle, eller stoppe. Ingen risiko for et årslangt prosjekt som aldri leverer.

        Verdiforslaget (for økonomiansvarlig):
        Tradisjonelle AI-prosjekter tar 6–12 måneder og koster 2–5 MNOK før noen vet om løsningen fungerer. AI Accelerator leverer et validert, målbart svar på 6 uker. Hvis det fungerer, har dere et fundament å skalere fra. Hvis ikke, har dere investert en brøkdel og har dataene til å forklare hvorfor.

        Den tekniske historien (for teknologiansvarlig):
        Vi deployer i deres sky (Azure, AWS eller GCP), bruker deres identitetsleverandør, og respekterer deres sikkerhetspolicyer. Arkitekturen er lagdelt og LLM-agnostisk. Vi starter fra en teknisk startpakke med infrastruktur-som-kode-maler, ferdige agentmønstre og integrasjonsakseleratorer.

        ## Håndtering av innvendinger

        - «6 uker er for kort» → Vi bygger den ene tingen som beviser verdien. Startpakken eliminerer uker med oppsett.
        - «Dataene våre er ikke klare» → Vi tilbyr en fokusert mini-dataplattform — avgrenser, strukturerer, og gjør nyttig nå.
        - «Hva hvis det ikke fungerer?» → 6 uker og et dokumentert svar er bedre enn 12 måneder uten svar.
        - «Vi vil bygge dette selv» → Alt vi bygger er deres. Vi får dere til validert utgangspunkt 10x raskere.
        - «Vi har allerede en AI-strategi» → AI Accelerator kompletterer strategi med gjennomføring.
        - «Vi er ikke klare til å sette opp AI-infrastruktur» → Forte hoster AI-modellene i løpet av de 6 ukene. Null oppsett for kunden.
        - «Hva med AI-kostnadene?» → Alle AI-brukskostnader dekkes av Forte i utviklingsperioden.
        - «Hva hvis vi ikke går videre?» → Dere beholder alt. Forte tilbyr gratis forvaltningsperiode for feilretting og justeringer.

        ## Etter de 6 ukene

        Tre muligheter: Skalere (flytt til pilot/produksjon), Videreutvikle (ny Accelerator-syklus), eller Stopp (dokumenterte grunner).

        ## Hva som er inkludert

        - AI-kostnader under utvikling dekkes av Forte (tokens, API-kall).
        - Forte-hostede AI-modeller — null oppsett for kunden. Enkel migrering etterpå.
        - Gratis forvaltning etter leveranse — feilretting og mindre justeringer.

        ## AI-overvåking (inkludert som standard)

        - Token-forbruk & kostnad per agent — sanntids kostnadsdashboard med varsler.
        - Responstid & gjennomstrømning — ytelsesmålinger per endepunkt.
        - Nøyaktighet & konfidens-score — kvalitetstrend-dashboard med terskelvarsler.
        - Tilbakemeldingsløkke — brukerkorrigeringer mates tilbake til prompt-optimalisering.

        ## AI-sikkerhet & databeskyttelse (inkludert som standard)

        - Prompt injection-beskyttelse
        - Innholdsfiltrering
        - Dataopphold i Azure Sweden Central (eller kundens foretrukne region)
        - PII-deteksjon og maskering
        - Full revisjonsspor
        - Rollebasert tilgangskontroll via Entra ID
        """;
}
