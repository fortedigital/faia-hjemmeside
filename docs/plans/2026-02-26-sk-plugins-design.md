# Design: Semantic Kernel Plugins for FAIA Chat

**Dato:** 2026-02-26

## Mål

Erstatte den hardkodede kunnskapsblokken i system-prompten med Semantic Kernel native plugins (function calling). Modellen henter relevant informasjon on-demand via 7 plugins i stedet for å motta alt innhold i prompten.

Dobbelt formål: lære SK plugin-mønsteret, og forbedre chatresponsene ved at kun relevant kunnskap hentes.

## Tilnærming

**SK Native Plugins med Auto Function Calling.** En C#-klasse med `[KernelFunction]`-dekorerte metoder registreres i en SK `Kernel`. Modellen kaller plugins selv via `FunctionChoiceBehavior.Auto()` (den nye, ikke-deprecated API-en i SK 1.72.0).

### Valgt fremfor

- **Manuell tool-definisjon** — for mye boilerplate, lærer lite om SK
- **YAML prompt functions** — designet for LLM-prompts, ikke statisk datalookup

## Plugin-klasse: `FaiaAcceleratorPlugin`

7 metoder med `[KernelFunction]` og `[Description]`-attributter:

| Metode | Parametre | Beskrivelse |
|---|---|---|
| `GetTrackDetails` | `string track` (A/B/C/D) | Sporets beskrivelse, typiske eksempler, team, nøkkelrisiko |
| `GetWeeklyPlan` | `string track` (A/B/C/D) | 6-ukersplan + uke-for-uke-tabell |
| `GetSalesArguments` | `string audience` (elevator/cfo/cto) | Salgsargumenter tilpasset målgruppe |
| `GetObjectionResponse` | `string topic` | Matcher mot kjente innvendinger |
| `GetCaseExamples` | ingen | Mini-caser + effektrapport-eksempel |
| `GetSecurityInfo` | ingen | Sikkerhet, compliance, beslutningskontroll |
| `GetPricingInfo` | ingen | Inkludert, kostmodell, etter 6 uker, neste steg, vedlegg B+C |

Innholdet er hardkodet som `const string`-felter i klassen. Metoder med ukjent parameter returnerer en hjelpsom feilmelding.

## Innholdskartlegging (whitepaper → plugins)

| Plugin-metode | Whitepaper-seksjon(er) |
|---|---|
| `GetTrackDetails` | "De fire sporene" intro + spesifikk spor-seksjon (beskrivelse, eksempler, team, risiko) |
| `GetWeeklyPlan` | Sporets 6-ukersplan + vedlegg A |
| `GetSalesArguments` | "Dette vil ledere se med en gang" + heispitch / CFO-verdi / CTO-teknikk |
| `GetObjectionResponse` | "Håndtering av innvendinger" |
| `GetCaseExamples` | "Mini-caser" + "Hva du kan forvente i en effektrapport" |
| `GetSecurityInfo` | "Sikkerhet og compliance" + "Tillit og risikohåndtering" + "Beslutningskontroll underveis" |
| `GetPricingInfo` | "Hva som er inkludert" + "Økonomisk kontroll" + "Etter 6 ukene" + "Neste steg" + vedlegg B+C |

Overordnet posisjonering ("Hvorfor AI-prosjekter stopper", "Hvorfor dette går raskere", "Hva vi eksplisitt unngår") bakes inn som kort kontekst i system-prompten.

## System-prompt

Strippes ned til oppførselsregler + kort FAIA-beskrivelse + overordnet posisjonering. Ingen `{Content}`-blokk. Modellen instrueres til å bruke verktøyene for å hente kunnskap.

Oppførselsregler er uendret fra i dag: maks 2-3 setninger, avslutt med oppfølgingsspørsmål, styr mot spor, booking-lenke, norsk, ingen emojier.

## Kernel-oppsett

- `builder.Services.AddKernel()` + `AddAzureOpenAIChatCompletion()`
- Plugin registreres via `kernelBuilder.Plugins.AddFromType<FaiaAcceleratorPlugin>()`
- Execution settings bruker `PromptExecutionSettings` med `FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()`
- Streaming-kallet beholder `chatService.GetStreamingChatMessageContentsAsync()` men sender `kernel` inn i stedet for `null`

## Filendringer

**Ny fil:**
- `api/FaiaChat.Api/Plugins/FaiaAcceleratorPlugin.cs`

**Endrede filer:**
- `api/FaiaChat.Api/Program.cs` — Kernel-registrering, plugin, execution settings
- `api/FaiaChat.Api/Services/SystemPromptBuilder.cs` — Ny slank prompt, fjern Content-konstant

**Uendret:**
- Frontend (`Chat.jsx`) — auto function calling er transparent for klienten
- `ChatRequest.cs`, `FaiaChat.Api.csproj` — ingen endringer nødvendig
