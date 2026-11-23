# ShopPulse user journeys, use-case coverage and verification

This is the implementation status for the full ShopPulse solution design. It distinguishes implemented code from work that still needs a backend contract, migration, provider integration or production acceptance test.

## Use-case coverage

| ID / feature | Current status | Implemented journey / boundary |
|---|---|---|
| UC-01 Authentication | Implemented | Login, logout, in-memory access token, HttpOnly refresh cookie, session restoration and single-flight refresh |
| UC-02 Product management | Implemented | Owner or operator with CanManageProducts can create, edit and deactivate products; SKU uniqueness, ZAR price and minimum-stock validation; audit event |
| UC-03 Receive stock | Implemented | Owner or operator with CanReceiveStock creates a batch with quantity, unit cost, supplier, reference and optional expiry; stock movement and audit event |
| UC-04 View inventory | Implemented | Product totals, live batch list and stock movement history |
| UC-05 Process sale | Implemented | Search, basket, cash/card selection, idempotency, FEFO allocation, receipt and browser printing |
| UC-06 Refund sale | Implemented (full refund) | Owner or operator with CanRefundSales can refund a completed sale once, record reason/payment confirmation, optionally restock original batches, and write stock movement plus audit event. Partial refunds and payment gateway integration are not included. |
| UC-07 Record waste | Implemented | Batch-specific write-off, reason, permission check, stock movement and audit event |
| UC-08 Monitor expiry | Implemented | Expired, expiring-soon and safe batches; FEFO prioritisation |
| UC-09 Cash shift | Implemented | Operator opens/closes own shift; owner can review and reconcile any open shift |
| UC-10 Anonymous queue | Deferred / optional | Deliberately excluded until a real shop pilot confirms the need |
| UC-11 Reports | Implemented (daily summary) | Net sales, gross sales, cash/card receipts, refunds by method, waste, current sellable inventory, low-stock count, cash variance and selected operational totals. Historical inventory snapshots are not included. |
| UC-12 Audit history | Implemented | Owner-only, tenant-scoped, read-only event history |
| UC-13 Manage staff | Implemented | Owner creates operator accounts, activates/deactivates them and grants/revokes stock-waste, refund, product-management and stock-receiving permissions |
| UC-14 Business settings | Implemented | Owner edits business name and address |
| Rush Mode | Implemented | Larger checkout product tiles and touch-friendly layout |
| Stock Rescue | Implemented | Rules-based estimate using recent 7-day sales and expiry dates; shows assumptions and never changes prices or stock automatically |

## Roles and access

| Journey | Owner | Operator |
|---|---|---|
| View catalogue and inventory | Yes | Yes |
| Complete sale | Yes | Yes |
| Manage products | Yes | With CanManageProducts permission |
| Receive stock | Yes | With CanReceiveStock permission |
| Record waste | Yes | Only when granted CanAdjustStock |
| View expiry and Stock Rescue | Yes | Yes |
| Open/close own cash shift | Yes | Yes |
| Review/reconcile another operator's shift | Yes | No |
| Financial reports | Yes | No |
| Audit history | Yes | No |
| Manage staff and business settings | Yes | No |

The API is the security boundary. Hiding navigation controls is only a usability measure.

## Authentication and session restoration

1. Staff enters email and password on the sign-in screen.
2. Client sends POST /api/auth/login; API returns a short-lived access token and sets an HttpOnly refresh cookie.
3. Client keeps the access token in memory and loads GET /api/auth/me.
4. On browser reload, the home page and protected pages call POST /api/auth/refresh and restore the user profile.
5. AuthMessageHandler serializes refresh attempts. When another request has already refreshed the token, the waiting request retries with that token instead of sending another refresh.
6. If refresh fails, the token store is cleared and the user is returned to the sign-in route.

The browser includes credentials on API calls. The deployed frontend origin must be in the API's explicit CORS allow-list. Cross-origin cookie behaviour must be tested in the actual target browsers.

## Product and inventory journeys

### Product management

1. Owner or operator with CanManageProducts opens Manage products.
2. Owner creates or edits SKU, name, category, selling price (ZAR), minimum-stock threshold and expiry tracking.
3. API validates fields and business-scoped SKU uniqueness, then records an audit event.
4. Deactivation is a soft state change; historical sales and stock records are retained.

### Inventory view

- GET /api/products returns active products and total available quantities.
- GET /api/stock/batches returns remaining batches with supplier, cost and expiry.
- GET /api/stock/movements returns the latest stock ledger events.
- Low-stock status compares current available quantity with the configured minimum-stock threshold.

## Checkout (UC-05)

1. Staff opens New sale and searches by product name or SKU.
2. Staff adds products and adjusts quantities in the basket.
3. The client previews subtotal, cash tendered and change.
4. Cash sales require an open cash shift; the page checks shift status before enabling checkout.
5. For card, the operator confirms that a separate external terminal approved the payment. ShopPulse records the method but does not process card details or integrate with a bank terminal.
6. Client sends POST /api/sales with an Idempotency-Key.
7. API validates products/payment, allocates stock FEFO, records sale lines, stock movements, audit event and idempotency response in a database transaction.
8. On success, UI shows the receipt, change and print action. A retry after a lost response reuses the same idempotency key while the basket/payment data remains unchanged.

A stock conflict returns HTTP 409 and keeps the basket available for review. The server's StockBatch.Version is an optimistic concurrency token; concurrent write conflicts still require explicit acceptance testing.

## Stock receiving (UC-03)

