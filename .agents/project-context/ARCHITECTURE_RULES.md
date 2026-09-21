---
version: 1.0.0
last-reviewed: 2026-09-21
---

# Architecture Rules — Milk Hisab

1. NEVER_BREAK_EXISTING_FUNCTIONALITY — preserve API contracts; run smoke after changes.
2. ALWAYS_VERIFY_LATEST_CONTEXT — read `.agents/project-context` + feature docs before edits.
3. SENIOR_ENGINEER_SURGICAL_EDITS — minimal diffs; reuse ApiService + feature services.
4. Backend hybrid only: Controller → IXxxService → XxxService(AppDbContext).
5. Frontend: pages must NOT call HttpClient directly — use feature `*.service.ts` → `ApiService`.
6. Templates live in `.page.html`; styles in `.page.scss`; no large inline templates.
7. Unique daily entry per `(CustomerId, Date)`.
8. Payments are manual ledger only; `mode` is a label (`cash`|`upi`|`other`).
9. Receipt numbers stable (`MH-yyyyMMdd-####` or `MH-yyyyMM-####`); never regenerate.
10. SQLite local DB file under `backend/`; seed is idempotent.
