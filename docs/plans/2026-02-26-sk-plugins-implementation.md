# Semantic Kernel Plugins Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace the hardcoded knowledge block in the system prompt with 7 Semantic Kernel native plugins that the model calls on-demand via auto function calling.

**Architecture:** A single `FaiaAcceleratorPlugin` class with 7 `[KernelFunction]`-decorated methods, registered in a SK `Kernel`. The chat endpoint uses `FunctionChoiceBehavior.Auto()` (the current API — `ToolCallBehavior` is deprecated) so the model decides when to call plugins. Streaming SSE to the frontend is unchanged.

**Tech Stack:** .NET 8, Semantic Kernel 1.72.0, Azure OpenAI

**Design doc:** `docs/plans/2026-02-26-sk-plugins-design.md`

---

### Task 1: Create `FaiaAcceleratorPlugin` with `GetTrackDetails`

**Files:**
- Create: `api/FaiaChat.Api/Plugins/FaiaAcceleratorPlugin.cs`

**Step 1: Create the Plugins directory and file with the first plugin method**

```csharp
using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace FaiaChat.Api.Plugins;

public class FaiaAcceleratorPlugin
{
    [KernelFunction("GetTrackDetails")]
    [Description("Hent detaljert beskrivelse av et AI Accelerator-spor. Bruk denne når brukeren spør om et spesifikt spor eller du trenger å forklare hva et spor innebærer.")]
    public string GetTrackDetails(
        [Description("Spor-bokstav: A (Prosessautomatisering), B (Dataintelligens), C (Ny app), eller D (Intelligent oppgradering)")] string track)
    {
        return track.Trim().ToUpperInvariant() switch
        {
            "A" => TrackA,
            "B" => TrackB,
            "C" => TrackC,
            "D" => TrackD,
            _ => "Ukjent spor. Velg A (Prosessautomatisering), B (Dataintelligens), C (Ny app) eller D (Intelligent oppgradering)."
        };
    }

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
}
```

**Step 2: Verify it compiles**

Run: `cd api/FaiaChat.Api && dotnet build`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add api/FaiaChat.Api/Plugins/FaiaAcceleratorPlugin.cs
git commit -m "feat: add FaiaAcceleratorPlugin with GetTrackDetails"
```

---

### Task 2: Add `GetWeeklyPlan` to the plugin

**Files:**
- Modify: `api/FaiaChat.Api/Plugins/FaiaAcceleratorPlugin.cs`

**Step 1: Add the method and constants**

Add after `GetTrackDetails`:

```csharp
[KernelFunction("GetWeeklyPlan")]
[Description("Hent 6-ukersplanen for et spesifikt AI Accelerator-spor. Bruk denne når brukeren spør om hvordan de 6 ukene ser ut eller hva som skjer i hver uke.")]
public string GetWeeklyPlan(
    [Description("Spor-bokstav: A, B, C eller D")] string track)
{
    return track.Trim().ToUpperInvariant() switch
    {
        "A" => WeeklyPlanA,
        "B" => WeeklyPlanB,
        "C" => WeeklyPlanC,
        "D" => WeeklyPlanD,
        _ => "Ukjent spor. Velg A, B, C eller D."
    };
}
```

Add the constants:

```csharp
private const string WeeklyPlanA = """
    6-ukersplan for Spor A (Prosessautomatisering):
    - Uke 1: Avklar & oppsett — kartlegg nåværende prosess, definer suksesskriterier, provisjoner infra, tilgang til systemer.
    - Uke 2-3: Kjerneagent — klassifiseringsagent, datauttrekk, rutingslogikk, rapportutkast, test med eksempeldata.
    - Uke 4-5: Integrasjoner & polering — koble til sakssystem, e-post/Teams varsler, menneske-i-loopen, brukertesting.
    - Uke 6: Mål & beslutning — målt tidsbesparelse, feilrate sammenligning, brukertilbakemelding, go/no-go beslutning.
    """;

private const string WeeklyPlanB = """
    6-ukersplan for Spor B (Dataintelligens):
    - Uke 1: Avklar & datakartlegging — kartlegg datakilder, vurder datakvalitet, definer hva «godt» betyr, provisjoner infra.
    - Uke 2-3: Datapipeline & indeksering — skraping/innlasting, vektorlager-oppsett, strukturering & metadata, test med ekte data.
    - Uke 4-5: AI-analyse & grensesnitt — AI-matching & rangering, dashboard/søke-UI, varsling, tilbakemeldingsløkke.
    - Uke 6: Mål & beslutning — tid spart, relevansnøyaktighet, go/no-go beslutning.
    """;

