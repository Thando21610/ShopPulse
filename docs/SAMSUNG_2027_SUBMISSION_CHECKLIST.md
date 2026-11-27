# Samsung Innovation Internship 2027 — Submission Readiness Checklist

**Applicant:** Thando Nogemane  
**Project:** ShopPulse  
**Target deadline:** 30 November 2026  
**Status reviewed:** 27 November 2026  
**Documentation:** PRs [#12](https://github.com/Thando21610/ShopPulse/pull/12), [#13](https://github.com/Thando21610/ShopPulse/pull/13) and [#14](https://github.com/Thando21610/ShopPulse/pull/14) are merged into `main`.

This checklist distinguishes applicant-confirmed completion from independent verification. The applicant confirmed on 27 November 2026 that deployed workflows had been tested and were satisfactory. This is not represented as an independent audit or commercial production sign-off.

## 1. Submission materials

| Requirement | Current status | Evidence / next action |
|---|---|---|
| Two-page solution / walkthrough document | **Prepared** | Attach `ShopPulse_Samsung_2027_Two_Page_Brief.pdf`. It is a concise assessment brief, not a full user manual. |
| Full system and role guide | **Prepared** | See [Owner and Operator Quick-Start Guide](OWNER_OPERATOR_QUICK_START.md). |
| Solution design and technical specification | **Merged** | Solution design and deployment details updated in PRs #12–#14. |
| README overview and links | **Merged** | README links to the brief, quick-start guide and technical documentation. |
| Owner interview evidence | **Completed** | Direct interview with Lelethu Noqa completed on **26 November 2026**. |
| Owner phone number / establishment name | **Withheld pending permission** | Do not publish the phone number or an unconfirmed establishment name. |
| Demo video (optional alternative to the two-page document) | **Optional** | The two-page brief is the selected written walkthrough option. |

## 2. Live application and access

| Requirement | Current status | Evidence / next action |
|---|---|---|
| Public demo URL | **Available; recheck before submission** | https://diagnostics.shoppulse-bpd.pages.dev/ |
| API health endpoint | **Available; recheck before submission** | https://shoppulse-production-e6673892.up.railway.app/health |
| Owner assessment account | **Prepared for private handoff** | Sign-in email: `ownerspaza@gmail.com`. Keep the password out of GitHub; provide it only in the private assessor copy/channel as required. |
| Operator assessment account | **Applicant-confirmed complete** | Applicant reports creating and signing in with an Operator account during deployed testing. |
| Operator permissions | **Applicant-confirmed complete** | Applicant reports testing the application workflows; this is not an independent authorization audit. |
| Anonymous guest catalogue / queue | **Not implemented** | Do not list it as an available role or promise this workflow in the submission. |

## 3. Final deployed-browser acceptance

**Applicant-confirmed status: completed 27 November 2026.** The applicant reports testing the deployed workflows and being satisfied with the result. The checkboxes below record that confirmation; they are not an independent audit certificate. Use synthetic data and avoid duplicate writes if repeating any check.

- [x] Owner login and authenticated dashboard.
- [x] Session restoration after browser refresh and logout.
- [x] Product catalogue and product management.
- [x] Owner creates an Operator and verifies the team entry.
- [x] Operator signs in with its own account.
- [x] Operator permissions and Owner-only navigation.
- [x] Product receiving and inventory updates.
- [x] Checkout, receipt and FEFO stock deduction.
- [x] Expiry and Stock Rescue screens.
- [x] Batch-specific waste recording.
- [x] Supported full refund workflow.
- [x] Cash shift and daily report workflows.
- [x] Tenant isolation checks, as reported by the applicant.

Automated CI and applicant-confirmed deployed acceptance are separate evidence types. Neither is presented as an independent security audit or commercial POS approval.

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
- [x] Create and verify a separate Operator assessment account (applicant-confirmed).
- [x] Complete deployed-browser acceptance (applicant-confirmed 27 November 2026; not an independent audit).
- [ ] Confirm the repository visibility/access expected by the Samsung submission process.
- [ ] Recheck the demo and API links on submission day.
- [ ] Attach the two-page PDF and include the repository and live demo links.
- [ ] Share credentials only through the designated private submission channel; rotate/revoke temporary credentials after assessment.
- [x] Merge and verify documentation PRs #12, #13 and #14 into `main`.
