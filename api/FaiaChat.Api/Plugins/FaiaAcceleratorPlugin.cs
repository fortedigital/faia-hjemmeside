using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace FaiaChat.Api.Plugins;

public class FaiaAcceleratorPlugin
{
    #region Track Details

    private const string TrackA = """
        Spor A: Prosessautomatisering & AI-agenter

        «Vi har manuelle prosesser som tar for lang tid, koster for mye, eller har for mange feil.»

        Vi automatiserer en eksisterende forretningsprosess ved å bygge AI-agenter som integrerer med kundens interne systemer. Agentene håndterer klassifisering, ruting, uttrekk eller generering — og mennesket forblir i loopen der det betyr noe.

        Typiske eksempler:
        - Hendelsesrapportering — Ansatte rapporterer hendelser via et enkelt skjema eller chat. En AI-agent klassifiserer alvorlighetsgrad, trekker ut strukturerte data, ruter til riktig avdeling, og skriver et rapportutkast.
        - Fakturabehandling — Innkommende fakturaer leses automatisk, matches mot innkjøpsordrer, flagges ved avvik, og rutes for godkjenning.
        - E-posttriagering — Kundehenvendelser klassifiseres etter hensikt, hastegrad vurderes, svarutkast genereres, og riktig team varsles.
        - Intern kunnskaps-Q&A — En agent besvarer ansattes spørsmål basert på intern dokumentasjon, retningslinjer og håndbøker.

        Team: Tech Lead, 1–2 utviklere. Ingen dedikert designer nødvendig.
        Nøkkelrisiko: Tilgang til interne systemer (API-er, tillatelser). Må avklares i uke 1.
        """;

    private const string TrackB = """
        Spor B: Dataintelligens & Beslutningsstøtte

        «Vi har data, men bruker den ikke godt nok. For mye manuelt arbeid, for få innsikter.»

        Vi bruker kundens eksisterende data til å redusere manuelt arbeid og gi AI-drevet beslutningsstøtte. For kunder som ikke har orden på dataene sine bygger vi en fokusert mini-dataplattform.

        Typiske eksempler:
        - Anbudsanalyse — Offentlige anbud skrapes, klassifiseres etter relevans, og matches mot selskapets kapabiliteter.
        - Kundeinnsiktsdashboard — Data fra CRM, supporthenvendelser og salg kombineres. AI identifiserer trender og churn-risiko.
        - Dokumentintelligens — Store volumer av kontrakter, rapporter eller retningslinjer indekseres og gjøres søkbare med AI.
        - Operasjonell analyse — Produksjons- eller logistikkdata struktureres og analyseres for flaskehalser og optimaliseringsmuligheter.

        Team: Tech Lead, 1–2 utviklere. Valgfri dataingeniør hvis datalandskapet er komplekst.
        """;

    private const string TrackC = """
        Spor C: Ny AI-drevet applikasjon

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

        Tech stack: React frontend, .NET backend, Azure OpenAI, Azure Container Apps, PostgreSQL.
        Team: Tech Lead, 1–2 utviklere, UX-designer.
        Nøkkelrisiko: Scope creep. MVP betyr MVP.
        """;

    private const string TrackD = """
        Spor D: Intelligent oppgradering

        «Vi har en eksisterende applikasjon eller et system, og vi vil gjøre det smartere.»

        Vi tar en eksisterende applikasjon og legger til AI-kapabiliteter — intelligent søk, automatisert klassifisering, samtalebasert grensesnitt, eller AI-drevne anbefalinger.

        Typiske eksempler:
        - Legacy rapporteringsverktøy + AI — Naturlig språk-spørringer over eksisterende rapporteringssystem.
        - CRM + intelligente forslag — AI-drevne neste-beste-handling-forslag og churn-prediksjoner.
        - Intranett + AI-søk — Samtalebasert søk som forstår kontekst og returnerer svar.
        - Eksisterende app + moderne UX — Redesign med moderne grensesnitt og AI-funksjoner.

        Team: Tech Lead, 1–2 utviklere, UX-designer (deltid).
        """;

