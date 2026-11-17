# Architecture decision log

## 001 Advisory review

**Context.** A review can be wrong or arrive late.

**Options.** Block Send when review flags a gap, or keep review advisory.

**Decision.** Run review automatically. The clinician can send at any time through the EHR. The CLI simulates this path.

**Consequence.** A timeout or false warning cannot block Send. Clinicians decide how to act on findings.

## 002 Clinic checklists and model evidence

**Context.** Clinics may require different information. A model can invent a quote or misread a real one.

**Options.** Use keyword rules, let the model decide what is required, or give the model a clinic checklist.

**Decision.** The API selects a clinic profile and asks the model to assess each item. Positive findings require a unique verbatim excerpt from the supplied source.

**Consequence.** A new clinic can define its own checks. Exact matching confirms the quote exists. Semantic mistakes remain possible, so evaluation uses human-labelled cases.

## 003 Inference adapter

**Context.** The prototype needs to run locally, while the target design routes inference through Acme AI Gateway.

**Options.** Couple the API to one provider or use an inference interface.

**Decision.** Use a chat-completions adapter for configured endpoints and a keyword baseline for offline comparison. Use a deterministic mock for fault tests.

**Consequence.** A local model can exercise the same API path. The Acme gateway contract and live model performance still need verification.

## 004 Minimal logging and bounded requests

**Context.** Referral text is sensitive and inference can be slow.

**Options.** Log full requests for debugging or record only operational metadata.

**Decision.** Log a random review ID, mode, outcome, latency and error category. Cap request and response sizes, concurrent reviews and inference time. Enable direct and transitive NuGet auditing.

**Consequence.** Debugging relies on synthetic cases and review IDs. Production still needs EHR authorization, access auditing and a confirmed gateway retention policy.