# FHIR Bundle Diff & Explainer

🚧 Work in progress — portfolio project

Reference doc consolidating all setup decisions.

## What it does
Compares two FHIR bundles structurally, then uses an LLM to generate a plain-language explanation of clinically significant changes.

## Tech Stack

- **Language/Runtime**: C#, .NET (latest LTS) — core requirement
- **FHIR parsing**: Firely .NET SDK (`Hl7.Fhir.R4`) — industry-standard, MIT licensed
- **FHIR version**: R4 — still dominant in production EHR systems; R5 adoption thin
- **AI/explanation**: Google Gemini API — 1,500 req/day on Flash, 1M token context
- **API**: ASP.NET Core Web API — exposes `/compare` endpoint
- **UI**: Blazor WASM — static hosting on GitHub Pages, full-stack C# story
- **IDE**: Visual Studio Community — suitable for individual/OSS use
- **Test data**: Synthea (synthetic patient records) — never real/de-identified patient data
- **Testing**: xUnit, mocked `IDiffExplainer` in unit tests — keeps CI free and fast

## Initial Architecture (subject to change)

```
FhirBundleDiff/
├── src/
│   ├── FhirDiff.Core/       # R4 parsing, structural diff (no AI)
│   ├── FhirDiff.Ai/         # IDiffExplainer interface + Gemini implementation
│   ├── FhirDiff.Api/        # ASP.NET Core Web API
│   └── FhirDiff.Web/        # Blazor WASM UI
├── tests/
├── samples/                 # Synthea-generated R4 bundle pairs
├── prompts/                 # versioned prompt templates
└── .github/workflows/       # CI: build + unit tests
```

Key design principle: diff engine is deterministic and AI-agnostic. `IDiffExplainer` interface makes the LLM backend swappable (Gemini now, could add Ollama/others later) and keeps unit tests free of live API calls.

## Data flow
1. Upload/paste two FHIR R4 bundles (JSON).
2. `FhirDiff.Core` produces structural diff: added/removed/modified resources, field-level changes.
3. Diff serialized into compact structured summary (not raw FHIR JSON) as prompt input.
4. `IDiffExplainer` (Gemini) returns structured explanation: summary, significant changes, plain-language text.
5. API returns combined JSON; Blazor UI renders diff table + explanation panel, color-coded by significance.

## Build order
1. R4 bundle parsing + structural diff (Core), unit tested.
2. Diff → prompt contract + `IDiffExplainer` with Gemini integration.
3. Web API `/compare` endpoint.
4. Blazor UI consuming the API.
5. Sample bundles (Synthea) + README with screenshot/GIF.
6. GitHub Actions CI (build + test).
7. Deploy Blazor WASM to GitHub Pages.

## Open items / future milestones
- Possible ML classifier module (clinically significant vs. administrative change) using scikit-learn or ML.NET — deferred, would need labeled sample data.
- Possible R5 support as a later milestone.
- Possible Ollama backend as a second `IDiffExplainer` implementation (offline/no-quota fallback).
