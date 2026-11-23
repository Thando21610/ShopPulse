# ShopPulse user journeys and API wiring

This document describes the current implementation in the Blazor WebAssembly client and ASP.NET Core API. It is an implementation map, not a substitute for live acceptance testing against Railway and Neon.

## Authentication and session

1. Staff enters email and password on the sign-in screen.
2. Client sends POST /api/auth/login; API returns a short-lived access token and sets an HttpOnly refresh cookie.
3. Client keeps the access token in memory and calls GET /api/auth/me for the staff profile and role.
4. On a browser reload or protected-page entry, client calls POST /api/auth/refresh and restores the profile.
5. The API request handler serializes concurrent refresh attempts. If refresh fails, the client clears the access token and returns to the sign-in route.

The browser sends credentials with API requests. The deployed frontend origin must be included in the API's explicit CORS origin list.

## Role and permission matrix

| Journey | Owner | Operator |
|---|---|---|
| View catalogue | Yes | Yes |
| Complete sale | Yes | Yes |
| Receive stock | Yes | No |
| Record waste | Yes | Only with CanAdjustStock permission |
| View expiry batches | Yes | Yes |
| Open own cash shift | Yes | Yes |
| Reconcile/close a cash shift | Yes | No |
| View financial reports | Yes | No |

The API authorization policies are authoritative. Hiding a navigation link is not a security boundary.

## Catalogue and low-stock awareness

- Client loads GET /api/products.
- The API returns SKU, product name/category, ZAR unit price, available batch quantity and minimum stock level.
- The dashboard highlights items at or below their minimum stock threshold.
- Checkout product tiles use category-coloured initials; the design intentionally uses no hero or remote product photos.

## Checkout (UC-05)

1. Staff opens New sale and searches by product name or SKU.
2. Staff adds products and adjusts quantities in the basket.
3. The client previews subtotal, cash tendered and change.
4. Cash sales require the operator to have an open cash shift. If no shift exists, the API returns a conflict and the UI links to Cash shift.
5. For card, the operator must confirm that the external card terminal approved the payment. ShopPulse records the payment method; it does not process card details or integrate with a bank terminal.
6. Client sends POST /api/sales with an idempotency key.
7. API validates products/payment, allocates stock FEFO, records sale lines, stock movements, audit event and idempotency response in a database transaction.
8. On success, UI shows the receipt, change and print action. A retry after a lost response reuses the same idempotency key while the basket/payment data remains unchanged.

Stock conflicts return HTTP 409 and keep the basket available for review.

## Stock receiving (UC-03)

1. Owner opens Receive stock.
2. Client loads active products with GET /api/products.
3. Owner enters product, quantity, unit cost (ZAR), optional expiry date, supplier and batch reference.
4. Client-side field validation runs first; API validates again.
5. Client sends POST /api/stock/receipts.
6. API creates the stock batch, positive stock movement and audit event in a transaction.
7. Success toast confirms the receipt.

## Waste management (UC-07)

1. Staff with stock-adjustment permission opens Record waste.
2. Client loads available batches with GET /api/stock/batches.
3. Staff selects a batch, quantity, reason (Expired, Damaged, Missing, Spoiled or Other) and optional notes.
4. Client checks quantity against the latest displayed batch amount; API rechecks authoritative availability.
5. Client sends POST /api/stock/waste.
6. API decreases batch quantity and writes waste record, negative stock movement and audit event in a transaction.
7. A 403 is shown as a permission message; a 409 stock conflict is shown inline.

## Expiry and FEFO (UC-08)

- Client requests GET /api/stock/expiry?days=N.
- API returns remaining batches expiring in the selected window, ordered by expiry date.
- UI separates expired (red) and expiring soon (amber) stock, with a link to record waste for expired batches.
- Checkout independently allocates sale quantities using FEFO ordering.

## Cash shift and reconciliation

1. Operator opens Cash shift and records opening float in ZAR with POST /api/cash-shifts/open.
2. Operator's current shift is returned by GET /api/cash-shifts/current.
3. Owner sees all open staff shifts through GET /api/cash-shifts/open, selects a specific shift and enters actual cash counted.
4. Owner closes the selected shift with POST /api/cash-shifts/close including the shift ID.
5. API calculates expected cash from opening float plus completed cash sales, records variance and audit event, and rejects a non-zero variance without an explanation.

## Reports

- Owner selects a date; client requests GET /api/reports/summary?date=YYYY-MM-DD.
- API returns completed sales count/total, cash/card totals and recorded waste cost.
- Monetary values display in South African Rand. API report boundaries use the South African calendar (UTC+02:00).

## Verification still required before live trading

- Deploy the latest main commit to the Railway API and frontend services.
- Set frontend ApiBaseUrl and API Cors__Origins__0 to the exact deployed origins.
- Verify refresh-cookie persistence in the target mobile/desktop browsers.
- Test Owner and Operator accounts separately, including an Operator without CanAdjustStock.
- Exercise each journey against the live Neon database; verify stock quantities, sale receipts, stock movements, waste records, cash variance and audit events.
- Test duplicate checkout submission, lost network response, concurrent sales of the same batch, and stale stock conflict handling.
- Verify print layout on the intended receipt printer. Browser print is implemented; thermal-printer-specific integration is not.
- Verify external card-terminal payment out of band. No card gateway is integrated.
