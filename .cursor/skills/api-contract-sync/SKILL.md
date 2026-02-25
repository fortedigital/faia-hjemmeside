---
name: api-contract-sync
description: Keeps backend API and frontend client in sync. Use when adding or changing API endpoints, when frontend must call new or modified backend endpoints, or when working across server/ and src/shared/api/ or feature API usage. Ensures correct order (backend → generate:api → frontend) and that only generated types and clients are used.
disable-model-invocation: false
---

# API Contract & Full-Stack Sync

**After any backend API change:** Restart the backend and then run `npm run generate:api` before implementing or changing any frontend code.

## When to Use This Skill

Apply this skill when:
- Adding a new API endpoint or modifying an existing one
- Frontend needs to call a new or changed backend endpoint
- Working on a feature that spans backend (server/) and frontend API usage (src/shared/api/, src/features/*/api.ts, queries.ts)
- User asks to "add API", "expose endpoint", "call backend from frontend", or similar

## Purpose

Ensures backend and frontend stay in contract: correct implementation order, no manual API types on frontend, and all API access through the generated client. Prevents wrong types, missing regeneration, and "forgot to add to createApiClients" mistakes.

---

## Agent behavior (required)

- When adding or changing backend API that the frontend will call, you **must** complete the full flow in one go: implement backend → ensure backend is restarted (or instruct the user to restart) → run `npm run generate:api` → implement frontend using **only** the generated client and types. Do not implement frontend with manual `fetch` or manual types and then suggest the user "optionally" run generate:api or "switch to the generated client" later.
- Do **not** end implementations with notes like "After you restart the backend and run npm run generate:api, you can switch to the generated client." Either complete the sync step as part of the implementation, or state clearly that the user **must** restart the backend and run `npm run generate:api` before the frontend will work, as a required step—not an optional improvement.
- If the backend cannot be restarted during the session (e.g. not running or not in your control), then: implement the backend and the frontend using the generated client **assuming** the new endpoints exist in Swagger; add to `createApiClients()` if a new API was added; and in the handover tell the user exactly once: "Restart the backend and run `npm run generate:api` so the generated client includes the new endpoints; the frontend is already written to use it." Do not add "optional" or "you could later switch" suggestions.

---

## Backend Checklist (server/)

1. **Controller** – Thin: routing, status codes, validation only. No business logic, no direct DB access.
2. **Service** – Business logic and orchestration. Uses repositories only; never `AppDbContext` or direct queries.
3. **Repository** – All database queries live here. Interface + implementation, registered in DI.
4. **DTOs** – Request/response types in `Models/DTOs/`. Used by controller and Swagger.
5. **Registration** – Register new services/repositories in `Extensions/ServiceCollectionExtensions.cs` via `AddApplicationServices()`.
6. **Swagger** – Endpoint is exposed via existing `[ApiController]` / routing; ensure DTOs and route are correct so the OpenAPI spec reflects the contract.

---

## Sync Step (Required Before Frontend Work)

1. **Restart the backend with your new changes** – After changing server code (DTOs, controllers, etc.), restart the backend (e.g. stop and run `dotnet run` in `server/`) so the running process serves the updated Swagger. OpenAPI is read from `http://localhost:8080/swagger/v1/swagger.json`; if an old process is still running, the generated client will be out of date.
2. **Regenerate frontend client** – From repo root: `npm run generate:api`  
   - Reads live Swagger from the backend and writes to `src/shared/api/generated/`.
3. Do **not** implement frontend types or API calls until after this step. Frontend must use only what was just generated.

---

## Frontend Checklist (src/)

1. **Types** – Use only types from `src/shared/api/generated/`. Do not define manual types for API requests/responses.
2. **Client** – All API access via the generated clients from `src/shared/api/client.ts`. If a **new** API was added (new controller/client in generated code), add it to `createApiClients()` and export the default instance at the bottom of `client.ts` (see existing pattern: e.g. `EmployeesApi`, `GiftcardsApi`).
3. **Queries / API usage** – In feature `queries.ts` or `api.ts`, get the appropriate client from `createApiClients(accessToken)` and call the generated methods. Use `useAppSelector(selectAccessToken)` where auth is required.
4. **Enums** – If the generated code or feature uses enums, import them as **values**, not types: `import { EnumName } from '...'` — never `import type { EnumName }` (enums are runtime values; type-only import can cause ReferenceError).
5. **Auth** – Generated client expects `apiKey` with `Bearer ${accessToken}`; `client.ts` already sets this when you use `createApiClients(accessToken)`.

---

## Order of Operations (Summary)

```
Backend (Controller → Service → Repository → DTOs → DI)
  → Restart/start backend with new code (e.g. dotnet run in server/)
  → npm run generate:api (from repo root)
  → Frontend: use only generated types and clients; add new API to client.ts if new; use in queries/features
```

---

## Common Mistakes to Avoid

- **Running `npm run generate:api` without restarting the backend after server changes** – Swagger is served by the running process; an old process will expose the old contract and the generated client will miss new or changed types.
- **Implementing frontend types or API calls before running `npm run generate:api`** – Types and method signatures may be wrong or missing.
- **Defining manual request/response types for endpoints** – Always use generated types from `src/shared/api/generated/`.
- **Putting database queries in controller or service** – All queries belong in repositories.
- **Forgetting to add a new API to `createApiClients()`** – If the OpenAPI generator added a new `*Api` class, it must be instantiated and returned in `createApiClients()` and exported for unauthenticated use if needed.
- **Leaving "optional" follow-up notes** – Do not end with "you can run generate:api and switch to the generated client." Either do the sync as part of the work or state it as a required step once.
