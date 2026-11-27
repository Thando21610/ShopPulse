# Samsung Innovation Internship 2027 — Submission Readiness Checklist

**Applicant:** Thando Nogemane  
**Project:** ShopPulse  
**Target deadline:** 30 November 2026  
**Status reviewed:** 27 November 2026  
**Documentation PR:** [#12 — Samsung 2027 submission pack](https://github.com/Thando21610/ShopPulse/pull/12) (merged into `main` via PR #12; interview-date correction follows)

This checklist records the difference between completed preparation and items that still require a final human/live-system check. A passing automated test run is not treated as proof that every deployed browser workflow works.

## 1. Submission materials

| Requirement | Current status | Evidence / next action |
|---|---|---|
| Two-page solution / walkthrough document | **Prepared** | Attach `ShopPulse_Samsung_2027_Two_Page_Brief.pdf`. It is a concise assessment brief, not a full user manual. |
| Full system and role guide | **Prepared** | See [Owner and Operator Quick-Start Guide](OWNER_OPERATOR_QUICK_START.md). |
| Solution design and technical specification | **Updated in draft PR** | Deployment, discovery findings, scope and access checklist aligned with the current implementation. |
| README overview and links | **Updated in draft PR** | Includes the two-page brief and owner/operator guide. |
| Owner interview evidence | **Completed** | Direct interview with Lelethu Noqa completed on **26 November 2026**. |
| Owner phone number / establishment name | **Withheld pending permission** | Do not publish the phone number or an unconfirmed establishment name. |
| Demo video (optional alternative to the two-page document) | **Not prepared** | The two-page brief is the selected written walkthrough option. A short demo video could strengthen communication but is not a substitute for testing. |

## 2. Live application and access

| Requirement | Current status | Evidence / next action |
|---|---|---|
| Public demo URL | **Available; recheck before submission** | https://diagnostics.shoppulse-bpd.pages.dev/ |
| API health endpoint | **Available; recheck before submission** | https://shoppulse-production-e6673892.up.railway.app/health |
| Owner assessment account | **Account details documented** | Sign-in email: `ownerspaza@gmail.com`. Password must be shared privately with assessors, never placed in GitHub or the PDF. |
| Operator assessment account | **Must be created and verified** | Owner signs in, opens **Manage staff**, creates an Operator with a unique email and initial password (minimum 12 characters), then shares credentials privately. |
| Operator permissions | **Documented; live verification required** | Test at least one operator with selected permissions and confirm denied actions remain blocked. |
| Anonymous guest catalogue / queue | **Not implemented** | Do not list it as an available role or promise this workflow in the submission. |

## 3. Final deployed-browser acceptance

Run these checks from the deployed diagnostic frontend before submission. Use synthetic data and avoid duplicate writes if an existing record is already present.

- [ ] Owner login works from a fresh/private browser session.
- [ ] Session remains authenticated after browser refresh; logout returns to sign-in.
- [ ] Owner can load the product catalogue and create/edit/deactivate a test product.
- [ ] Owner can create an Operator and the operator appears in the team list.
- [ ] Operator can sign in with its own account.
- [ ] Operator sees only the actions allowed by its assigned permissions.
- [ ] Operator cannot open Owner-only Reports, Audit, Manage staff or Business settings by direct route/API access.
- [ ] Product receiving creates a batch and increases stock.
- [ ] Checkout commits a sale and deducts stock using FEFO.
- [ ] Expiry and Stock Rescue screens load and show batch/suggestion data.
- [ ] Waste recording reduces the selected batch and appears in history.
- [ ] Full refund workflow behaves as documented.
- [ ] Cash shift opens/closes and owner daily report loads.
- [ ] No tenant's products, sales, staff or reports are visible to another business.

Do not mark a check complete until it has been observed in the live browser. The backend CI result does not replace this list.

## 4. Engineering evidence already recorded

- PR #11 was merged to `main` as `46cf2b0aa4c8c36a03672c73517a1c175df2c034`.
- GitHub Actions run `45763994739` passed for PR commit `bc20c5439f1435df7eb18164c77af2bebbc3af7e` on 26 November 2026.
- The developer reported that `dotnet test ShopPulse.slnx --configuration Release` passed locally after syncing that commit.
- These checks establish automated build/test coverage, not complete live browser acceptance, production readiness or commercial POS approval.

## 5. Product claims to keep accurate

- Implemented: staff sign-in, Owner/Operator authorization, catalogue, stock receiving, inventory, sales, FEFO allocation, full refunds, waste, expiry visibility, rules-based Stock Rescue, cash shifts, daily reports, audit history and business settings.
- Not implemented: anonymous guest catalogue/queue, payment gateway, partial refunds, barcode/QR scanning, password-reset/invitation email and historical inventory snapshots.
- The two-day expiry review is a **proposed pilot operating practice** based on owner feedback, not a verified automatic two-day alert.
- Customer experience analytics are not implemented; voluntary customer feedback is a suggested pilot measurement/future enhancement.
- Do not claim measured reductions in waste, waiting time or improved satisfaction without pilot measurements.
- ShopPulse remains an assessment prototype until live acceptance, security review, operational recovery and backup/restore testing are complete.

## 6. Before pressing submit

- [x] Confirm the interview date and add it to the brief and solution design (**26 November 2026**).
- [ ] Confirm what owner/business details Lelethu has consented to share with Samsung reviewers.
- [ ] Create and verify a separate Operator assessment account.
- [ ] Complete the deployed-browser acceptance checks above.
- [ ] Confirm the repository visibility/access expected by the Samsung submission process.
- [ ] Recheck the demo and API links on submission day.
- [ ] Attach the two-page PDF and include the repository and live demo links.
- [ ] Share credentials only through the designated private submission channel; rotate/revoke temporary credentials after assessment.
- [ ] Review and merge documentation PR #12 only after final details and checks are complete.
