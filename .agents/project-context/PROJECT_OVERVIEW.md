---
version: 1.0.0
last-reviewed: 2026-09-21
---

# Project Overview — Milk Hisab

## What this is

Milk delivery ledger PWA: daily take/skip per customer, monthly hisab (delivered − paid = due),
**manual** payment recording, and **receipt view/download**. No payment gateway.

## Stack

| Layer | Technology |
|-------|------------|
| Backend | .NET 10 Web API, EF Core, SQLite (`milk_hisab.db`) |
| Frontend | Angular 20 standalone, signals, SCSS, PWA |
| Ports | API `http://localhost:8080`, UI `http://localhost:4200` |

## Backend layout

- `Controllers/` → `Services/` → `Data/AppDbContext` (+ models)
- Hybrid: Controller → IService → Service(DbContext). No MediatR, no repositories.

## Frontend layout (required)

- `core/api.service.ts` — HTTP transport only
- `features/{feature}/`
  - `*.page.ts` + `*.page.html` + `*.page.scss` (UI only)
  - `*.service.ts` (feature business logic / orchestration)
- Routes in `app.routes.ts`

## Features

customers · daily · hisab · payments · receipts · dashboard · settings (PWA)

## Hard rules

- No Razorpay / UPI collect / payment SDK
- Receipt HTML download + print required after payment
- Due = delivered − paid