    #endregion

    #region Weekly Plans

    private const string PlanA = """
        6-ukersplan for Spor A (Prosessautomatisering):
        - Uke 1: Avklar & oppsett — kartlegg nåværende prosess, definer suksesskriterier, provisjoner infra, tilgang til systemer.
        - Uke 2-3: Kjerneagent — klassifiseringsagent, datauttrekk, rutingslogikk, rapportutkast, test med eksempeldata.
        - Uke 4-5: Integrasjoner & polering — koble til sakssystem, e-post/Teams varsler, menneske-i-loopen, brukertesting.
        - Uke 6: Mål & beslutning — målt tidsbesparelse, feilrate sammenligning, brukertilbakemelding, go/no-go beslutning.
        """;

    private const string PlanB = """
        6-ukersplan for Spor B (Dataintelligens):
        - Uke 1: Avklar & datakartlegging — kartlegg datakilder, vurder datakvalitet, definer hva «godt» betyr, provisjoner infra.
        - Uke 2-3: Datapipeline & indeksering — skraping/innlasting, vektorlager-oppsett, strukturering & metadata, test med ekte data.
        - Uke 4-5: AI-analyse & grensesnitt — AI-matching & rangering, dashboard/søke-UI, varsling, tilbakemeldingsløkke.
        - Uke 6: Mål & beslutning — tid spart, relevansnøyaktighet, go/no-go beslutning.
        """;

    private const string PlanC = """
        6-ukersplan for Spor C (Ny AI-drevet applikasjon):
        - Uke 1: Avklar & oppsett — scope & UX-workshop, design nøkkelskjermer, tech stack & infra, CI/CD-pipeline.
        - Uke 2-3: Kjerneapp & AI — frontend med nøkkelskjermer, AI-pipeline, API-lag, første demo.
        - Uke 4-5: Integrasjoner & finpuss — backend-integrasjoner, auth & roller, feilhåndtering, brukertesting.
        - Uke 6: Mål & beslutning — brukertilfredshet, prosesseringsnøyaktighet, tid per oppgave vs. baseline, go/no-go.
        """;

    private const string PlanD = """
        6-ukersplan for Spor D (Intelligent oppgradering):
        - Uke 1: Kartlegging & vurdering — revider eksisterende system, identifiser smertepunkter, definer omfang, baselinemål.
        - Uke 2-3: AI-kjerne & nytt grensesnitt — indekser data, bygg AI-søk/chatlag, redesign nøkkelskjermer, første demo.
        - Uke 4-5: Integrasjon & migrering — koble til eksisterende auth & data, migrer nøkkelflyter, side-ved-side testing.
        - Uke 6: Mål & beslutning — søkenøyaktighet, brukeradopsjon, oppgaver per minutt vs. baseline, go/no-go.
        """;

    #endregion

    #region Sales Arguments

    private const string ElevatorPitch = """
        Heispitchen (30 sekunder):
        AI Accelerator gjør et reelt forretningsproblem om til en fungerende AI-løsning på 6 uker. Vi bygger i deres miljø, med deres data, og måler reell effekt. Etter 6 uker har dere en validert MVP og en tydelig beslutning: skalere, videreutvikle, eller stoppe. Ingen risiko for et årslangt prosjekt som aldri leverer.
        """;

