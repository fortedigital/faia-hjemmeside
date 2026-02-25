export const welcomeMessage = 'Hei! Jeg er FAIA-assistenten. Fortell meg om utfordringen din, så hjelper jeg deg å finne ut hvordan AI Accelerator kan hjelpe.'

export const fallbackResponse = 'Interessant! Det høres ut som noe vi kan hjelpe med. AI Accelerator er en 6-ukers leveransemodell der vi bygger en fungerende AI-løsning i deres miljø. Vil du vite mer om en spesifikk type løsning?'

export const chatResponses = [
  {
    keywords: ['prosess', 'automatiser', 'manuell', 'agent', 'workflow', 'rutine'],
    response: 'Det høres ut som Spor A: Prosessautomatisering & AI-agenter. Vi automatiserer eksisterende forretningsprosesser ved å bygge AI-agenter som integrerer med deres interne systemer. Typiske eksempler er hendelsesrapportering, fakturabehandling, e-posttriagering og intern kunnskaps-Q&A. Alt bygges i deres miljø på 6 uker.'
  },
  {
    keywords: ['data', 'innsikt', 'analyse', 'dashboard', 'rapport', 'beslutning'],
    response: 'Det høres ut som Spor B: Dataintelligens & Beslutningsstøtte. Vi bruker deres eksisterende data til å redusere manuelt arbeid og gi AI-drevet beslutningsstøtte. Dette kan være smart søk over interne dokumenter, automatisert analyse, eller AI-drevne anbefalinger. Har dere data som ikke utnyttes godt nok i dag?'
  },
  {
    keywords: ['app', 'applikasjon', 'bygge', 'ny', 'produkt', 'portal', 'verktøy'],
    response: 'Det høres ut som Spor C: Ny AI-drevet applikasjon. Vi bygger nye fullstack-applikasjoner fra bunnen med AI-kapabiliteter dypt integrert. Eksempler er tale-til-tekst-apper, AI-assisterte anbudsverktøy, eller intelligente kundeportaler. Vi starter med en MVP på 6 uker.'
  },
  {
    keywords: ['oppgrader', 'eksisterende', 'legacy', 'gammel', 'moderniser', 'forbedre'],
    response: 'Det høres ut som Spor D: Intelligent oppgradering. Vi tar en eksisterende applikasjon og legger til AI-kapabiliteter — intelligent søk, automatisert klassifisering, samtalebasert grensesnitt, eller AI-drevne anbefalinger. Den eksisterende applikasjonen består, vi forbedrer den.'
  },
  {
    keywords: ['pris', 'kost', 'betale', 'investering', 'budsjett'],
    response: 'AI Accelerator er en 6-ukers fast leveranse. Alle AI-brukskostnader (tokens, API-kall) i utviklingsperioden dekkes av Forte — dere betaler for teamet, ikke for å eksperimentere med modeller. Etter 6 uker har dere en validert MVP og en tydelig go/no-go-beslutning.'
  },
  {
    keywords: ['tid', 'lang', 'uker', 'frist', 'rask'],
    response: '6 uker. Det er hele modellen. Vi starter fra et velprøvd teknisk fundament som betyr at den første fungerende prototypen typisk er klar i uke 1. De resterende ukene brukes på å bygge, teste med ekte data, og måle forretningseffekt.'
  },
  {
    keywords: ['team', 'hvem', 'folk', 'mennesker', 'kompetanse'],
    response: 'Teamet tilpasses oppdraget, men typisk: en Tech Lead, 1–2 utviklere, og en UX-designer ved behov. Vi jobber i deres miljø, med deres data, og alt vi bygger er deres — kode, infrastruktur, dokumentasjon.'
  },
  {
    keywords: ['etter', 'ferdig', 'videre', 'etterpå', 'vedlikehold'],
    response: 'Etter 6 uker har dere tre valg: skalere til produksjon, kjøre en ny Accelerator-syklus for å bygge videre, eller stoppe. Uansett valg tilbyr Forte en gratis forvaltningsperiode med feilretting og støtte. Alt vi har bygget er deres.'
  },
  {
    keywords: ['sikkerhet', 'personvern', 'gdpr', 'data', 'trygt'],
    response: 'Sikkerhet er bakt inn fra dag én. All AI-prosessering skjer i Azure Sweden Central (eller deres foretrukne region), med prompt injection-beskyttelse, innholdsfiltrering, PII-deteksjon og full revisjonssporing. Rollebasert tilgangskontroll følger deres eksisterende identitetsleverandør.'
  },
  {
    keywords: ['hei', 'hallo', 'heisann', 'hjelp'],
    response: 'Hei! Jeg kan hjelpe deg å forstå hvordan AI Accelerator fungerer. Du kan spørre om:\n\n• Automatisering av prosesser\n• Dataintelligens og analyse\n• Bygging av nye AI-apper\n• Oppgradering av eksisterende systemer\n• Pris og tidsramme\n\nHva lurer du på?'
  }
]

export function getResponse(userMessage) {
  const lower = userMessage.toLowerCase()
  const match = chatResponses.find(r =>
    r.keywords.some(kw => lower.includes(kw))
  )
  return match ? match.response : fallbackResponse
}
