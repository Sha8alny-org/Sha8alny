# Unit 12: Payments — Getting Paid After the Work

> **Before reading this unit:** You should have read Unit 11 (Reviews and Certificates — what `Completed` status means). This unit explains how companies pay students after a job is completed.

---

## 12.1 The Business Problem: Who Pays Whom, and When?

On Sha8alny, payment is optional for some project types (Internships are always free — BidAmount = 0) but necessary for paid work (PartTime, FullTime, GraduationProject, Training). When a student's work is accepted and the job is marked `Completed`, the company owes the student their agreed `BidAmount`.

The payment flow is:
1. Company posts a paid project with a budget
2. Student bids an amount when applying
3. If accepted, the BidAmount becomes the agreed price
4. After the job is completed and marked `Completed`, the company pays the student through the platform

---

## 12.2 Two Data Models: Payment vs. Transaction

The codebase has two separate database tables for financial data — a common source of confusion:

**`Payment`** — designed for Paymob integration (the Egyptian payment gateway). It stores the external gateway's identifiers and raw responses:

```
Payments table:
PaymentID | ProjectID | StudentID | CompanyID
Amount | Currency | Status (PaymentStatus)
PaymobOrderId      ← ID from Paymob's "Order Registration" API
PaymobTransactionId ← ID from Paymob's "Webhook" after user pays
GatewayRawResponse ← raw JSON response from Paymob (for debugging)
PaymentMethod (PaymentMethod enum) | Description
CreatedAt | PaidAt | UpdatedAt
```

**`Transaction`** — the internal ledger record. Simpler, tracks who paid whom:

```
Transactions table:
Id | ApplicationId | PayerId (Company UserID) | PayeeId (Student UserID)
Amount | TransactionDate | PaymentMethod (string) | ReferenceId | Status (TransactionStatus)
```

**The current state:** `PaymentService.ProcessPaymentAsync` only creates `Transaction` records. The `Payment` entity exists in the codebase and database (it has its own migration) but is not yet used by the service. This reflects an architectural decision: the `Payment` model represents the planned Paymob integration (with full webhook support), while `Transaction` is the simplified version used in the current working implementation.

---

## 12.3 Paymob — What It Is and Why It's There

