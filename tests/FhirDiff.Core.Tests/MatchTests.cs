using FhirDiff.Core.Models;
using FhirDiff.Core.Services;
using FhirDiff.Core.Tests.CoreTestHelpers;


namespace FhirDiff.Core.Tests;


public class MatchTests
{
    private const string OldFileName = "match-test-old-bundle.json";
    private const string NewFileName = "match-test-new-bundle.json";

    [Fact]
    public void Match_MixedBundles_ClassifiesAddedRemovedAndMatchedCorrectly()
    {
        var oldBundle = BundlesFetcher.GetBundleFromFile(OldFileName);
        var newBundle = BundlesFetcher.GetBundleFromFile(NewFileName);
        var matcher = new BundlesMatcher();

        var matchResults = matcher.Match(oldBundle, newBundle);        
        var expectedRemovedKey = new ResourceKey("Observation", "obs-removed-1");
        var expectedAddedKey = new ResourceKey("Observation", "obs-added-1");
        var expectedMatchedKey = new ResourceKey("Patient", "patient-1");

        Assert.Contains(matchResults.Added, a => a.key == expectedAddedKey);
        Assert.Contains(matchResults.Removed, r => r.key == expectedRemovedKey);

        var matchedPair = matchResults.Matched.Single(m => m.Key == expectedMatchedKey);

        Assert.Same(oldBundle.Entry[0].Resource, matchedPair.Old);
        Assert.Same(newBundle.Entry[0].Resource, matchedPair.New);
        Assert.Contains(matchResults.OldResourcesWithoutId, r => r.Id == null);
        Assert.Equal(1, matchResults.Added.Count);
        Assert.Equal(1, matchResults.Removed.Count);
        Assert.Equal(1, matchResults.Matched.Count);

    }
}
