# BRD Requirement Delta — Milk Hisab P1

**Patch against:** Milk Hisab Business Requirements Document  
**Effective:** 2026-09-21 IST  
**Author pack:** p1-pack (manual payments + receipts)

## Summary

Replace any BRD language that implies **online payment collection** or **third-party payment gateway** with **manual payment ledger + printable/downloadable receipt**.

## Delta table

| BRD area | Previous intent (if any) | New / corrected intent |
|----------|--------------------------|------------------------|
| Payments | Possibly “integrate UPI / Razorpay” | **Manual ledger only.** Store amount, mode label (cash/upi/other), paid-on, note, auto receipt number. |
| UPI | Collect via gateway / intent | **Label only** for how cashless payment was received offline. No SDK. |
| Razorpay / gateways | Integration candidate | **Removed / forbidden** for P1 and default product path. |
| Receipts | Unclear | **Must** show receipt UI after payment; **must** allow HTML download and print-friendly PDF via browser. |
| Settlement | Gateway settlement | N/A — operator reconciles cash/UPI bank statements externally. |
| API | Charge / capture endpoints | `POST /api/v1/payments` records only; `GET /api/v1/receipts/{id}` + `/html` for view/download. |

## Acceptance criteria (updated)

1. User can record a payment without any gateway credentials or keys.
2. User sees receipt screen with receipt number immediately after save.
3. User can download receipt HTML and print it.
4. No NuGet/npm packages for Razorpay, Stripe, or UPI collect appear in the solution.
5. Month hisab due = delivered − manually recorded payments.

## Doc text to scrub

Search BRD / README for: `Razorpay`, `payment gateway`, `collect payment`, `UPI intent`, `webhook`, `order_id` (gateway). Replace with manual-ledger wording or delete.

## Non-goals (explicit)

- Auto-debit, QR dynamic payment links, refund via API, escrow.
