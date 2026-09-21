# Milk Hisab — Requirements Update (P1)

**Date:** 2026-09-21 (Asia/Calcutta)

## Payment policy (critical)

- **NO payment gateway integration.** Do not add Razorpay, PayU, Stripe, PhonePe, Google Pay SDK, or any UPI collect / QR payment API.
- **Payments are MANUAL records only.** Staff enters amount, mode label (`cash` | `upi` | `other`), paid-on date, and optional note after money is received offline.
- Mode `upi` means “customer paid via UPI and we recorded it” — not an in-app UPI charge.

## Receipts (required)

- After recording a payment, the app **must show a receipt screen** (`/receipts/:paymentId`).
- Receipt must support **Download** (HTML blob from `GET /api/v1/receipts/{paymentId}/html`) and/or **Print / Save as PDF** (`window.print()` on printable HTML).
- Receipt includes: receipt number, customer, amount, mode, paid-on, note, and month delivered/paid/due snapshot.

## Architecture (unchanged)

- Backend: ASP.NET Core (`MilkHisab.Api`), hybrid Controller → IService → Service(`AppDbContext`), SQLite local.
- Frontend: Angular standalone PWA shell, teal theme, API `http://localhost:8080/api/v1`.
- Unique daily entry per `(CustomerId, Date)`.

## Out of scope for this pack

- Online payment collection, webhooks, settlement reports from gateways.
- SMS/WhatsApp auto-send of receipts (manual download/print is enough for P1).