private const string WeeklyPlanC = """
    6-ukersplan for Spor C (Ny AI-drevet applikasjon):
    - Uke 1: Avklar & oppsett — scope & UX-workshop, design nøkkelskjermer, tech stack & infra, CI/CD-pipeline.
    - Uke 2-3: Kjerneapp & AI — frontend med nøkkelskjermer, AI-pipeline, API-lag, første demo.
    - Uke 4-5: Integrasjoner & finpuss — backend-integrasjoner, auth & roller, feilhåndtering, brukertesting.
    - Uke 6: Mål & beslutning — brukertilfredshet, prosesseringsnøyaktighet, tid per oppgave vs. baseline, go/no-go.
    """;

private const string WeeklyPlanD = """
    6-ukersplan for Spor D (Intelligent oppgradering):
    - Uke 1: Kartlegging & vurdering — revider eksisterende system, identifiser smertepunkter, definer omfang, baselinemål.
    - Uke 2-3: AI-kjerne & nytt grensesnitt — indekser data, bygg AI-søk/chatlag, redesign nøkkelskjermer, første demo.
    - Uke 4-5: Integrasjon & migrering — koble til eksisterende auth & data, migrer nøkkelflyter, side-ved-side testing.
    - Uke 6: Mål & beslutning — søkenøyaktighet, brukeradopsjon, oppgaver per minutt vs. baseline, go/no-go.
    """;
```

**Step 2: Verify it compiles**

Run: `cd api/FaiaChat.Api && dotnet build`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add api/FaiaChat.Api/Plugins/FaiaAcceleratorPlugin.cs
git commit -m "feat: add GetWeeklyPlan to FaiaAcceleratorPlugin"
```

---

### Task 3: Add `GetSalesArguments` to the plugin

**Files:**
- Modify: `api/FaiaChat.Api/Plugins/FaiaAcceleratorPlugin.cs`

**Step 1: Add the method and constants**

```csharp
[KernelFunction("GetSalesArguments")]
[Description("Hent salgsargumenter tilpasset en bestemt målgruppe. Bruk denne når brukeren spør om verdi, kostnad, ROI, eller når du trenger å forklare AI Accelerator fra et forretningsmessig perspektiv.")]
public string GetSalesArguments(
    [Description("Målgruppe: 'elevator' (kort pitch), 'cfo' (økonomiansvarlig), eller 'cto' (teknologiansvarlig)")] string audience)
{
    return audience.Trim().ToLowerInvariant() switch
    {
        "elevator" => SalesElevator,
        "cfo" => SalesCfo,
        "cto" => SalesCto,
        _ => $"Ukjent målgruppe '{audience}'. Velg 'elevator', 'cfo' eller 'cto'."
    };
}
```

```csharp
private const string SalesElevator = """
    Heispitchen (30 sekunder):
    AI Accelerator gjør et reelt forretningsproblem om til en fungerende AI-løsning på 6 uker. Vi bygger i deres miljø, med deres data, og måler reell effekt. Etter 6 uker har dere en validert MVP og en tydelig beslutning: skalere, videreutvikle, eller stoppe. Ingen risiko for et årslangt prosjekt som aldri leverer.
    """;

private const string SalesCfo = """
    Verdiforslaget (for økonomiansvarlig):
    Tradisjonelle AI-prosjekter tar 6–12 måneder og koster 2–5 MNOK før noen vet om løsningen fungerer. AI Accelerator leverer et validert, målbart svar på 6 uker. Hvis det fungerer, har dere et fundament å skalere fra. Hvis ikke, har dere investert en brøkdel og har dataene til å forklare hvorfor.

    Dette vil ledere se med en gang:
    - Hva får vi her? En første fungerende løsning i deres miljø + målt effekt + beslutningsgrunnlag for skalering.
    - Kan dette hjelpe oss med økte kostnader og lav produktivitet? Ja, når use caset er valgt der det finnes målbare friksjoner eller volum.
    - Hva binder vi oss til? Modellen har faste go/no-go-punkt. Hvis forutsetninger ikke er på plass, stopper vi eller justerer scope.
    - Hvor lang tid? 6 uker, tidsbokset.
    - Hva skjer etterpå? Fungerende MVP, effektrapport og en plan: skaler, endre eller stopp.
    """;

private const string SalesCto = """
    Den tekniske historien (for teknologiansvarlig):
    Vi deployer i deres sky (Azure, AWS eller GCP), bruker deres identitetsleverandør, og respekterer deres sikkerhetspolicyer. Arkitekturen er lagdelt og LLM-agnostisk. Vi starter fra en teknisk startpakke med infrastruktur-som-kode-maler, ferdige agentmønstre og integrasjonsakseleratorer.

    Hvorfor dette går raskere enn vanlige prosjekter:
    1) Referansearkitektur (production-first) — velprøvd rammeverk som gjør løsningen driftbar og styrbar tidlig.
    2) Ferdigbygde komponenter og mønstre — gjenbrukbare byggesteiner for arbeidsflyt, kildetilgang, kvalitetstest/evaluering og kontrollmønstre.
    """;
```

