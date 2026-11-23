# ShopPulse frontend-to-API integration audit

**Scope:** source-level review of the Blazor WebAssembly screens, their event handlers and HTTP calls, the API controller routes and authorization attributes. This is not a browser-driven or deployed end-to-end test.

## Screen-to-API map

| Frontend screen | API operations wired in the page | Backend contract / authorization |
|---|---|---|
| Sign-in / overview (`/`) | `GET api/products`; authentication uses `api/auth/login`, `api/auth/me`, `api/auth/refresh`, `api/auth/logout` | Product list requires staff authentication; login/refresh/logout are anonymous endpoints by design; `me` requires authentication. |
| Product management (`/products/manage`) | `GET api/products/manage`, `POST api/products`, `PUT api/products/{id}`, `POST api/products/{id}/deactivate` | Product management policy; owner or operator with current database permission. |
| Stock receiving (`/stock/receive`) | `GET api/products`, `POST api/stock/receipts` | Receive-stock policy; owner or operator with current database permission. |
| Inventory (`/inventory`) | `GET api/products`, `GET api/stock/batches`, `GET api/stock/movements?take=250` | Staff-authenticated read operations, tenant scoped. |
| Checkout (`/sales`) | `GET api/products`, `GET api/cash-shifts/current`, `POST api/sales` with `Idempotency-Key` | Sale completion requires staff role; API validates active products, expected prices, available non-expired batches, payment confirmation and cash-shift state, then records sale, stock movements and audit event in a transaction. |
| Refunds (`/sales/refunds`) | `GET api/sales/refundable`, `POST api/sales/{id}/refunds` | Refund permission; API checks original sale state, refund-issued confirmation, optional original-batch restock and cash-shift requirement for cash refunds. |
| Waste (`/stock/waste`) | `GET api/stock/batches`, `POST api/stock/waste` | Stock-adjust permission; API validates batch/product/quantity and writes waste, movement and audit event transactionally. |
| Expiry (`/expiry`) | `GET api/stock/expiry?days=...` | Staff-authenticated read; tenant scoped. |
| Stock rescue (`/stock-rescue`) | `GET api/stock/rescue?days=...` | Staff-authenticated read; tenant scoped. |
| Cash shifts (`/cash-shift`) | `GET api/cash-shifts/current`, owner-only `GET api/cash-shifts/open`, `GET api/cash-shifts/{id}/movements`, `POST api/cash-shifts/open`, `POST api/cash-shifts/movements`, `POST api/cash-shifts/close` | Current-shift and movement endpoints enforce operator ownership or owner access; open-shift listing is owner-only; close verifies ownership and cash reconciliation. |
| Reports (`/reports`) | `GET api/reports/summary?date=...` | Owner-only financial reports policy. |
| Audit (`/audit`) | `GET api/audit?take=250...` | Owner-only audit policy; API caps page size at 250. |
| Staff (`/staff`) | `GET api/staff`, `POST api/staff`, `PUT api/staff/{id}` | Owner-only controller. |
| Business settings (`/settings`) | `GET api/business/current`, `PUT api/business/current` | Owner-only controller. |

## Interaction scan

- All 14 operational pages use the shared `WorkspaceNav` component.
- Static scan of the current page components found no `<button>` without either an `@onclick` handler or a form-submit action.
- Form submissions use Blazor `EditForm` handlers; the API remains authoritative for validation and authorization.
- Checkout sends product IDs, quantities and expected unit prices; the server recalculates totals and allocates FEFO batches. The UI does not calculate or submit stock deductions.
- Cash and card paths are intentionally different: cash requires the operator's open shift and tender/change validation; card requires explicit external-terminal approval and does not collect card credentials.

## Changes in this UX pass

- Payment modal has a programmatic focus entry, keyboard Tab focus trap, Escape handling, focus restoration on cancel and focus transfer to the receipt after success.
- Basket quantity controls now use 44px targets; mobile product cards become a single-column list; basket totals announce changes to assistive technology.
- Receipt now includes receipt number, SAST timestamp, SKU, unit price, quantity, line total, subtotal, discount, payment method, tendered amount and change.
- Product refresh reconciles basket prices and availability. A stale or unavailable basket is blocked from checkout until corrected.
- If a checkout request has an uncertain network/server outcome, the exact request payload and idempotency key are retained. Editing/closing the payment flow is blocked and the operator can retry the same request, allowing the API to replay the original receipt rather than create a duplicate.
- Loading failures are distinguished from a genuinely empty product catalogue; retry is available.

## Risks and follow-up work

1. **Runtime acceptance is still required.** No browser automation, keyboard/screen-reader session, mobile device test or live Railway/Neon transaction test was run as part of this source review.
2. **Idempotency is not uniform outside checkout.** Stock receipt, waste, cash movement and refund POST flows do not currently expose the same request idempotency contract as checkout. The UI disables a submit button while its request is in flight, but a lost response followed by a manual retry can still be ambiguous. Add server-side idempotency or operation-specific reconciliation before production use.
3. **Integration test coverage is narrow.** The repository test project currently contains authorization-focused tests; add API integration tests for sale/FEFO, cash shift, receive stock, waste, refund/restock, reports, tenant isolation and duplicate submissions.
4. **Consistent error/loading states remain a cross-screen task.** Some list pages catch only `HttpRequestException`; malformed responses and API authorization/server errors need consistent presentation, retry and empty-state behaviour.
5. **External payment is not integrated.** Card checkout records an operator's confirmation of approval on a separate terminal; it does not call an acquiring gateway.

## Acceptance checklist

- [ ] 320px, 360px, 390px, tablet and desktop viewport checks.
- [ ] Keyboard-only payment modal: focus entry, Tab/Shift+Tab wrap, Escape cancel, focus restoration, busy-state lock.
- [ ] Screen-reader announcement of validation, change due, basket total and completed receipt.
- [ ] Cash sale with open shift; cash sale without shift; exact change; overpayment; insufficient tender.
- [ ] Card sale with terminal confirmation; reject without confirmation.
- [ ] Duplicate checkout request and lost-response retry returns one receipt and one stock deduction.
- [ ] Stale price, stale stock, expired batch and concurrent sale conflicts.
- [ ] Owner and Operator role matrix across every route and direct API request.
- [ ] Live CORS/session restoration against deployed frontend and API origins.
