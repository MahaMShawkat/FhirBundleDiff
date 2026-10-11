using FhirDiff.Core.Models;
using FhirDiff.Core.Services;
using FhirDiff.TestHelpers;
using Hl7.Fhir.Model;


namespace FhirDiff.Core.Tests;


public class DuplicatedResourceTest
{
    private const string fileName = "duplicated-resource-test.json";

    [Fact]
    public void GetBundleResources_DuplicatedResources_AreDetected()
    {
        // Arrange
        Bundle bundle = BundlesFetcher.GetBundleFromFile(fileName);
        BundlesMatcher matcher = new BundlesMatcher();
        // Act
        BundleResources resources = matcher.GetBundleResources(bundle);
        // Assert
        Assert.Equal(2, resources.ResourcesWithId.Count);
        Assert.Contains(new ResourceKey("Patient", "patient-1"), resources.ResourcesWithId.Keys);
        Observation firstObs = (Observation)resources.ResourcesWithId[new ResourceKey("Observation", "Obs-1")];
        Assert.Equal(ObservationStatus.Final, firstObs.Status);

        Resource duplicatedObservationResource = resources.DuplicateResources.Single(t => t.Key.Equals(new ResourceKey("Observation", "Obs-1"))).Resource;
        Assert.Equal(ObservationStatus.Amended, ((Observation)duplicatedObservationResource).Status);
        Assert.Single(resources.DuplicateResources); 
    }
}