**Step 2: Verify it compiles**

Run: `cd api/FaiaChat.Api && dotnet build`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add api/FaiaChat.Api/Plugins/FaiaAcceleratorPlugin.cs
git commit -m "feat: add GetSalesArguments to FaiaAcceleratorPlugin"
```

---

### Task 4: Add `GetObjectionResponse` to the plugin

**Files:**
- Modify: `api/FaiaChat.Api/Plugins/FaiaAcceleratorPlugin.cs`

**Step 1: Add the method and constant**

```csharp
[KernelFunction("GetObjectionResponse")]
[Description("Hent svar på en vanlig innvending mot AI Accelerator. Bruk denne når brukeren uttrykker skepsis, bekymring eller innvendinger som tid, kost, data-readiness, eller risiko.")]
public string GetObjectionResponse(
    [Description("Stikkord for innvendingen, f.eks. 'tid', 'kort', 'data', 'kost', 'risiko', 'selv', 'strategi', 'infrastruktur', 'videre'")] string topic)
{
    var key = topic.Trim().ToLowerInvariant();

    foreach (var (keywords, response) in Objections)
    {
        if (keywords.Any(k => key.Contains(k)))
            return response;
    }

    return ObjectionsAll;
}

private static readonly (string[] Keywords, string Response)[] Objections =
[
    (["kort", "tid", "6 uke", "seks uke", "for lite tid"], "«6 uker er for kort» — Vi bygger den ene tingen som beviser verdien. Startpakken eliminerer uker med oppsett."),
    (["data", "klar", "kvalitet", "ryddig"], "«Dataene våre er ikke klare» — Vi tilbyr en fokusert mini-dataplattform — avgrenser, strukturerer, og gjør nyttig nå."),
    (["fungere", "feil", "risiko", "garanti"], "«Hva hvis det ikke fungerer?» — 6 uker og et dokumentert svar er bedre enn 12 måneder uten svar. Modellen har faste go/no-go-punkt — dere kan stoppe eller justere underveis."),
    (["selv", "egen", "internt", "bygge"], "«Vi vil bygge dette selv» — Alt vi bygger er deres. Vi får dere til validert utgangspunkt 10x raskere."),
    (["strategi", "allerede", "plan"], "«Vi har allerede en AI-strategi» — AI Accelerator kompletterer strategi med gjennomføring."),
    (["infra", "oppsett", "miljø"], "«Vi er ikke klare til å sette opp AI-infrastruktur» — Forte hoster AI-modellene i løpet av de 6 ukene. Null oppsett for kunden."),
    (["kost", "pris", "dyr", "budsjett", "penger"], "«Hva med AI-kostnadene?» — Alle AI-brukskostnader dekkes av Forte i utviklingsperioden. I løpet av 6 uker tydeliggjør vi også driftskost og kostdrivere ved videre bruk."),
    (["videre", "etterpå", "stopp", "avslut"], "«Hva hvis vi ikke går videre?» — Dere beholder alt. Forte tilbyr gratis forvaltningsperiode for feilretting og justeringer.")
];