    private const string CfoArgument = """
        Verdiforslaget (for økonomiansvarlig):
        Tradisjonelle AI-prosjekter tar 6–12 måneder og koster 2–5 MNOK før noen vet om løsningen fungerer. AI Accelerator leverer et validert, målbart svar på 6 uker. Hvis det fungerer, har dere et fundament å skalere fra. Hvis ikke, har dere investert en brøkdel og har dataene til å forklare hvorfor.

        Dette vil ledere se med en gang:
        - Hva får vi her? En første fungerende løsning i deres miljø + målt effekt + beslutningsgrunnlag for skalering.
        - Kan dette hjelpe oss med økte kostnader og lav produktivitet? Ja, når use caset er valgt der det finnes målbare friksjoner eller volum.
        - Hva binder vi oss til? Modellen har faste go/no-go-punkt. Hvis forutsetninger ikke er på plass, stopper vi eller justerer scope.
        - Hvor lang tid? 6 uker, tidsbokset.
        - Hva skjer etterpå? Fungerende MVP, effektrapport og en plan: skaler, endre eller stopp.
        """;

    private const string CtoArgument = """
        Den tekniske historien (for teknologiansvarlig):
        Vi deployer i deres sky (Azure, AWS eller GCP), bruker deres identitetsleverandør, og respekterer deres sikkerhetspolicyer. Arkitekturen er lagdelt og LLM-agnostisk. Vi starter fra en teknisk startpakke med infrastruktur-som-kode-maler, ferdige agentmønstre og integrasjonsakseleratorer.

        Hvorfor dette går raskere enn vanlige prosjekter:
        1) Referansearkitektur (production-first) — velprøvd rammeverk som gjør løsningen driftbar og styrbar tidlig.
        2) Ferdigbygde komponenter og mønstre — gjenbrukbare byggesteiner for arbeidsflyt, kildetilgang, kvalitetstest/evaluering og kontrollmønstre.
        """;

    #endregion

    #region Objections

    private static readonly (string[] Keywords, string Response)[] Objections =
    [
        (["kort", "tid", "6 uke", "seks uke", "for lite tid"],
            "«6 uker er for kort» — Vi bygger den ene tingen som beviser verdien. Startpakken eliminerer uker med oppsett."),

        (["data", "klar", "kvalitet", "ryddig"],
            "«Dataene våre er ikke klare» — Vi tilbyr en fokusert mini-dataplattform — avgrenser, strukturerer, og gjør nyttig nå."),

        (["fungere", "feil", "risiko", "garanti"],
            "«Hva hvis det ikke fungerer?» — 6 uker og et dokumentert svar er bedre enn 12 måneder uten svar. Modellen har faste go/no-go-punkt — dere kan stoppe eller justere underveis."),

        (["selv", "egen", "internt", "bygge"],
            "«Vi vil bygge dette selv» — Alt vi bygger er deres. Vi får dere til validert utgangspunkt 10x raskere."),

        (["strategi", "allerede", "plan"],
            "«Vi har allerede en AI-strategi» — AI Accelerator kompletterer strategi med gjennomføring."),

        (["infra", "oppsett", "miljø"],
            "«Vi er ikke klare til å sette opp AI-infrastruktur» — Forte hoster AI-modellene i løpet av de 6 ukene. Null oppsett for kunden."),

        (["kost", "pris", "dyr", "budsjett", "penger"],
            "«Hva med AI-kostnadene?» — Alle AI-brukskostnader dekkes av Forte i utviklingsperioden. I løpet av 6 uker tydeliggjør vi også driftskost og kostdrivere ved videre bruk."),

        (["videre", "etterpå", "stopp", "avslut"],
            "«Hva hvis vi ikke går videre?» — Dere beholder alt. Forte tilbyr gratis forvaltningsperiode for feilretting og justeringer.")
    ];

    #endregion

    #region Case Examples

    private const string CaseExamples = """
        Mini-caser (anonymiserte eksempler):
        - Kundeservice: utkast til svar med kildelenker og tydelig eskalering — kortere behandlingstid, mer konsistente svar.
        - Intern kunnskapsstøtte: søk og oppsummering på tvers av policy/dokumentasjon — færre avbrudd, raskere avklaringer.
        - Operativ planlegging: forslag til prioritering basert på historikk — bedre beslutningsstøtte og sporbarhet.

        Hva du kan forvente i en effektrapport:
        - Baseline vs. etter 6 uker (KPI, kvalitet, adopsjon)
        - Hva som ble bygget — og hva som beviselig virker
        - Kostdrivere og anbefalte styringsgrep ved videre utrulling
        - Risikovurdering og anbefalt kontrollnivå for skalering
        """;

