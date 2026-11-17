# What the tests check

Each test has three parts. **Given** sets up the example. **When** calls the code. **Then** checks what happened.

## Review request

| Test | What it checks |
| --- | --- |
| Valid draft | A draft with no extra sources is accepted. |
| Reserved ID | An extra source cannot use the ID `draft`. |
| Duplicate IDs | Two extra sources cannot share an ID. |
| Oversized draft | A draft over 12,000 characters is rejected. |

## Evidence

| Test | What it checks |
| --- | --- |
| Invented quote | A quote that does not appear in the draft is hidden. |
| Unknown source | A quote cannot be used if its source ID is unknown. |
| Unavailable list | “Medication list unavailable” does not count as medication information. |
| Partial quote | Leaving “unavailable” out of the quote does not pass the check. |
| Multiline quote | The check still spots “unavailable” on the next line. |
| Repeated quote | A phrase that appears twice cannot point to one passage. |

## Review service

| Test | What it checks |
| --- | --- |
| Supported finding | A supported answer for every item returns `no_gaps_found`. |
| Duplicate findings | Two answers to the same item leave it unverified. |
| Missing finding | An unanswered item is marked `unable_to_verify`. |
| Gateway failure | An HTTP error from the gateway returns `review_unavailable`. |

## HTTP API tests

The tests start the API in process and supply gateway replies through a test-only HTTP handler.

| Test | What it checks |
| --- | --- |
| 16 referral fixtures | Each request returns the expected review result and checklist statuses. |
| Empty draft | The endpoint returns HTTP 400. |
| Invented gateway quote | A test-only gateway reply makes up an ECG result. The API hides the quote and marks the check as unverified. |

Run the invented-quote case during the demo to show how the API handles a false positive.

```powershell
dotnet test ReferralReview.sln --filter InventedGatewayQuoteIsHidden
```

## Live demo results

One run with `gpt-5.4-mini-2026-03-17` returned results for all 16 synthetic referrals. It matched 15 of 16 overall results and 40 of 48 checklist labels. In case O06, the model accepted a vague statement about daily activities, so the API missed the gap.

A follow-up run after narrowing the output schema and clarifying the prompt matched all 16 overall results and 44 of 48 checklist labels. Four labels still differed, including the wrong-specialty question in O08.

Use the [demo workflow](Demo-workflow.md) to try the cases in Postman.