private const string ObjectionsAll = """
    Vanlige innvendinger og svar:
    - «6 uker er for kort» — Vi bygger den ene tingen som beviser verdien. Startpakken eliminerer uker med oppsett.
    - «Dataene våre er ikke klare» — Vi tilbyr en fokusert mini-dataplattform — avgrenser, strukturerer, og gjør nyttig nå.
    - «Hva hvis det ikke fungerer?» — 6 uker og et dokumentert svar er bedre enn 12 måneder uten svar.
    - «Vi vil bygge dette selv» — Alt vi bygger er deres. Vi får dere til validert utgangspunkt 10x raskere.
    - «Vi har allerede en AI-strategi» — AI Accelerator kompletterer strategi med gjennomføring.
    - «Vi er ikke klare til å sette opp AI-infrastruktur» — Forte hoster AI-modellene i løpet av de 6 ukene.
    - «Hva med AI-kostnadene?» — Alle AI-brukskostnader dekkes av Forte i utviklingsperioden.
    - «Hva hvis vi ikke går videre?» — Dere beholder alt. Forte tilbyr gratis forvaltningsperiode.
    """;
```

**Step 2: Verify it compiles**

Run: `cd api/FaiaChat.Api && dotnet build`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add api/FaiaChat.Api/Plugins/FaiaAcceleratorPlugin.cs
git commit -m "feat: add GetObjectionResponse to FaiaAcceleratorPlugin"
```

---

### Task 5: Add `GetCaseExamples`, `GetSecurityInfo`, `GetPricingInfo` to the plugin

**Files:**
- Modify: `api/FaiaChat.Api/Plugins/FaiaAcceleratorPlugin.cs`

**Step 1: Add the three parameterless methods and their constants**

```csharp
[KernelFunction("GetCaseExamples")]
[Description("Hent anonymiserte eksempler på AI Accelerator-leveranser og hva man kan forvente i en effektrapport. Bruk denne når brukeren spør om referanser, eksempler, eller hva andre har fått ut av det.")]
public string GetCaseExamples() => CaseExamples;

[KernelFunction("GetSecurityInfo")]
[Description("Hent informasjon om sikkerhet, compliance, tillit og beslutningskontroll i AI Accelerator. Bruk denne når brukeren spør om datasikkerhet, personvern, kontroll, risiko, eller governance.")]
public string GetSecurityInfo() => SecurityInfo;

[KernelFunction("GetPricingInfo")]
[Description("Hent informasjon om hva som er inkludert, kostnadsmodell, hva som skjer etter 6 uker, og neste steg. Bruk denne når brukeren spør om pris, hva som er inkludert, oppfølging, eller hvordan man kommer i gang.")]
public string GetPricingInfo() => PricingInfo;
```

```csharp
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
```

**Step 2: Verify it compiles**

Run: `cd api/FaiaChat.Api && dotnet build`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add api/FaiaChat.Api/Plugins/FaiaAcceleratorPlugin.cs
git commit -m "feat: add GetCaseExamples, GetSecurityInfo, GetPricingInfo plugins"
```

---

### Task 6: Update `SystemPromptBuilder` — replace content with slim prompt

**Files:**
- Modify: `api/FaiaChat.Api/Services/SystemPromptBuilder.cs`

**Step 1: Replace the entire file content**

The new `SystemPromptBuilder` removes the `Content` constant and replaces the prompt with behavioral rules only. Overordnet posisjonering is included as brief context.

```csharp
namespace FaiaChat.Api.Services;

public class SystemPromptBuilder
{
    public Task<string> BuildAsync()
    {
        return Task.FromResult(Prompt);
    }