    #endregion

    #region Security Info

    private const string SecurityInfo = """
        Sikkerhet og compliance:
        - Tilgangsstyring og tydelige grenser (roller, API, secrets)
        - Sporbarhet der det trengs (etterprøvbarhet)
        - Tiltak mot hallusinasjon der relevant (kildekrav, verifisering, human-in-the-loop)
        - Policy for hvilke data som kan sendes til hvilke modeller/leverandører

        Tillit og risikohåndtering:
        - Testing mot realistiske data: faktiske caser og definisjon av hva som er «riktig»
        - Løpende evaluering: kvalitet og robusthet måles og forbedres
        - Kildekrav/verifisering der det er relevant
        - Menneskelig kontroll der konsekvensen av feil er høy

        Beslutningskontroll underveis (go/no-go):
        - Etter uke 1: Enighet om mål, baseline og risikonivå. Uten dette bygger vi ikke.
        - Etter uke 2: Enighet om integrasjon, arkitekturvalg og kostrammer. Uten dette går vi ikke videre med full pilot.
        - Etter uke 5: Pilot viser målbar effekt og akseptabel risiko/kost. Uten dette anbefaler vi ikke bred utrulling.
        """;

    #endregion

    #region Pricing Info

    private const string PricingInfo = """
        Hva som er inkludert:
        - AI-kostnader under utvikling dekkes av Forte (tokens, API-kall)
        - Forte-hostede AI-modeller — null oppsett for kunden, enkel migrering etterpå
        - Gratis forvaltning etter leveranse — feilretting og mindre justeringer

        Økonomisk kontroll:
        - Måle/estimere kost per sak/volum (på MVP-nivå)
        - Begrensninger og smarte tiltak for forbruk (rate limiting, caching, riktig kontekst)
        - Modellvalg tilpasset oppgave (ikke «størst mulig modell» overalt)

        Hva dere får etter 6 uker:
        - Første fungerende løsning (MVP) i deres miljø
        - Effektmåling med baseline, KPI-er og dokumentert resultat
        - Validert fundament: integrasjoner og arkitekturvalg som kan bygges videre på
        - Kost- og risikobilde (hva driver kost og hvilke kontroller som trengs)
        - Go/no-go + skaleringsplan: anbefalt retning og kort roadmap

        Tre veier videre: Skalere (flytt til pilot/produksjon), Videreutvikle (ny Accelerator-syklus), eller Stopp (dokumenterte grunner).

        Neste steg — 60–90 minutter avklaringssamtale:
        - Anbefalt scope (riktig første use case)
        - Forslag til KPI-er og baseline-oppsett
        - Risikobilde (data, sikkerhet og nødvendige kontroller)
        - Grov gjennomføringsplan for 6 uker

        Hvem bør delta: prosesseier, IT/arkitekt, data-/plattformansvarlig, sikkerhet ved behov, økonomi/controlling hvis kostprofil er kritisk.
        """;

    #endregion

    [KernelFunction("GetTrackDetails")]
    [Description("Hent detaljert beskrivelse av et AI Accelerator-spor. Bruk denne når brukeren spør om hva et spesifikt spor innebærer, hvilke eksempler som finnes, teamsammensetning eller risiko.")]
    public string GetTrackDetails(
        [Description("Spor-bokstav: «A» (prosessautomatisering), «B» (dataintelligens), «C» (ny app), eller «D» (intelligent oppgradering)")] string track)
    {
        return track.Trim().ToUpperInvariant() switch
        {
            "A" => TrackA,
            "B" => TrackB,
            "C" => TrackC,
            "D" => TrackD,
            _ => $"Ukjent spor «{track}». Gyldige verdier er A, B, C eller D."
        };
    }

