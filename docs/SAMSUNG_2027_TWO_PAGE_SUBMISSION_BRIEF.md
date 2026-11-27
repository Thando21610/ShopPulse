# ShopPulse — Samsung Innovation Internship 2027
## Two-page solution and pilot brief

**Project:** ShopPulse — Smart retail operations for independent South African spaza shops  
**Applicant:** Thando Nogemane  
**Live demonstration:** https://diagnostics.shoppulse-bpd.pages.dev/  
**Source code:** https://github.com/Thando21610/ShopPulse  
**API health:** https://shoppulse-production-e6673892.up.railway.app/health  
**Interview contact:** Lelethu Noqa, shop owner (interview completed; interview date to be recorded).  
**Document status:** Submission draft. Owner contact number is intentionally omitted until explicit permission to publish it is confirmed.

---

## 1. The problem — grounded in owner discovery

ShopPulse is designed around the day-to-day realities of a small South African spaza shop where one person may need to serve customers, handle payments, monitor stock and keep products safe for sale.

In a direct discussion, shop owner **Lelethu Noqa** highlighted two practical concerns:

- **Food expiry and wastage:** food products can expire before they are sold. This creates avoidable losses and risks customers receiving products that are no longer good quality.
- **Limited visibility of customer experience:** the owner does not have a clear, structured view of whether customers are happy with the service and quality of products.

These points inform the pilot priorities. ShopPulse does not claim that every spaza shop has the same challenges; the solution is being shaped around the feedback received from this owner.

## 2. The solution

ShopPulse is a mobile-friendly retail operations prototype built with **.NET 10, ASP.NET Core Web API, Blazor WebAssembly, Entity Framework Core and PostgreSQL**. The hosted API runs on Railway, the web frontend on Cloudflare Pages and the production database on Neon.

The system brings key shop tasks into one workflow:

- **Product catalogue:** maintain product names, item codes/SKUs and selling prices in South African Rand (ZAR).
- **Stock receiving:** record received quantities, supplier/reference details, unit cost and batch expiry dates.
- **Expiry and waste management:** view expired, expiring-soon and safe stock; record batch-specific waste with reasons and an audit record.
- **Stock Rescue:** surface rules-based suggestions to prioritise stock that may expire before it sells. Suggestions do not silently change stock or prices.
- **Checkout and refunds:** process sales with server-side stock validation, FEFO batch allocation, transaction records and supported full-refund workflows.
- **Cash shifts and reporting:** record cash movements, reconcile shifts and review daily business summaries.
- **Owner/operator access:** owners manage staff and permissions; operators use individual accounts with permissions enforced by the API.

## 3. How the pilot addresses the owner's feedback

**Reduce food waste and protect product quality.** During receiving, staff should capture the expiry date for applicable food batches. The operating practice for the pilot is to review products expiring within the next **two days**, prioritise those items for appropriate action, and record any expired or damaged stock as waste. ShopPulse provides batch expiry visibility and waste recording; the two-day review is the proposed shop procedure and must be validated in the live pilot rather than represented as an independently verified automated alert threshold.

**Make customer experience more visible.** The current prototype focuses on retail operations and does not yet provide a complete customer satisfaction analytics module. The pilot should gather simple, voluntary customer feedback (for example, a short rating and optional comment) and review it with the owner. A future ShopPulse feature can summarise feedback trends without requiring customers to create accounts or disclose unnecessary personal information.

## 4. Future enhancement — QR/barcode scanning

A future scanning workflow could identify a product and its specific batch using:

1. Product name
2. Item code / SKU
3. Serial number or batch/lot number
4. Expiry date

At **receiving or stock inspection**, scanning should create or update the correct product/batch and capture expiry details; it must not reduce on-hand quantity merely because an item was scanned. At **checkout**, scanning an item should add it to the sale, and the successfully committed sale should automatically reduce the correct batch quantity using FEFO rules. This separation prevents accidental stock deductions during expiry checks and receiving.

## 5. Responsible scope and current limitations

ShopPulse is an assessment prototype, not yet approved for processing a shop's live commercial transactions. The README documents the implemented journeys and scope boundaries. The current home route is staff sign-in; a separate anonymous guest catalogue is not implemented. Card payments are recorded as operator-confirmed external-terminal payments; there is no payment-gateway integration. Full refunds are implemented; partial refunds are not. The current reporting is a daily summary, not a complete historical inventory analytics suite.

The codebase has automated API, authorization and database integration coverage. The latest PR CI run for commit `bc20c5439f1435df7eb18164c77af2bebbc3af7e` completed successfully (GitHub Actions run **45763994739**), and the developer also confirmed the full solution test command passed locally. This is not a claim that every browser workflow has passed live acceptance. The deployed application still requires a documented owner/operator end-to-end acceptance run, backup/restore exercise and security review before real shop use.

## 6. Pilot validation and success measures

The pilot should use synthetic or owner-approved test data and record a baseline before judging impact:

- Number and value of expired/written-off food items.
- Whether expiry dates are captured consistently at receiving.
- Whether staff review items due to expire within two days.
- Time needed to find stock and complete a sale.
- Customer feedback themes and simple satisfaction ratings, collected voluntarily.
- Owner/operator feedback on ease of use, errors and missing workflows.

The owner should review the results and decide which improvements matter most. No reduction in waste or increase in customer satisfaction is claimed until measured in the shop.

**Core proposition:** ShopPulse aims to help a small shop keep better track of stock, act earlier on food approaching expiry, record waste and gain a clearer picture of daily operations — while keeping the workflow practical for a small team.