    private const string Prompt = """
        Du er FAIA, en erfaren rådgiver hos Forte som hjelper potensielle kunder å forstå AI Accelerator — en 6-ukers leveransemodell som tar ett tydelig avgrenset forretningsproblem til en første fungerende løsning (MVP) i kundens eget miljø, med målt effekt og et tydelig go/no-go for videre satsing.

        Bakgrunn du kjenner til:
        Mange organisasjoner sitter fast i POC-fella — demoer som ser bra ut men ikke tåler reelle brukere. AI Accelerator løser dette med stram avgrensning, effektmåling og produksjonsnær arkitektur fra dag 1. Tempoet kommer fra en referansearkitektur og ferdigbygde komponenter — ikke snarveier. Vi unngår bevisst å bygge før baseline er avklart, magiske demoer uten plan, og over-automatisering der konsekvensen av feil er høy.

        Slik oppfører du deg:
        - Du svarer ALLTID med maks 2-3 korte setninger. Aldri mer.
        - Du avslutter ALLTID svaret med ett relevant oppfølgingsspørsmål for å forstå brukerens situasjon bedre.
        - Du skriver som i en uformell chat — ingen lister, ingen overskrifter, ingen markdown-formatering, ingen punktlister, ingen nummererte punkter. Bare vanlige setninger.
        - Du er en aktiv guide som styrer samtalen. Still spørsmål for å identifisere riktig spor (A: Prosessautomatisering, B: Dataintelligens, C: Ny app, D: Oppgradering).
        - Bruk verktøyene dine til å slå opp detaljer når du trenger dem — ikke gjett.
        - Når du har forstått brukerens behov og identifisert riktig spor, oppsummer kort og avslutt med: "Vil du ta en prat med teamet? Book et møte her: https://forte.no/kontakt"
        - Etter at du har foreslått å booke møte, si noe hyggelig som avslutning. IKKE fortsett å stille spørsmål. Samtalen er ferdig.
        - Du kan IKKE booke møter, sende e-post, sende kalenderinvitasjoner, eller gjøre noe utenfor denne chatten. Du er kun en informasjonsassistent. Aldri lat som om du kan utføre handlinger.
        - Aldri be om personlig informasjon som e-post, telefonnummer eller navn.
        - Svar kun basert på det du finner via verktøyene dine. Hvis du ikke vet, si det ærlig og tilby å koble brukeren med teamet via lenken over.
        - Svar på norsk. Varm og direkte tone, som en erfaren konsulent.
        - Ikke bruk emojier.
        """;
}
```

**Step 2: Verify it compiles**

Run: `cd api/FaiaChat.Api && dotnet build`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add api/FaiaChat.Api/Services/SystemPromptBuilder.cs
git commit -m "refactor: replace hardcoded content with slim behavioral system prompt"
```

---

### Task 7: Update `Program.cs` — wire up Kernel, plugin, and auto function calling

**Files:**
- Modify: `api/FaiaChat.Api/Program.cs`

**Step 1: Add using statement**

Add at the top of the file:

```csharp
using FaiaChat.Api.Plugins;
```

**Step 2: Replace the Azure OpenAI registration with Kernel registration**

Replace this block:

```csharp
builder.Services.AddAzureOpenAIChatCompletion(deploymentName, endpoint, apiKey);
```

With:

```csharp
builder.Services.AddKernel();
builder.Services.AddAzureOpenAIChatCompletion(deploymentName, endpoint, apiKey);
```

**Step 3: Register the plugin after `builder.Build()`**

After `var app = builder.Build();`, add:

```csharp
// Register Semantic Kernel plugin
var kernel = app.Services.CreateScope().ServiceProvider.GetRequiredService<Kernel>();
kernel.Plugins.AddFromType<FaiaAcceleratorPlugin>();
```

Wait — `Kernel` is scoped, so we need a different approach. Register the plugin type in DI instead:

Replace the above with adding the plugin to the kernel builder:

```csharp
builder.Services.AddKernel();
builder.Services.AddAzureOpenAIChatCompletion(deploymentName, endpoint, apiKey);
builder.Services.AddSingleton<FaiaAcceleratorPlugin>();
```

And in the endpoint, after getting the kernel, register the plugin:

Actually, the cleanest approach: use the `KernelPluginFactory` at the DI level. Replace the entire Azure OpenAI + Kernel setup block with:

```csharp
var kernelBuilder = builder.Services.AddKernel();
kernelBuilder.AddAzureOpenAIChatCompletion(deploymentName, endpoint, apiKey);
kernelBuilder.Plugins.AddFromType<FaiaAcceleratorPlugin>();
```

This registers the plugin once so every `Kernel` instance has it.

**Step 4: Update the endpoint signature**

Change the endpoint to inject `Kernel` alongside the existing services:

```csharp
app.MapPost("/api/chat", async (ChatRequest request, Kernel kernel, SystemPromptBuilder promptBuilder, HttpContext context) =>
```

**Step 5: Update execution settings**

Replace the current `PromptExecutionSettings` block:

```csharp
var executionSettings = new PromptExecutionSettings
{
    ExtensionData = new Dictionary<string, object>
    {
        ["temperature"] = 0.7,
        ["max_tokens"] = 400,
        ["top_p"] = 0.9
    }
};
```

With:

```csharp
var executionSettings = new PromptExecutionSettings
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
    ExtensionData = new Dictionary<string, object>
    {
        ["temperature"] = 0.7,
        ["max_tokens"] = 400,
        ["top_p"] = 0.9
    }
};
```

**Step 6: Update the streaming call**

Replace:

```csharp
await foreach (var chunk in chatService.GetStreamingChatMessageContentsAsync(chatHistory, executionSettings, kernel: null, context.RequestAborted))
```

With:

```csharp
var chatService = kernel.GetRequiredService<IChatCompletionService>();
await foreach (var chunk in chatService.GetStreamingChatMessageContentsAsync(chatHistory, executionSettings, kernel, context.RequestAborted))
```

Remove `IChatCompletionService chatService` from the endpoint parameter list (we get it from kernel now).

**Step 7: Verify it compiles**

Run: `cd api/FaiaChat.Api && dotnet build`
Expected: Build succeeded

**Step 8: Commit**

```bash
git add api/FaiaChat.Api/Program.cs
git commit -m "feat: wire up SK Kernel with auto function calling and FaiaAcceleratorPlugin"
```

---

### Task 8: Manual smoke test

**Step 1: Start the API**

Run: `cd api/FaiaChat.Api && dotnet run`
Expected: API starts on configured port (check `launchSettings.json`)

**Step 2: Test basic chat**

```bash
curl -X POST http://localhost:5000/api/chat \
  -H "Content-Type: application/json" \
  -d '{"messages":[{"role":"user","content":"Hei, vi bruker mye tid på manuell fakturabehandling. Kan AI hjelpe?"}]}' \
  --no-buffer
```

Expected: SSE stream with a response. The model should internally call `GetTrackDetails("A")` or similar, but the response should look like a normal chat message (2-3 sentences, follow-up question, Norwegian).

**Step 3: Test track-specific question**

```bash
curl -X POST http://localhost:5000/api/chat \
  -H "Content-Type: application/json" \
  -d '{"messages":[{"role":"user","content":"Hva er spor B?"}]}' \
  --no-buffer
```

Expected: Response informed by `GetTrackDetails("B")` content.

**Step 4: Test objection handling**

```bash
curl -X POST http://localhost:5000/api/chat \
  -H "Content-Type: application/json" \
  -d '{"messages":[{"role":"user","content":"6 uker høres altfor kort ut."}]}' \
  --no-buffer
```

Expected: Response informed by `GetObjectionResponse` content about timing.

**Step 5: Commit (no code changes — just verification)**

No commit needed. If issues found, fix and commit fixes.

---

### Task 9: Run eval suite

**Step 1: Start the API (if not already running)**

Run: `cd api/FaiaChat.Api && dotnet run`

**Step 2: Run evals in separate terminal**

Run: `cd api/FaiaChat.Evals && dotnet run`

Expected: Eval suite runs all 8 personas. Check for:
- Deterministic checks still passing (max 3 sentences, no markdown, etc.)
- LLM judge scores >= 3 on all dimensions
- No regressions compared to previous runs

**Step 3: If evals fail, investigate and fix**

Common issues to watch for:
- Model may return longer responses if function call results are verbose — may need to adjust `max_tokens`
- Model may include markdown if function results contain markdown formatting
- Streaming behavior may differ with function calling enabled

**Step 4: Commit any fixes**

```bash
git add -A
git commit -m "fix: address eval regressions after SK plugin migration"
```

---

### Task 10: Update design doc with API change

**Files:**
- Modify: `docs/plans/2026-02-26-sk-plugins-design.md`

**Step 1: Update the Kernel-oppsett section**

Replace the reference to `ToolCallBehavior.AutoInvokeKernelFunctions` with `FunctionChoiceBehavior.Auto()`. Note that `ToolCallBehavior` is deprecated in SK 1.72.0.

**Step 2: Commit**

```bash
git add docs/plans/2026-02-26-sk-plugins-design.md
git commit -m "docs: update design doc with correct SK API (FunctionChoiceBehavior)"
```
