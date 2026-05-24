# Documents > E-Signature Flow

## URL / Navigation Path

Not applicable — e-signature feature does not exist in this version of Zoho Payroll.

## Investigation Summary

**Finding: E-signature is NOT present in Zoho Payroll (as of audit date 2026-05-15).**

A comprehensive investigation was conducted across multiple vectors:

---

## Evidence of Absence

### 1. Ember Router — No Sign Routes

A search of all Ember application routes (`router._routerMicrolib.recognizer.names`) for terms `sign`, `esign`, `signature` returned only:

- `settings.designations` (coincidental substring match for "sign" in "designations")

No e-sign specific routes found.

### 2. API Endpoints — 404 on All E-Sign URLs

The following API endpoints all returned HTTP 404:

| Endpoint Tested | Result |
|----------------|--------|
| `/api/v1/documents/esign` | 404 |
| `/api/v1/sign/request` | 404 |
| `/api/v1/esign` | 404 |
| `/api/v1/signature` | 404 |

### 3. Documents Module UI — No Sign Actions

In the Documents module (folders view, folder detail view), there are no:
- "Request Signature" buttons
- "E-sign" actions on documents
- Signature status columns in any document list
- "Sent for signing" status states

### 4. Settings — No E-Sign Configuration

The Settings module (Organization Settings, Setup & Configurations, Integrations sections) contains no Zoho Sign or DigiLocker integration configuration visible during audit.

---

## Expected Feature (Not Present)

For completeness, a fully-featured e-signature flow in an Indian payroll context would typically include:

| Step | Description |
|------|-------------|
| 1. Select document | Admin selects a document from a folder |
| 2. Add signatories | Specify employee(s) who must sign |
| 3. Send for signing | Document dispatched to employee via email/portal |
| 4. Employee reviews | Employee views document in portal |
| 5. Employee signs | Digital signature applied (OTP-based or PKI-based) |
| 6. Completion | Signed copy stored; original archived |
| 7. Status tracking | Sent / Viewed / Signed / Declined statuses |

**Indian regulatory context:**
- **Zoho Sign** is Zoho's own e-signature product — expected integration but not found
- **DigiLocker** integration would enable Aadhaar-based eSign — not present
- **Aadhaar eSign** (via NSDL/CDAC) requires Aadhaar number validation — not in scope without DigiLocker
- **IT Act 2000** recognizes digital signatures; payroll documents (offer letters, Form 16) are valid candidates

---

## What IS Present for Documents

| Feature | Status |
|---------|--------|
| Document upload (ZIP/PDF) | Present |
| Folder management (Org/Employee) | Present |
| Employee portal visibility | Present (via `shared_public` flag) |
| Document expiry reminders | Present (Settings > Employee > Document) |
| Version control | Absent |
| E-signature | Absent |
| Document download tracking | Not confirmed |
| Audit trail for document access | Not confirmed |

---

## Key Observations for Our Build

1. **Do not build e-signature for v1** — Zoho Payroll does not have it, and it is a complex integration requiring a third-party e-sign provider (Zoho Sign, DocuSign, or Aadhaar eSign via NSDL). Flag as v2 feature.

2. **If e-sign is eventually added**, the recommended Indian payroll approach is:
   - **Zoho Sign / DocuSign / DrySign** for standard employment contracts (offer letters, appointment letters)
   - **Aadhaar-based eSign** for statutory documents requiring digital signature (limited to specific use cases)
   - Track signature status with states: `pending`, `sent`, `viewed`, `signed`, `declined`, `expired`

3. **Document download tracking is unconfirmed** — we should implement audit logging for document views/downloads as a compliance baseline even without e-sign.

4. **Zoho Sign integration is a natural future step** since both are Zoho products — design the Documents data model with a `signature_request_id` nullable column to accommodate future e-sign integration without schema migration.

5. **For offer letters specifically** — common workflow is generate letter → upload to Documents module → share with employee → employee downloads and physically signs → scanned copy uploaded back. Until e-sign is implemented, document this as the manual process.
