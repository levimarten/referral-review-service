# Decision log

## 001 Review and Send

**Context.** A review can be wrong or late.

**Options.** Make Send wait for review, or keep the two actions independent.

**Decision.** Give review results as advice. The existing EHR owns Send.

**Consequence.** The clinician can send while a review is pending or unavailable.

## 002 Checklists and quotes

**Context.** Clinics ask for different information. A gateway can invent or misread a quote.

**Options.** Let the gateway choose what to check, or use a checklist for each clinic.

**Decision.** The API selects a checklist and verifies quoted text against the input.

**Consequence.** A passing quote can still be irrelevant. The evaluation includes those cases.

## 003 Gateway connection

**Context.** The prototype needs a local demo. Deployment uses the approved Acme gateway.

**Options.** Run inference inside the Review API, or call a separate service.

**Decision.** The API calls a gateway over HTTP. The demo sends synthetic referrals to OpenAI and requests structured findings.

**Consequence.** The Acme gateway can replace OpenAI at the same boundary. Tests supply their own gateway replies.

## 004 Logs and limits

**Context.** Referral text is sensitive, and inference can stall.

**Options.** Log referral content for debugging, or log review metadata.

**Decision.** Log the review ID, result, duration, and error category. Limit request and response sizes, concurrent requests, and wait time. Enable NuGet dependency auditing.

**Consequence.** Use synthetic cases to investigate failures. Deployment still needs EHR access controls and auditing.

## 005 API contract

**Context.** The review needs a draft and selected supporting text.

**Options.** Expose a FHIR operation now, or map EHR data to a small JSON request.

**Decision.** Demo the JSON API with an HTTP client. Document the planned EHR screen and FHIR mapping.

**Consequence.** The API can be demonstrated before the EHR integration is built.