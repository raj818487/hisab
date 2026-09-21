---
version: 1.1.0
last-reviewed: 2026-09-21
---

# Backend Pattern — Milk Hisab

## Folder layout (hybrid, single project)

```
backend/
  Controllers/          # thin HTTP adapters only
  Services/
    Abstractions/       # ICustomerService, IDailyEntryService, …
    CustomerService.cs  # business logic + AppDbContext
    …
  Data/                 # AppDbContext, DbSeeder
  Models/               # EF entities
  DTOs/                 # request/response records
  Extensions/           # ServiceCollectionExtensions (DI)
  Program.cs            # pipeline only
```

## Layering

`Controller` → `I{Feature}Service` → `{Feature}Service(AppDbContext)`

- Controllers: validate HTTP input, map status codes — **no EF, no business rules**
- Services: all business logic (hisab math, receipt numbers, upsert rules)
- Data: EF mapping + seed only
- No MediatR, no repository layer, no payment gateway packages

## API prefix

`/api/v1/...`

| Area | Routes |
|------|--------|
| Health | `GET /api/v1/health`, `GET /health` |
| Customers | CRUD `/api/v1/customers` |
| Daily | `GET /api/v1/daily-entries/day-board?date=`, `POST /api/v1/daily-entries` |
| Hisab | `GET /api/v1/hisab/month?year=&month=` |
| Payments | `POST /api/v1/payments`, `GET .../by-month`, `.../by-customer/{id}` |
| Receipts | `GET /api/v1/receipts/{paymentId}`, `.../html` |

## DI

Register in `Extensions/ServiceCollectionExtensions.AddMilkHisabCore`.
