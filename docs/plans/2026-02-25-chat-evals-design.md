# FAIA Chat — Prompt-tuning & Eval Design

## Goal

Systematisk forbedring av FAIA-chatens system prompt gjennom automatiserte evalueringer og observability, med Langfuse som plattform.

## Architecture

Three components:

1. **Langfuse (self-hosted)** — Docker Compose. Tracing, prompt management, eval dashboard.
2. **Tracing i API-et** — `/api/chat` logger hver samtale til Langfuse med input, output, tokens, latency.
3. **Eval-suite** — Separat .NET-prosjekt som kjører testsamtaler mot API-et og scorer dem.

## Langfuse Integration

### Tracing

Legges direkte i `/api/chat`-endepunktet i `Program.cs`. Hver request oppretter en Langfuse trace med:
- Session ID (fra frontend, eller generert)
- Bruker-meldinger (input)
- Bot-svar (output)
- Token count, latency, model info

Integrasjon via Langfuse REST API fra .NET (ingen offisiell .NET SDK, bruker HttpClient).

### Prompt Management

System-prompten flyttes fra hardkodet `SystemPromptBuilder.cs` til Langfuse prompt management.
- `SystemPromptBuilder` henter aktiv prompt-versjon fra Langfuse ved oppstart (cachet med TTL)
- Fallback til hardkodet prompt hvis Langfuse er utilgjengelig
- Gjør det mulig å A/B-teste prompt-varianter uten deploy

## Eval-suite

### Personas (7-8 stk)

1. **CEO** — Spør om ROI, strategi, vil forstå verdiforslag
2. **Utvikler** — Tekniske spørsmål om stack, integrasjoner, arkitektur
3. **Prosjektleder** — Konkret problem, vil vite om tidslinje og prosess
4. **Off-topic** — Spør om vær, politikk, irrelevante ting
5. **Prompt injection** — Forsøker å manipulere boten
6. **Engelsk-talende** — Skriver på engelsk
7. **Snekker** — Lik testsamtalen, praktisk yrke med konkret behov
8. **Usikker beslutningstaker** — Vet AI er viktig, vet ikke hvor de skal begynne

### Samtale-lengder

- **Korte (3-5 meldinger)**: Tester regler og tidlig fase — brukes for alle personas
- **Fulle (10-15 meldinger)**: Tester hele flyten inkl. avslutning — brukes for CEO, prosjektleder, snekker, usikker beslutningstaker

### Scoring

#### Deterministiske sjekker (pass/fail)

| Sjekk | Beskrivelse |
|---|---|
| `max_sentences` | Hvert bot-svar har maks 3 setninger |
| `no_lists` | Ingen bullet points, nummererte lister, markdown-formatering |
| `no_pii_request` | Ber aldri om e-post, telefon, navn |
| `no_hallucinated_actions` | Påstår aldri å kunne sende e-post, booke møter, etc. |
| `norwegian_language` | Svarer på norsk (med mindre brukeren skriver engelsk) |
| `no_emojis` | Ingen emojier i svar |
| `ends_with_question` | Hvert svar (untatt avslutning) avsluttes med et spørsmål |

#### LLM-as-judge (1-5 score)

| Dimensjon | Beskrivelse |
|---|---|
| `track_identification` | Identifiserer riktig spor (A/B/C/D) basert på brukerens behov |
| `conversation_flow` | Naturlig samtaleflyt, ikke repetitiv eller mekanisk |
| `appropriate_closure` | Avslutter med booking-lenke når samtalen er moden, ikke for tidlig/sent |
| `knowledge_accuracy` | Informasjon stemmer med kunnskapsbasen |
| `tone` | Varm, direkte, konsulentaktig — ikke for formell, ikke for uformell |

### Kjøring

Eval-suiten er et .NET console-prosjekt (`FaiaChat.Evals`) som:
1. Starter en testsamtale mot `/api/chat`
2. Sender pre-scriptede bruker-meldinger sekvensielt
3. Samler bot-svar
4. Kjører deterministiske sjekker lokalt
5. Sender fulle samtaler til LLM-as-judge for kvalitetsscoring
6. Logger alle resultater som scores på Langfuse traces

## Prompt-tuning Workflow

1. Kjør eval-suite → baseline-score
2. Endre prompt i Langfuse UI
3. Kjør eval-suite → ny score
4. Sammenlign i Langfuse dashboard
5. Rull ut beste variant (oppdater aktiv prompt-versjon)

## Tech Stack

- Langfuse: Docker Compose (PostgreSQL + Langfuse server)
- Langfuse integration: HttpClient mot REST API
- Eval-suite: .NET 8 console app
- LLM-as-judge: Azure OpenAI (samme deployment)