1. Owner or operator with CanReceiveStock opens Receive stock.
2. Client loads active products with GET /api/products.
3. Owner enters product, quantity, unit cost (ZAR), optional expiry date, supplier and batch reference.
4. Client-side field validation runs first; API validates again using the South African calendar date.
5. Client sends POST /api/stock/receipts.
6. API creates the stock batch, positive stock movement and audit event in a transaction.
7. Success toast confirms the receipt.

## Waste management (UC-07)

1. Staff with stock-adjustment permission opens Record waste.
2. Client loads available batches with GET /api/stock/batches.
3. Staff selects a batch, quantity, reason (Expired, Damaged, Missing, Spoiled or Other) and optional notes.
4. API rechecks authoritative availability and permission.
5. Client sends POST /api/stock/waste.
6. API decreases batch quantity and writes waste record, negative stock movement and audit event in a transaction.
7. A 403 is shown as a permission message; a 409 stock conflict is shown inline.

## Expiry and Stock Rescue (UC-08)

- GET /api/stock/expiry?days=N groups remaining batches as Expired, ExpiringSoon or Safe.
- Checkout independently allocates sale quantities using FEFO ordering.
- GET /api/stock/rescue?days=N compares expiring stock with completed sales over the last seven days.
- The recommendation shows recent sales, average daily sales, earliest expiry and estimated at-risk units.
- With no sales history, the UI says the estimate is unavailable. The system never applies a markdown or writes off stock automatically.

## Refund a sale (UC-06)

1. Owner or operator with `CanRefundSales` opens Refund a sale.
2. Client loads up to 100 completed receipts through `GET /api/sales/refundable`.
3. Staff selects the original receipt, reviews its line items and enters a reason.
4. Staff confirms that the money was refunded externally through the original payment channel. ShopPulse does not process card or bank payments.
5. Staff may mark returned goods as safe to restock. If selected, the API restores quantities to the original sale batches, writes positive Refund stock movements and retains expired batches as expired/non-sellable.
6. API records the full refund against the original sale, updates status, records actor/time/method/reason and writes an audit event in a transaction. A sale can only be refunded once.
7. Daily reporting subtracts refunds on the date the refund was issued; cash-shift expected balance subtracts cash refunds.

Partial refunds are not implemented; the current workflow is a full-sale refund.

## Cash shift and reconciliation (UC-09)

1. Operator opens a shift and records opening float in ZAR.
2. Operator can view and close their own shift; owner can review all open shifts and select one to reconcile, while also opening their own shift.
3. Operator/owner records cash-in and cash-out movements with an amount and reason. Each movement is persisted and audited.
4. Close request includes the selected shift ID, actual cash and a variance explanation when actual differs from expected.
5. API calculates expected cash as opening float + cash sales − cash refunds + cash in − cash out, records variance and audit event.
## Reports (UC-11)

- GET /api/reports/summary?date=YYYY-MM-DD returns gross and net sales, refunds by payment method, cash/card totals, cash-in/out totals, waste cost, active product count, current sellable inventory units, low-stock products, audit actions and cash-shift variance for the selected SAST date.
- Inventory values are a current sellable-stock snapshot at report time, not historical stock-at-midnight.
- All monetary values are ZAR. Detailed historical cash-shift and stock-movement analytics remain future work.

## Audit history (UC-12)

- GET /api/audit?take=N&action=... is owner-only and tenant-scoped.
- Product, staff, business, sale, stock, waste, cash-in/out and cash-shift operations add audit events.
- The UI is read-only; there is no edit or delete action for audit events.

## Staff and business settings (UC-13 / UC-14)

- Owner creates Operator accounts with an initial password of at least 12 characters.
- Owner can activate/deactivate an operator and grant/revoke CanAdjustStock, CanRefundSales, CanManageProducts and CanReceiveStock.
- Owner accounts are protected from changes through the staff-management screen.
- Password reset, invitation email and self-service account recovery are not integrated.
- Business settings currently cover business name and address only.

## Visual and interaction rules

- System font stack, accent #0b5f8a, ink #1a2332 and danger #c62828.
- White bordered cards, mobile-first layout and touch targets of at least 44px.
- Material Symbols for interface icons.
- No hero images or remote product photos; product initials use category colours.
- Rush Mode enlarges product tiles for quick checkout.
- Validation is shown inline; successful writes use success feedback; stock conflicts and permission failures remain visible.

## Not yet implemented / not safe to claim

- Partial refunds and automated payment reversals.
- Anonymous queue / customer ticket display (optional and deferred).
- External card gateway or card-terminal integration.
- Password reset/invitation workflow.
- Historical inventory snapshots and a full financial/cash-shift report.
- Offline-first write queue and explicit cold-start/offline banners.
- Full end-to-end browser acceptance tests against the deployed Railway and Neon services.

## Verification still required before live trading

- Deploy the latest main commit to the Railway API and frontend services.
- Set frontend ApiBaseUrl and API Cors__Origins__0 to the exact deployed origins.
- Verify refresh-cookie persistence in the target mobile/desktop browsers.
- Test Owner and Operator accounts separately, including operators with each delegated permission individually and with no delegated permissions.
- Exercise product create/edit/deactivate, receiving, sales, waste, expiry, stock rescue, cash shift, reports, audit, staff and business settings against Neon.
- Verify stock quantities, sale receipts, stock movements, waste records, cash variance and audit events after each write.
- Test duplicate checkout submission, lost network response, concurrent sales of the same batch and stale stock conflict handling.
- Verify receipt printing on the intended receipt printer. Browser print is implemented; thermal-printer-specific integration is not.
- Verify external card-terminal payment and refunds out of band. No card gateway is integrated.
