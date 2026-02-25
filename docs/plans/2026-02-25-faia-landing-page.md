# FAIA Landing Page Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build a clean, minimal landing page for Forte AI Accelerator with three tabs (Hjem, Caser, Hva vi gjor), featuring a chat mockup on the home page.

**Architecture:** Single-page React app with client-side routing (react-router-dom). Three page components rendered via tab navigation. Chat component with mocked responses. All styling via SCSS modules. No backend.

**Tech Stack:** React 18, Vite, react-router-dom, SCSS modules, Inter font (Google Fonts)

---

### Task 1: Project Scaffolding

**Files:**
- Create: Vite React project with SCSS support
- Create: `src/styles/_variables.scss` (shared design tokens)
- Modify: `index.html` (add Inter font, meta tags)

**Step 1: Initialize Vite project and install dependencies**

```bash
cd /Users/edvard.unsvag/faia-hjemmeside
npm create vite@latest . -- --template react
npm install
npm install react-router-dom sass
```

**Step 2: Clean up default Vite files**

Delete: `src/App.css`, `src/index.css`, `src/assets/react.svg`, `public/vite.svg`
Clear `src/App.jsx` to a minimal shell.

**Step 3: Create design tokens**

Create `src/styles/_variables.scss`:
```scss
$color-primary: #511e29;
$color-black: #1a1a1a;
$color-white: #ffffff;
$color-bg: #ffffff;
$color-gray-light: #f5f5f5;
$color-border: #e0e0e0;

$font-family: 'Inter', sans-serif;
$font-size-base: 16px;
$font-size-sm: 14px;
$font-size-lg: 18px;
$font-size-xl: 24px;
$font-size-2xl: 32px;
$font-size-3xl: 48px;
$font-size-hero: 64px;

$spacing-xs: 8px;
$spacing-sm: 16px;
$spacing-md: 24px;
$spacing-lg: 48px;
$spacing-xl: 80px;
$spacing-2xl: 120px;

$max-width: 1200px;
$header-height: 72px;

$breakpoint-mobile: 768px;
$breakpoint-tablet: 1024px;
```

**Step 4: Update index.html**

Add Inter font via Google Fonts link. Set lang="no". Update title to "Forte AI Accelerator".

**Step 5: Create global styles**

Create `src/styles/global.scss`:
```scss
@use 'variables' as *;

*, *::before, *::after {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}

html {
  font-size: $font-size-base;
  -webkit-font-smoothing: antialiased;
}

body {
  font-family: $font-family;
  color: $color-black;
  background: $color-bg;
  line-height: 1.6;
}

a {
  color: inherit;
  text-decoration: none;
}
```

Import global.scss from `main.jsx`.

**Step 6: Verify dev server runs**

```bash
npm run dev
```
Expected: Blank white page at localhost:5173

**Step 7: Init git and commit**

```bash
git init
# create .gitignore for node_modules, dist, .env, etc.
git add -A
git commit -m "chore: scaffold Vite + React + SCSS project"
```

---

### Task 2: Logo Component

**Files:**
- Create: `src/components/Logo/Logo.jsx`
- Create: `src/components/Logo/Logo.module.scss`

**Step 1: Create Logo component with inline SVG**

Use the Forte SVG logo extracted from fortedigital.com. Add "AI Accelerator" text below/beside the logo mark. SVG fill color uses currentColor so it can be styled via CSS.

**Step 2: Style the logo**

Logo mark + "AI Accelerator" text. The "AI Accelerator" text should be smaller, uppercase, letter-spaced, in $color-primary.

**Step 3: Verify visually in browser**

**Step 4: Commit**

```bash
git add src/components/Logo/
git commit -m "feat: add Forte logo component with AI Accelerator subtitle"
```

---

### Task 3: Header & Navigation Layout

**Files:**
- Create: `src/components/Header/Header.jsx`
- Create: `src/components/Header/Header.module.scss`
- Create: `src/components/Layout/Layout.jsx`
- Create: `src/components/Layout/Layout.module.scss`
- Modify: `src/App.jsx` (add router + layout)

**Step 1: Create Header component**

Logo on left. Three NavLink tabs on right: "Hjem" (/), "Caser" (/caser), "Hva vi gjor" (/hva-vi-gjor). Active tab gets a bottom border in $color-primary.

**Step 2: Style Header**

Fixed position, white background, subtle bottom border. Height: 72px. Max-width container centered.

**Step 3: Create Layout component**

Wraps Header + page content with proper padding below the fixed header.

**Step 4: Set up React Router in App.jsx**

```jsx
import { BrowserRouter, Routes, Route } from 'react-router-dom'
import Layout from './components/Layout/Layout'
// Page imports (placeholder divs for now)

function App() {
  return (
    <BrowserRouter>
      <Layout>
        <Routes>
          <Route path="/" element={<Home />} />
          <Route path="/caser" element={<Caser />} />
          <Route path="/hva-vi-gjor" element={<HvaViGjor />} />
        </Routes>
      </Layout>
    </BrowserRouter>
  )
}
```

Create placeholder page components that just render their name.

**Step 5: Verify navigation works in browser**

Click between tabs, verify URL changes and active tab highlights.

**Step 6: Commit**

```bash
git add src/
git commit -m "feat: add header navigation with routing"
```

---

### Task 4: Home Page - Hero Section

**Files:**
- Create: `src/pages/Home/Home.jsx`
- Create: `src/pages/Home/Home.module.scss`

**Step 1: Build Hero section**

Large headline: "Fra problem til fungerende AI-losning pa 6 uker"
Subtitle paragraph: elevator pitch from Notion content.
Left-aligned, generous whitespace, big typography.

