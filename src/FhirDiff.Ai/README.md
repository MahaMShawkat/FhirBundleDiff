# FhirBundleDiff

Compare two FHIR R4 bundles and get a field-level diff with plain-language explanations of what changed and why it matters.

Built for healthcare developers and QA testers who need to verify FHIR data changes quickly.

> **Beta.** The public API may change between releases.
> **Requires .NET 10.** Your project must target `net10.0` or later.

## Data privacy

This tool sends the contents of matched resources to Gemini to generate explanations. 
If your key is on the free tier, Google may use the data to improve its products, and human reviewers may read it. 
Do not send real patient data on a free-tier key. Use a paid key, de-identified data, or synthetic data such as Synthea bundles.

## Install

```
dotnet add package MahaMS.FhirBundleDiff --prerelease
```

## Quick start

```csharp
using FhirDiff.Core.Services;
using FhirDiff.Ai.Services;

var processor = new BundleDiffProcessor(
    new BundleMatcher(),
    new BundleDiffer(),
    new GeminiDiffExplainer(apiKey));

BundleDiffResult result = await processor.Process(oldBundle, newBundle);

foreach (var change in result.Changes)
{
    Console.WriteLine($"{change.ChangeType} {change.ResourceType}/{change.ResourceId}");
    Console.WriteLine(change.ChangeExplanation);
}
```

`oldBundle` and `newBundle` are Firely SDK `Bundle` objects (`Hl7.Fhir.R4`).

## How it works

1. **Match**: resources are paired by `ResourceType` + `id` and classified as Added, Removed or Matched.
2. **Diff**: matched pairs are compared field by field, recursing into nested objects and arrays. `meta.versionId` and `meta.lastUpdated` are ignored as noise.
3. **Explain**: modified resources are sent to Gemini, which flags clinically meaningful changes. Added, Removed and NoId resources get fixed local messages, with no API call.

The diff engine does not depend on AI. `IDiffExplainer` can be replaced with your own implementation.

## Change types

| Type | Meaning |
|---|---|
| Added | Resource exists only in the new bundle |
| Removed | Resource exists only in the old bundle |
| Modified | Matched resource with field differences |
| Unchanged | Matched resource with no differences |
| NoId | Resource has no `id`, so it cannot be matched |

## Configuration

Provide a Gemini API key from [Google AI Studio](https://aistudio.google.com/). Keep it out of source control, for example in user secrets or an environment variable.

Transient `503` responses are retried up to 3 times.

## Known limitations

- A partial change inside an array element is reported as one Removed and one Added entry, not as Modified.
- Resources that repeat the same `ResourceType` + `id` within one bundle are not diffed and do not appear in the result.
- Bundle entries without a `resource` are skipped.
- The Gemini model name is hardcoded in this release.
- Rate-limit responses (HTTP 429) are not retried and throw an exception. Free-tier keys reach this limit quickly.

## License

MIT