    [KernelFunction("GetWeeklyPlan")]
    [Description("Hent 6-ukersplanen for et spesifikt spor. Bruk denne når brukeren spør om tidsplan, ukefordeling, hva som skjer i hvilken uke, eller hvordan de 6 ukene er strukturert.")]
    public string GetWeeklyPlan(
        [Description("Spor-bokstav: «A» (prosessautomatisering), «B» (dataintelligens), «C» (ny app), eller «D» (intelligent oppgradering)")] string track)
    {
        return track.Trim().ToUpperInvariant() switch
        {
            "A" => PlanA,
            "B" => PlanB,
            "C" => PlanC,
            "D" => PlanD,
            _ => $"Ukjent spor «{track}». Gyldige verdier er A, B, C eller D."
        };
    }

    [KernelFunction("GetSalesArguments")]
    [Description("Hent salgsargumenter tilpasset en bestemt målgruppe. Bruk denne når brukeren spør om hvordan man pitcher AI Accelerator, verdiforslaget for ledelse/økonomi, eller den tekniske historien for IT.")]
    public string GetSalesArguments(
        [Description("Målgruppe: «elevator» (30-sekunders heispitch), «cfo» (økonomiansvarlig/ledelse), eller «cto» (teknologiansvarlig/IT)")] string audience)
    {
        return audience.Trim().ToLowerInvariant() switch
        {
            "elevator" => ElevatorPitch,
            "cfo" => CfoArgument,
            "cto" => CtoArgument,
            _ => $"Ukjent målgruppe «{audience}». Gyldige verdier er elevator, cfo eller cto."
        };
    }

    [KernelFunction("GetObjectionResponse")]
    [Description("Hent svar på en innvending eller bekymring fra kunden. Bruk denne når brukeren nevner en innvending, tvil eller bekymring om AI Accelerator — for eksempel om tid, kostnader, datakvalitet, risiko, eller om de vil bygge selv.")]
    public string GetObjectionResponse(
        [Description("Nøkkelord eller kort beskrivelse av innvendingen, f.eks. «for kort tid», «datakvalitet», «risiko», «bygge selv», «kostnad», «hva skjer etterpå»")] string topic)
    {
        var lowerTopic = topic.Trim().ToLowerInvariant();

        foreach (var (keywords, response) in Objections)
        {
            foreach (var keyword in keywords)
            {
                if (lowerTopic.Contains(keyword))
                {
                    return response;
                }
            }
        }

        // Fallback: return all objections
        return string.Join("\n\n", Objections.Select(o => o.Response));
    }

    [KernelFunction("GetCaseExamples")]
    [Description("Hent anonymiserte mini-caser og eksempler på effektrapporter. Bruk denne når brukeren spør om referanser, eksempler, hva andre har gjort, eller hva man kan forvente av resultater og effektmåling.")]
    public string GetCaseExamples()
    {
        return CaseExamples;
    }

    [KernelFunction("GetSecurityInfo")]
    [Description("Hent informasjon om sikkerhet, compliance, tillit og go/no-go beslutningskontroll. Bruk denne når brukeren spør om sikkerhet, personvern, hallusinasjon, risikohåndtering, eller beslutningspunkter underveis i prosjektet.")]
    public string GetSecurityInfo()
    {
        return SecurityInfo;
    }

    [KernelFunction("GetPricingInfo")]
    [Description("Hent informasjon om hva som er inkludert, kostnadsmodell, hva som skjer etter 6 uker, og neste steg. Bruk denne når brukeren spør om pris, hva som er inkludert, AI-kostnader, hva man får levert, eller hvordan man kommer i gang.")]
    public string GetPricingInfo()
    {
        return PricingInfo;
    }
}