**Step 2: Style Hero**

- Headline: font-size-hero on desktop, font-size-3xl on mobile
- Subtitle: font-size-lg, color slightly muted
- Padding: $spacing-2xl top, $spacing-xl bottom
- Max-width text container ~700px

**Step 3: Verify responsive behavior**

Check desktop and mobile widths.

**Step 4: Commit**

```bash
git add src/pages/Home/
git commit -m "feat: add hero section to home page"
```

---

### Task 5: Home Page - Chat Component

**Files:**
- Create: `src/components/Chat/Chat.jsx`
- Create: `src/components/Chat/Chat.module.scss`
- Create: `src/data/chatResponses.js` (mocked responses)
- Modify: `src/pages/Home/Home.jsx` (add Chat below hero)

**Step 1: Create mocked response data**

Create `src/data/chatResponses.js` with keyword-matched responses from the Notion content. Map common questions to pre-written answers about the four tracks, 6-week model, pricing, etc.

```js
export const chatResponses = [
  {
    keywords: ['prosess', 'automatiser', 'manuell', 'agent'],
    response: 'Spor A: Prosessautomatisering & AI-agenter — Vi automatiserer eksisterende forretningsprosesser ved a bygge AI-agenter som integrerer med deres interne systemer. Typisk: hendelsesrapportering, fakturabehandling, e-posttriagering.'
  },
  // ... more entries
]
```

**Step 2: Build Chat component**

- Message list (scrollable)
- Input field + send button
- Initial welcome message from "FAIA"
- On user send: match keywords from chatResponses, display matched response (or a fallback)
- Typing indicator animation before response appears

**Step 3: Style Chat**

- Container: white bg, 1px border $color-border, subtle border-radius
- Messages: user messages right-aligned with $color-primary bg + white text, bot messages left-aligned with $color-gray-light bg
- Input: clean input field, send button in $color-primary
- Max-height with scroll for message area
- Responsive: full-width on mobile

**Step 4: Integrate into Home page below Hero**

**Step 5: Test chat interaction in browser**

Type messages, verify responses appear with typing animation.

**Step 6: Commit**

```bash
git add src/components/Chat/ src/data/ src/pages/Home/
git commit -m "feat: add chat component with mocked AI responses"
```

---

### Task 6: Cases Page

**Files:**
- Create: `src/pages/Caser/Caser.jsx`
- Create: `src/pages/Caser/Caser.module.scss`
- Create: `src/components/CaseCard/CaseCard.jsx`
- Create: `src/components/CaseCard/CaseCard.module.scss`
- Create: `src/data/cases.js`

**Step 1: Create case data**

Four cases from Notion, one per track:
- A: Hendelsesrapporteringsagent
- B: Anbudsintelligensplattform
- C: Tale-til-tekst-applikasjon
- D: Legacy-system + AI-sok & redesign

Each: title, track label, description, example outcomes.

**Step 2: Build CaseCard component**

Card with track label (small pill/tag in $color-primary), title, description text. Clean, minimal. No images.

**Step 3: Build Caser page**

Headline: "Kundecaser"
Subtitle: "Hver kunde er forskjellig, men problemene folger gjenkjennbare monstre."
Grid of 4 CaseCards (2 columns desktop, 1 mobile).

**Step 4: Style everything**

Cards: white bg, subtle border or shadow, generous padding. Track label as small uppercase pill.

**Step 5: Verify in browser**

**Step 6: Commit**

```bash
git add src/pages/Caser/ src/components/CaseCard/ src/data/cases.js
git commit -m "feat: add cases page with four track examples"
```

---

### Task 7: What We Do Page

**Files:**
- Create: `src/pages/HvaViGjor/HvaViGjor.jsx`
- Create: `src/pages/HvaViGjor/HvaViGjor.module.scss`
- Create: `src/data/tracks.js`

**Step 1: Create tracks data**

Four tracks with: name, icon/letter (A/B/C/D), tagline (the customer quote), description, typical examples list.

Also: "after 6 weeks" data (scale, continue, stop), value proposition text, and "what's included" items.

**Step 2: Build HvaViGjor page sections**

Section 1 - Intro: "AI Accelerator" headline + paragraph description of the 6-week model.

Section 2 - Four tracks: Each track as a clean section with letter label, name, tagline quote, description, bullet list of examples.

Section 3 - After 6 weeks: Three outcomes presented as simple columns.

Section 4 - Value proposition: Quote-style text block for decision makers.

**Step 3: Style all sections**

Generous spacing between sections. Track sections separated by subtle horizontal lines. Quotes in italic with left border in $color-primary. Outcomes as three equal columns.

**Step 4: Verify responsive behavior**

**Step 5: Commit**

```bash
git add src/pages/HvaViGjor/ src/data/tracks.js
git commit -m "feat: add what-we-do page with tracks and value proposition"
```

---

### Task 8: Polish & Responsive

**Files:**
- Modify: Various component SCSS files
- Create: `src/styles/_mixins.scss` if needed

**Step 1: Review all pages at mobile width (375px)**

Fix any layout issues: stack columns, reduce font sizes, adjust spacing.

**Step 2: Review at tablet width (768px-1024px)**

**Step 3: Add smooth page transitions**

Simple fade-in on page change if it feels abrupt.

**Step 4: Final visual review**

Check spacing consistency, typography hierarchy, color usage across all three pages.

**Step 5: Commit**

```bash
git add -A
git commit -m "feat: polish responsive layout and visual consistency"
```

---

### Task 9: Final Build Verification

**Step 1: Run production build**

```bash
npm run build
npm run preview
```

**Step 2: Verify all three pages work in preview**

**Step 3: Final commit if any fixes needed**
