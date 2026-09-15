using FhirDiff.Ai.Services;
using FhirDiff.Ai.Tests.AiTestHelpers;
using FhirDiff.Core.Models;
namespace FhirDiff.Ai.Tests
{
    public class DiffExplainerTest
    {

        [Fact]
        public void GetNonMatchedResources_MixedChangeTypes_ReturnsAddedRemovedAndNoId()
        {
            var listResourceChanges = ResourceChangeFixtures.GetResourceChanges();
            List<ResourceChange> resourceChanges = new List<ResourceChange> { listResourceChanges.Added, listResourceChanges.Removed, listResourceChanges.NoId, listResourceChanges.Modified, listResourceChanges.Unchanged };
            GeminiDiffExplainer explainer = new GeminiDiffExplainer("fake-key");

            List<ResourceChange> listUnmatchedResources = explainer.GetNonMatchedResources(resourceChanges);
            Assert.Equal(new List<ResourceChange> { listResourceChanges.Added, listResourceChanges.Removed, listResourceChanges.NoId }, listUnmatchedResources);
        }

        [Fact]
        public void GetMatchedResources_MixedChangeTypes_ReturnsOnlyModifiedAndUnchanged()
        {
            var listResourceChanges = ResourceChangeFixtures.GetResourceChanges();
            List<ResourceChange> resourceChanges = new List<ResourceChange> { listResourceChanges.Added, listResourceChanges.Removed, listResourceChanges.NoId, listResourceChanges.Modified, listResourceChanges.Unchanged };
            GeminiDiffExplainer explainer = new GeminiDiffExplainer("fake-key");

            List<ResourceChange> listMatchedResources = explainer.GetMatchedResources(resourceChanges);
            Assert.Equal(new List<ResourceChange> { listResourceChanges.Modified, listResourceChanges.Unchanged }, listMatchedResources);
        }
    }
}
