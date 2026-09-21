# Feature — Payments & Receipts

## Purpose
Record offline payments (cash/upi/other labels) and show/download receipt HTML.

## Hard rule
**No payment gateway.** `upi` is a mode label only.

## Frontend
- `features/payments/payments.page.{ts,html,scss}` + `payments.service.ts`
- `features/receipts/receipt.page.{ts,html,scss}` + `receipts.service.ts` (download + print)

## Backend
- `PaymentsController`, `PaymentService`
- `ReceiptsController`, `ReceiptService` (`NextReceiptNumberAsync`, HTML builder)

## API
- `POST /api/v1/payments`
- `GET /api/v1/payments/by-month?year&month`
- `GET /api/v1/receipts/{paymentId}`
- `GET /api/v1/receipts/{paymentId}/html`
