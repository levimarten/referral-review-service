# Referral Review Service

A case-study proof of concept that checks synthetic GP-to-specialist referrals for missing or unclear information. OpenAI supplies findings for the demo. The API checks the quotes before returning them.

## Run locally

With the .NET 10 SDK, set `OPENAI_API_KEY` in the environment or in the ignored `.env` file at the repo root. The inference endpoint and default model are set in [appsettings.json](ReferralReview.Api/appsettings.json).

Run the API from the repo root.

```powershell
dotnet run --project ReferralReview.Api
```

The [demo workflow](docs/Demo-workflow.md) has two Postman request bodies and their expected results. The API listens on port 5077. Review requests send text to OpenAI, so use synthetic data only.

## Test

```powershell
dotnet test ReferralReview.sln
```

The suite tests the API with local test replies. It runs without an API key or a running demo. Build and test output goes into the ignored `artifacts/` folder.

## Read the code

- [How a review works](docs/review-api.md), including the planned EHR button and API fields
- [What each test checks](docs/tests.md)
- [Decision log](docs/adr.md)

## Future work

- Use MFA for GitHub and provider accounts, scope and rotate API keys, and protect `main` with required reviews and CI
- Use OAuth 2.0 for EHR callers. Validate issuer, audience, and scopes, check referral access, and audit access separately from operational logs
- Run CSharpier in CI and consider Husky hooks for pre-commit and pre-push checks
- Set up CI for pull requests with formatting and lint checks, a build, unit and HTTP integration tests, and NuGet security auditing
- Set up a frontend for demoing purposes
- Test with live streamed feedback
- Connect to the approved Acme gateway
- Add the EHR screen and FHIR integration
