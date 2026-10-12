using FhirDiff.Core.Models;
using FhirDiff.Core.Services;
using FhirDiff.TestHelpers;
using Hl7.Fhir.Model;


namespace FhirDiff.Core.Tests;


public class MatchDuplicateTest
{
    private const string OldFileName = "match-duplicate-test-old-bundle.json";
    private const string NewFileName = "match-duplicate-test-new-bundle.json";

    [Fact]
    public void Match_RepeatedKeysInBundles_ClassifiesDuplicateCorrectly()
    {
        // Arrange
        Bundle oldBundle = BundlesFetcher.GetBundleFromFile(OldFileName);
        Bundle newBundle = BundlesFetcher.GetBundleFromFile(NewFileName);
        BundleMatcher matcher = new BundleMatcher();
        // Act
        BundleMatchResult matchResults = matcher.Match(oldBundle, newBundle);
        ResourceKey expectedDuplicateKey = new ResourceKey("Observation", "Obs-1");
        // Assert
        (ResourceKey Key, Resource Resource) duplicate = Assert.Single(matchResults.NewDuplicateResources);
        Assert.Equal(expectedDuplicateKey, duplicate.Key);
        Assert.Empty(matchResults.OldDuplicateResources);
    }
}
