# ShopPulse — Owner and Operator Quick-Start Guide

**Assessment environment:** https://diagnostics.shoppulse-bpd.pages.dev/  
**Applicant:** Thando Nogemane  
**Owner demo sign-in email:** ownerspaza@gmail.com  
**Password:** Shared separately and securely with the assessor. Never commit passwords, refresh tokens or signing secrets to this repository.  
**Status:** Prototype for assessment and controlled testing; not approved for live commercial transactions.

## 1. Sign in as the owner

1. Open the assessment link in a modern browser.
2. Enter the owner email above and the password supplied privately.
3. Select **Sign in**. The authenticated workspace should load.
4. Refreshing the browser should restore the session while the refresh cookie remains valid.
5. Select **Sign out** when finished, especially on a shared device.

If sign-in fails, verify the email/password, network connection and service status. Do not repeatedly create another owner account. The owner account is not managed from the staff screen.

## 2. Create an operator account (Owner only)

1. Sign in using the Owner account.
2. Open **Manage staff** from the workspace navigation (route `/staff`).
3. Under **Add an operator**, enter the operator's full name, unique email address and an initial password.
4. Use a strong initial password of at least **12 characters**. The app does not send an invitation email; give the email and password to the operator through a private channel.
5. Select only the permissions the operator needs (see the permission guide below).
6. Select **Create operator**.
7. Confirm the success message and verify the new person appears in **Store team**. If the list did not refresh, use **Refresh** before attempting to create the account again.
8. The operator signs in at the same ShopPulse login page using their own email and initial password.

The operator account is active immediately. There is no built-in invitation email or self-service password-reset flow yet. Do not share the Owner account with operators.

## 3. Operator permissions explained

All operators can view the product catalogue and inventory, complete sales, view expiry/Stock Rescue information, and open/close their own cash shift. The following optional permissions grant additional actions:

| Permission in Manage staff | What it allows |
|---|---|
| **Record stock waste** (`CanAdjustStock`) | Record batch-specific expired, damaged, missing or spoiled stock. This is not unrestricted manual stock editing. |
| **Refund sales** (`CanRefundSales`) | Perform a supported full refund, enter a reason and confirm the refund was processed externally. Partial refunds and automatic payment reversals are not supported. |
| **Manage products** (`CanManageProducts`) | Create, edit and deactivate catalogue products, including SKU, name, selling price (ZAR) and minimum-stock details. |
| **Receive stock** (`CanReceiveStock`) | Record a stock delivery/batch, quantity, unit cost, supplier/reference and optional expiry date. |

Recommended least-privilege setup: enable only the permissions required for that operator's duties. The Owner can later use **Edit access** to change permissions or deactivate the account. Deactivating an operator revokes their active refresh sessions. Owner accounts are protected from edits on this screen.

## 4. Daily shop workflow — end to end

### A. Set up the catalogue (Owner or permitted operator)
1. Open **Manage products**.
2. Add each product with a clear name, unique SKU/item code, category, selling price in ZAR and minimum-stock threshold.
3. Save and verify that the product appears in the catalogue.
4. Use edit to correct details or deactivate a product that should no longer be sold. Deactivation preserves historical records.

### B. Receive a delivery (Owner or operator with Receive stock)
1. Open **Receive stock**.
2. Select the product and enter received quantity, unit cost in ZAR, supplier and delivery/batch reference.
3. Enter the expiry date for perishable products where applicable.
4. Save, then verify the batch and stock movement in Inventory.
5. Receiving stock increases inventory; it is not a sale and does not open a cash shift.

### C. Check stock and expiry
1. Open **Inventory** to review product quantities, batches and stock movement history.
2. Open **Expiry** to review expired, expiring-soon and safe batches.
3. Open **Stock Rescue** for rules-based suggestions based on recent sales and expiry exposure. Suggestions do not change prices or stock automatically.
4. For the proposed pilot routine, review products expiring within the next two days and decide appropriate action with the shop owner. The two-day review is an operating procedure proposed from owner feedback; do not describe it as a verified automated two-day alert.

### D. Open a cash shift and complete a sale
1. Open **Cash shift** and start your own shift with the opening float in ZAR.
2. Open **New sale**. Search by product name or SKU and add the required quantities to the basket.
3. Review the total and choose Cash or Card.
4. For cash, enter the amount tendered and check the change. Cash checkout requires an open shift for the signed-in operator.
5. For card, use the shop's separate terminal and confirm that it approved the payment. ShopPulse records the confirmation; it does not connect to a bank or process card details.
6. Commit the sale. On success, review the receipt and print if required.

A committed sale records the transaction and deducts the correct stock batches using FEFO (first-expiring, first-out). Do not deduct stock manually after a successful sale.

### E. Record waste (Owner or operator with Record stock waste)
1. Open **Waste**.
2. Select the affected batch, quantity and reason (for example Expired, Damaged, Missing or Spoiled).
3. Save and verify the stock movement. Waste reduces the selected batch quantity and creates an audit record.

### F. Refund a sale (Owner or operator with Refund sales)
1. Open **Refund** and select the original completed sale.
2. Enter the reason and confirm that the money has been refunded outside ShopPulse through the original payment channel.
3. Decide whether returned goods are safe to restock.
4. Submit and verify the refund record. Current support is for a full-sale refund only; ShopPulse does not move money or perform a card reversal.

### G. Close a cash shift
1. Open **Cash shift** and select your own open shift.
2. Review expected cash, count the actual cash and enter the amount.
3. Explain any variance before closing.
4. The Owner can review/reconcile another operator's open shift; an Operator cannot do so.

### H. Owner review
- **Reports:** Owner-only daily summary for sales, refunds, payment mix, waste, current sellable inventory and cash variance.
- **Audit:** Owner-only read-only history of recorded business actions.
- **Manage staff:** create operators, review their access and deactivate accounts.
- **Settings:** update the business name and address.

## 5. Role access summary

| Area/action | Owner | Operator |
|---|---:|---:|
| View products and inventory | Yes | Yes |
| Complete sales | Yes | Yes |
| View expiry and Stock Rescue | Yes | Yes |
| Open/close own cash shift | Yes | Yes |
| Manage products | Yes | With permission |
| Receive stock | Yes | With permission |
| Record waste | Yes | With permission |
| Refund full sale | Yes | With permission |
| Review/reconcile another operator's shift | Yes | No |
| Daily financial reports | Yes | No |
| Audit history | Yes | No |
| Manage staff and business settings | Yes | No |

Permissions are enforced by the API, not just by hiding navigation links. Each person should use their own account.

## 6. Important limitations for assessors

- There is no anonymous public guest catalogue or customer queue module in the current build.
- No payment gateway or bank-terminal integration is included.
- Refunds are full-sale only; partial refunds are not supported.
- Reports are daily summaries; historical inventory snapshots are not available.
- No operator invitation email or password reset is integrated.
- No barcode/QR scanning workflow is implemented yet. It is a future enhancement. A future receiving/inspection scan must capture product and batch/expiry data without reducing stock; checkout scanning should add an item to a sale and only reduce the relevant batch after the sale commits.
- The app is a prototype/assessment project. Live-browser acceptance, operational recovery, backup-restore and security review are still required before real commercial use.

## 7. Safe assessment practice

Use synthetic catalogue and sales data. Do not enter customer personal information. Do not publish passwords or private owner contact details. After assessment access is no longer required, rotate or revoke the demo credentials and deactivate temporary operator accounts.
