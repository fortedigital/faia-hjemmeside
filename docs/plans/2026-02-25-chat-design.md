# AI Accelerator Chat — Design

## Formål

Informerende chat på landingssiden for AI Accelerator. Lar potensielle kunder stille spørsmål og forstå AI Accelerator i sin egen kontekst.

## Beslutninger

| Aspekt | Beslutning |
|--------|-----------|
| Motor | Claude LLM via API |
| Kunnskapskilde | Notion-sider hentet via Notion API, cachet med TTL |
| Orkestrering | Semantic Kernel (.NET) |
| Backend | ASP.NET Core Web API |
| Utenfor scope | Innrømme ærlig, oppfordre til kontakt |
| Datainnsamling | Ingen — rent informativ |
| Samtalehistorikk | sessionStorage (overlever refresh, ikke fane-lukking) |
| Plassering | Innebygd i siden (som nå) |
| Meldingsgrense | 20 meldinger, deretter lås input og vis kontaktinfo |
| Tone | Profesjonell og saklig |

## Arkitektur

```
Chat.jsx → POST /api/chat → ASP.NET Core API
                              ↓
                         Semantic Kernel
                           - ChatHistory (samtalehistorikk)
                           - System prompt + Notion-kontekst
                           - IChatCompletionService → Claude API
                              ↓
                         Streamed respons tilbake
```

### Frontend (React)

Oppgraderer eksisterende `Chat.jsx`:

- **Streaming:** Svar strømmes ord for ord via Server-Sent Events / ReadableStream
- **SessionStorage:** Samtalehistorikk lagres og gjenopprettes ved refresh
- **Meldingsgrense:** Ved 20 meldinger låses input, kontaktinfo vises
- **Samtalehistorikk til API:** Hele historikken sendes med hvert request
- **Typing-indikator:** Vises til første token strømmes inn
- Beholder eksisterende design (header, bobler, input-felt)

### Backend (ASP.NET Core + Semantic Kernel)

Én endpoint: `POST /api/chat`

**Request:**
```json
{
  "messages": [
    { "role": "user", "content": "Hva er AI Accelerator?" },
    { "role": "assistant", "content": "AI Accelerator er..." },
    { "role": "user", "content": "Hva koster det?" }
  ]
}
```

**Respons:** Streamed text

**Logikk:**
1. Valider request (messages finnes, antall innenfor grensen)
2. Hent Notion-innhold fra cache (eller Notion API hvis utløpt)
3. Bygg system prompt (instruksjoner + Notion-innhold)
4. Kall Claude via Semantic Kernel med streaming
5. Strøm svaret tilbake til klienten

**Sikkerhet:**
- Rate limiting (maks 5 requests per minutt per IP)
- Claude API-nøkkel kun i miljøvariabler
- Ingen brukerdata lagres server-side

### System prompt

Todelt:

**Fast del — instruksjoner:**
- Du er FAIA-assistenten, profesjonell og saklig rådgiver
- Svar kun basert på innholdet du har fått som kontekst
- Utenfor scope: si det ærlig, oppfordre til kontakt
- Svar på norsk, kort og konsist
- Ikke spekuler eller finn på informasjon

**Dynamisk del — Notion-innhold:**
- Hentes via Notion API, caches med TTL (f.eks. 1 time)
- Felles cache for alle brukere
- Injiseres som kontekst-blokker i system prompten

### Notion-caching

- Peker mot spesifikke Notion side-IDer
- Cache i minne med TTL (1 time)
- Ved Notion-nedetid: bruk stale cache som fallback
- Uten cache: vis "Assistenten er midlertidig utilgjengelig"-melding

## Feilhåndtering

| Scenario | Håndtering |
|----------|-----------|
| Claude API nede/treg | Timeout 30s, feilmelding + retry-knapp |
| Notion API nede | Bruk stale cache, eller vis utilgjengelig-melding |
| Tomt input / spam | Frontend-validering + rate limiting |
| Lange meldinger | Maks 500 tegn i input |
| Streaming avbrytes | Ufullstendig svar vises, samtalen kan fortsette |

## Fremtidig utvidelse

- **RAG med vektor-database:** Semantic Kernel har innebygd støtte for Memory/Embeddings — naturlig oppgradering hvis dokumentmengden vokser
- **Lead-innsamling:** Kan legge til valgfri kontaktinfo-innsamling senere
- **Analytics:** Kan legge til server-side logging av samtaler (krever GDPR-vurdering)