**Paymob** is the dominant Egyptian payment gateway — the Egyptian equivalent of Stripe or PayPal. It handles credit cards, Meeza (Egypt's national card), mobile wallets (Fawry, Vodafone Cash), and kiosk cash payments.

The `Payment` entity's Paymob fields tell the story of how the real integration would work:

**Step 1 — Order Registration:**
Before showing the payment form, the backend calls Paymob's API to register the intended transaction. Paymob responds with a `PaymobOrderId`. This ID ties all subsequent events back to this specific payment intention.

**Step 2 — User Pays:**
The frontend redirects the student (or company) to Paymob's hosted payment form. The user enters their card details on Paymob's servers (not Sha8alny's).

**Step 3 — Webhook Confirmation:**
After payment, Paymob sends an HTTP POST to Sha8alny's backend (a "webhook" — an automated callback). This webhook contains the `PaymobTransactionId` and success/failure status. The backend updates the `Payment` record accordingly.

The `GatewayRawResponse` field stores the raw JSON from Paymob's webhook — this is used when a company disputes a payment or a developer needs to debug a failed transaction.

**Why not fully implemented?** Real Paymob integration requires a registered business account, HTTPS webhooks, and sandbox credentials. For a graduation project, the team built the architecture and data model correctly but uses a mock implementation for actual payment processing.

---

## 12.4 The Current Payment Flow (Mock Implementation)

When TechCorp calls `POST /api/Payments/pay`:

```
1. [Authorize(Roles = "Company")] — only companies can pay

2. PaymentService.ProcessPaymentAsync(companyUserId, dto):
   a. Verify company profile exists
   b. Get the application by dto.ApplicationId
   c. Verify company owns the project (security check — prevent cross-company payment)
   d. GATE: application.Status must be Completed
   e. GATE: application.IsPaid must be false (prevents double payment)
   f. Get student info

3. Mock processing:
   await Task.Delay(500)  ← simulates network/processing delay
   
   if (dto.PaymentMethod == "FailTest")
       → return failure (for testing error paths)

4. Create Transaction record:
   PayerId = companyUserId
   PayeeId = student.UserID
   Amount = application.BidAmount ?? 0
   ReferenceId = Guid.NewGuid().ToString("N").ToUpper()  ← unique reference
   Status = TransactionStatus.Completed

5. Update application:
   application.IsPaid = true
   application.PaidAt = DateTime.UtcNow

6. SaveAsync()

7. Notify student:
   "You have received a payment of $500.00 for project 'Backend Internship'."
   + SignalR real-time push

8. Return PaymentReceiptDto:
   TransactionId, ReferenceId, Amount, Currency, Date,
   PayerName (company), PayeeName (student), ProjectName
```

---

## 12.5 Double-Payment Prevention

The service checks `application.IsPaid` before processing:

```csharp
if (application.IsPaid)
{
    return Failure("Payment has already been processed for this application.");
}
```

`IsPaid` is a boolean field on `Application`. After a successful payment, it is set to `true` permanently. This is a simple but effective idempotency guard — even if the company calls the endpoint twice, the second call fails with a clear error message.

---

## 12.6 PaymentStatus and TransactionStatus — Two Separate Enums

Both the `Payment` and `Transaction` models have their own status enum:

**`PaymentStatus`** (on the `Payment` entity):
`Pending → Processing → Completed / Failed / Refunded / Cancelled`

**`TransactionStatus`** (on the `Transaction` entity):
`Pending → Processing → Completed / Failed / Refunded`

In the current mock implementation, transactions are always created with `TransactionStatus.Completed` directly — there is no intermediate `Pending` state because the mock does not involve an asynchronous external system.

In a real Paymob integration, the flow would be:
1. Create `Payment` with status `Pending` (before redirect to Paymob)
2. On webhook confirmation → update to `Completed` or `Failed`

---

## 12.7 The "FailTest" Test Mode

The service has a deliberately visible test hook:

```csharp
if (dto.PaymentMethod.Equals("FailTest", StringComparison.OrdinalIgnoreCase))
{
    return Failure("Payment failed. Please try again or use a different payment method.");
}
```

Sending `"paymentMethod": "FailTest"` in the request body triggers a simulated payment failure. This allows the frontend and testing tools to exercise the error path without special mocking infrastructure. It is an in-code test mode, not a separate test environment.

---

## 12.8 What to Say in Your Defense

- "The system has two payment-related entities: `Payment` (designed for the full Paymob gateway integration, including webhook handling) and `Transaction` (the internal ledger record). Currently the mock `PaymentService` creates only `Transaction` records, but the `Payment` entity is ready for when real Paymob integration is activated."
- "Payment is gated by two conditions: the application must be `Completed`, and `application.IsPaid` must be `false`. This prevents premature payment and double-payment."
- "Paymob is Egypt's dominant payment gateway. The integration pattern is: register the order with Paymob → user pays on Paymob's hosted form → Paymob calls our webhook with confirmation → we store the transaction. The `PaymobOrderId` and `PaymobTransactionId` fields on `Payment` are designed to track this flow."
- "The `GatewayRawResponse` field stores the raw JSON webhook payload from Paymob. This is essential for payment dispute resolution — you always need the original confirmation from the gateway, not just your internal interpretation of it."
- "When payment succeeds, the student receives both a database notification (for later reading) and an immediate SignalR push. `application.IsPaid = true` and `application.PaidAt` are set atomically with the Transaction insert."

---

## 12.9 Self-Check Questions

**Q1: What is the difference between the `Payment` and `Transaction` tables?**
`Payment` is designed for Paymob integration — it stores gateway-specific IDs (`PaymobOrderId`, `PaymobTransactionId`) and the raw response JSON. `Transaction` is the internal ledger — who paid whom, how much, with a unique reference ID. Currently only `Transaction` is used in the working payment flow; `Payment` is ready for when real gateway integration is activated.

**Q2: What two conditions must be true before a payment can be processed?**
(1) `application.Status == ApplicationStatus.Completed` — work must be marked done. (2) `application.IsPaid == false` — prevents double payment. Both are checked before any money movement.

**Q3: What is Paymob and why is it in the codebase?**
Paymob is Egypt's leading payment gateway (similar to Stripe). The `Payment` entity's `PaymobOrderId`, `PaymobTransactionId`, and `GatewayRawResponse` fields are designed for a full Paymob integration: register order → redirect user → receive webhook. It is not yet active because a real business account and HTTPS webhook endpoint are required.

**Q4: What does `GatewayRawResponse` store, and why is it important?**
The raw JSON response from Paymob's webhook — the exact confirmation payload the gateway sent. This is stored for dispute resolution: if a company claims a payment failed but Paymob's logs show success, the raw response is the authoritative record.

**Q5: How is double-payment prevented?**
The `application.IsPaid` boolean is checked before processing. If `true`, the service returns "Payment has already been processed." After successful payment, `IsPaid` is set to `true`. This check runs before any `Transaction` insert.

**Q6: What does the "FailTest" payment method do?**
It is a test hook — sending `paymentMethod: "FailTest"` causes the service to return a payment failure response without creating a `Transaction` record. It simulates the error path for testing purposes, without needing special mock infrastructure.

**Q7: Who gets notified after a payment, and how?**
The student (payee) receives: (1) a `Notification` row saved to the database, and (2) a real-time SignalR push through `INotifier`. The notification message includes the amount and project name.
