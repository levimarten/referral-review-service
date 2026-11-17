# Referral Review Service

A .NET 8 prototype that checks synthetic referrals for missing or unclear information. Opening a draft in the CLI starts a review. Press Enter to simulate sending at any time.

## Quick start

With the .NET 8 SDK installed, start the offline baseline:

```powershell
dotnet run --project src/Api -- --baseline
```

In a second terminal, run a case or evaluate all 16:

```powershell
dotnet run --project src/Cli -- C01
dotnet run --project src/Cli -- --evaluate
dotnet run --project tests/ReviewTests
```

Try `C05` for an unavailable medication list and `O07` for a false positive. The offline baseline takes a sibling's X-ray as evidence for the patient in `O07`.

## Local model

Start llama.cpp with an instruction-tuned GGUF:

```powershell
.\llama-server.exe -m "C:\path\to\model.gguf" --alias referral-local --host 127.0.0.1 --port 8081
```

Point the API at the model and start it:

```powershell
$env:REFERRAL_MODEL_URL = 'http://127.0.0.1:8081/v1/chat/completions'
$env:REFERRAL_MODEL_NAME = 'referral-local'
dotnet run --project src/Api
```

The API also accepts HTTPS chat-completions endpoints. Set `REFERRAL_MODEL_KEY` in the API environment if the provider requires a bearer token. Use synthetic data only.

## Code map

| Path | Purpose |
| --- | --- |
| `src/Api` | `POST /review`, clinic profiles, inference and evidence checks |
| `src/Cli` | Demo and evaluation runner |
| `src/Contracts` | Shared request and response types |
| `tests/ReviewTests` | Review service and evidence assertions |
| `fixtures` | Synthetic referrals and expected results |
| [Decision log](docs/adr.md) | Architecture decisions and trade-offs |

The API binds to `127.0.0.1:5077`, caps request size and inference concurrency, and leaves referral content out of operational logs. The EHR send path is simulated. Local llama.cpp and the Acme gateway still need live integration tests.