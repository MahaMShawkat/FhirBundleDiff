using FhirDiff.Core.Models;
using FhirDiff.Core.Services;
using FhirDiff.TestHelpers;


namespace FhirDiff.Core.Tests;


public class NullResourceTest
{
    private const string fileName = "null-resource-test.json";

    [Fact]
    public void LoadNullResourceBundle_ParsesSuccessfully()
    {
        // Arrange
        var bundle = BundlesFetcher.GetBundleFromFile(fileName);
        var matcher = new BundlesMatcher();
        // Act
        var resources = matcher.GetBundleResources(bundle);
        // Assert
        Assert.Single(resources.ResourcesWithId);
        Assert.Contains(new ResourceKey("Patient", "patient-1"), resources.ResourcesWithId.Keys);
        Assert.Equal("Observation", resources.ResourcesWithoutId[0].TypeName);
    }
}
