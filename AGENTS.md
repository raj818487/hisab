# AGENTS — Milk Hisab

Before coding, load:

1. `.agents/project-context/PROJECT_OVERVIEW.md`
2. `.agents/project-context/FRONTEND_PATTERN.md` / `BACKEND_PATTERN.md`
3. Matching `.agents/features/*.md`

## Frontend structure (enforced)

`*.page.ts` + `*.page.html` + `*.page.scss` + feature `*.service.ts`  
HTTP only via `core/api.service.ts`.

## Product rule

No payment gateway. Receipt view + HTML download/print only.
