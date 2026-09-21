---
version: 1.0.0
last-reviewed: 2026-09-21
---

# Frontend Pattern — Milk Hisab

## Page file set (mandatory)

For every screen under `features/{name}/`:

| File | Role |
|------|------|
| `{name}.page.ts` | Component class, `templateUrl` + `styleUrl`, inject feature service |
| `{name}.page.html` | Template only |
| `{name}.page.scss` | Styles only |
| `{name}.service.ts` | Business logic, signals, calls `ApiService` |

## Layering

```
Page (UI events) → FeatureService (state + rules) → ApiService (HTTP) → .NET API
```

## State

- Prefer Angular **signals** in feature services for lists/loading/error
- Pages stay thin: bind to `svc.*` signals / methods

## Shared

- `core/api.service.ts` — DTOs + HTTP
- `environments/environment*.ts` — `apiBaseUrl`

## Golden modules

- Simple CRUD: **Customers** (`customers-list` + `customer-form` + `customers.service`)
- Board/upsert: **Daily** (`daily.page` + `daily.service`)
- Read model: **Hisab** (`hisab.page` + `hisab.service`)
- Side-effect + artifact: **Payments → Receipts**
