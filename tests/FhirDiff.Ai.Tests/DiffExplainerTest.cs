using FhirDiff.Ai.Models;
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

        [Fact]
        public void GetNonMatchedResources_MixedChangeTypes_ReturnsHardCodedResourceChangeExplanation()
        {
            //added, removed, and noId
            var listResourceChanges = ResourceChangeFixtures.GetResourceChanges();
            List<ResourceChange> resourceChanges = new List<ResourceChange> { listResourceChanges.Added, listResourceChanges.Removed, listResourceChanges.NoId };
            GeminiDiffExplainer explainer = new GeminiDiffExplainer("fake-key");

            //Act & Assert
            List<ResourceChangeExplanation> actualExplanations = explainer.BuildHardcodedExplanations(resourceChanges);
            List<ResourceChangeExplanation> expectedExplanations = new List<ResourceChangeExplanation>
            {
                new ResourceChangeExplanation ("Patient", "Patient-1", "Resource is added"),//Added
                new ResourceChangeExplanation("Observation", "Observation-1", "Resource is removed"),//Removed
                new ResourceChangeExplanation("Condition", null, "Resource has no Id. Matching is inapplicable")//NoId               
            };

            Assert.Equal(expectedExplanations, actualExplanations);

            //Modified Changes
            List<ResourceChange> modifiedResourceChanges = new List<ResourceChange> { listResourceChanges.Modified };

            // Act & Assert
            var modifiedException = Assert.Throws<ArgumentException>(() => explainer.BuildHardcodedExplanations(modifiedResourceChanges));
            Assert.Equal($"Unexpected changeType '{ChangeType.Modified}' in non-matched resources.", modifiedException.Message);

            //Unchanged
            List<ResourceChange> UnchangedResourceChanges = new List<ResourceChange> { listResourceChanges.Unchanged };

            // Act & Assert
            var unchangedException = Assert.Throws<ArgumentException>(() => explainer.BuildHardcodedExplanations(UnchangedResourceChanges));
            Assert.Equal($"Unexpected changeType '{ChangeType.Unchanged}' in non-matched resources.", unchangedException.Message);
        }
    }
}
