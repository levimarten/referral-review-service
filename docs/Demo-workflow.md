# Demo workflow

## Start the API

Install the .NET 10 SDK. Create a file named `.env` beside `ReferralReview.sln` and put your OpenAI API key in it:

```dotenv
OPENAI_API_KEY=your_openai_api_key_here
```

Git ignores `.env`. Set the inference endpoint or model in [appsettings.json](../ReferralReview.Api/appsettings.json). The demo sends requests to OpenAI, so use synthetic data only.

From the repo root, run:

```powershell
dotnet run --project ReferralReview.Api
```

Check that `GET http://127.0.0.1:5077/health` returns `200 OK`.

## Send a review in Postman

Choose `POST http://127.0.0.1:5077/review`. Under **Body**, choose **raw** and **JSON**. Paste one request body at a time.

### 1. Complete cardiology referral

**Expected result** `no_gaps_found`. The draft includes a specific question, current medication, and a completed ECG finding.

```json
{
  "profile": "cardiology",
  "referralType": "specialist_consultation",
  "draft": "Could cardiology advise on further evaluation of exertional symptoms?\nCurrent medication: amlodipine.\nECG today: sinus rhythm.",
  "supportingSources": []
}
```

### 2. Orthopedics referral with vague evidence

**Expected result** `review_suggested`, with `checks.function.status` set to `unclear`. The draft says daily activities are affected without naming an activity or describing the impact.

```json
{
  "profile": "orthopedics",
  "referralType": "specialist_consultation",
  "draft": "Could orthopedics assess treatment options for persistent right knee pain?\nDaily activities are affected.\nRight knee X-ray report: mild degenerative changes.",
  "supportingSources": []
}
```

In the live run, the API returned `no_gaps_found` after the model treated “Daily activities are affected” as enough evidence. The words appear in the draft, so the quote passes the source check. The checklist asks for a specific daily activity. This is a useful failure to discuss if it recurs. For a repeatable failure, run the invented-quote test described in the [test guide](tests.md).

Press **Ctrl+C** in the API terminal when you finish.