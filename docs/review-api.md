# How a review works

The current demo starts with an HTTP request and ends with an HTTP response. Postman or [review.http](../ReferralReview.Api/review.http) can play the role of the EHR screen.

## Follow one request

1. **`ReviewEndpoints` receives the draft.** It checks the request and finds the right checklist in `review-profiles.json`. For cardiology, the checklist asks for a question, current medication, and a completed ECG result.
2. **`ReferralReviewService` asks for findings.** It gives the draft the source ID `draft` and passes the checklist and any extra text to `ChatCompletionsClient`.
3. **The client calls OpenAI over HTTPS.** OpenAI stands in for the AI gateway and returns a status and optional quote per checklist item.
4. **`EvidenceValidator` checks those quotes.** It checks where a quote came from and whether the words appear there. For example, “Medication list unavailable” cannot count as proof that the medication list is present. A valid quote can still be about the wrong patient.
5. **The API returns the findings.** The response contains a review ID, an overall result, and the status and evidence for each check.

## Future EHR screen

The EHR frontend is future work. When a clinician clicks **Review**, the screen would send the current draft and show **Checking…**. It would display the returned checklist items beside the draft. If the draft changes while the API is working, the screen would discard that old result. **Send** would remain a separate action in the EHR and would be available during review.

The future EHR integration can read a FHIR R4 `ServiceRequest` and permitted supporting resources. It would map the service and destination to a checklist and pass the text needed for that review. [HL7 FHIR R4 ServiceRequest](https://hl7.org/fhir/R4/servicerequest.html), [HL7 supporting information](https://hl7.org/fhir/R4/servicerequest-definitions.html#ServiceRequest.supportingInfo)

## Request and response

Send JSON to `POST http://127.0.0.1:5077/review`.

| Field | What to send |
| --- | --- |
| `profile` | `cardiology` or `orthopedics` |
| `referralType` | `specialist_consultation` |
| `draft` | Referral text, up to 12,000 characters |
| `supportingSources` | Up to five `{ "id", "text" }` entries, or `[]`. IDs must be unique. `draft` is reserved. |

A `200` response has a `reviewId`, a `result`, a `mode` of `gateway`, and `checks` keyed by checklist item. Each check has a `status` of `present`, `missing`, `unclear`, or `unable_to_verify`. Where available, it includes `sourceId` and `excerpt`.

| Result | Meaning |
| --- | --- |
| `no_gaps_found` | Every item is marked present with a checked quote. |
| `review_suggested` | An item is missing, unclear, or could not be verified. |
| `review_unavailable` | The gateway failed or timed out. |

`400` means the request is invalid. `429` means all review slots are busy. `GET /health` checks if the API is running. A review request also checks the connection to the gateway.

## Where things live

- `ReferralReview.Api/Contracts/Review` holds the request and response for the EHR screen.
- `ReferralReview.Api/Contracts/Gateway` holds the request and response for the inference gateway.
- `ChatCompletionsClient` calls OpenAI for the demo. Change the endpoint and model in [appsettings.json](../ReferralReview.Api/appsettings.json) to connect a gateway. Its key comes from `REFERRAL_MODEL_KEY`.
- `ReferralReview.Tests/` contains unit tests, HTTP API tests, and their synthetic fixtures. The API loads its checklist file at startup.