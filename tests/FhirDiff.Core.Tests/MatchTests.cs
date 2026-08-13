using FhirDiff.Core.Models;
using FhirDiff.Core.Models.Services;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using System.Linq;

namespace FhirDiff.Core.Tests;


public class MatchTests
{
    private const string oldFileName = "match-test-old-bundle.json";
    private const string newFileName = "match-test-new-bundle.json";

    [Fact]
    public void Match_MixedBundles_ClassifiesAddedRemovedAndMatchedCorrectly()
    {
        var parser = new FhirJsonParser();
        var matcher = new BundlesMatcher();
        var oldFilePath = Path.Combine(AppContext.BaseDirectory, "TestData", oldFileName);
        var newFilePath = Path.Combine(AppContext.BaseDirectory, "TestData", newFileName);
        var oldJson = File.ReadAllText(oldFilePath);
        var newJson = File.ReadAllText(newFilePath);
        var oldBundle = parser.Parse<Bundle>(oldJson);
        var newBundle = parser.Parse<Bundle>(newJson);

        var matchResults = matcher.Match(oldBundle, newBundle);
        var expectedRemovedKey = new ResourceKey("Observation", "obs-removed-1");
        var expectedAddedKey = new ResourceKey("Observation", "obs-added-1");
        var expectedMatchedKey = new ResourceKey("Patient", "patient-1");

        Assert.Contains(expectedAddedKey, matchResults.Added);
        Assert.Contains(expectedRemovedKey, matchResults.Removed);
        var matchedPair = matchResults.Matched.Single(m => m.Key == expectedMatchedKey);
        Assert.Same(oldBundle.Entry[0].Resource, matchedPair.Old);
        Assert.Same(newBundle.Entry[0].Resource, matchedPair.New);
        Assert.Equal(1, matchResults.Added.Count);
        Assert.Equal(1, matchResults.Removed.Count);
        Assert.Equal(1, matchResults.Matched.Count);

    }
}
