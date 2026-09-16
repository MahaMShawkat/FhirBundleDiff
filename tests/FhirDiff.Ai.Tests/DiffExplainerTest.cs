using FhirDiff.Ai.Models;
using FhirDiff.Ai.Services;
using FhirDiff.Ai.Tests.AiTestHelpers;
using FhirDiff.Core.Models;
using System.Net;
using System.Text;
using System.Text.Json;

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

        [Fact]
        public async Task DescribeResourceChanges_MixedChangeTypes_ReturnsHardcodedAndAiExplanations()
        {
            var listResourceChanges = ResourceChangeFixtures.GetResourceChanges();
            List<ResourceChange> resourceChanges = new List<ResourceChange> {
                listResourceChanges.Added,
                listResourceChanges.Removed,
                listResourceChanges.NoId,
                listResourceChanges.Modified,
                listResourceChanges.Unchanged
            };

            // Hand-built "AI" explanations — these represent what Gemini would return
            // for the two Matched resources (Modified, Unchanged)
            List<ResourceChangeExplanation> aiExplanations = new List<ResourceChangeExplanation>
            {
                new ResourceChangeExplanation("Patient", "Patient-2", "Gender changed from male to female"),
                new ResourceChangeExplanation("Observation", "Observation-2", "No change detected")
            };
            // Layer 1: the inner JSON — what ParseLlmResponse's second parse step deserializes.
            // This must match List<ResourceChangeExplanation>'s shape exactly.
            string innerJson = JsonSerializer.Serialize(aiExplanations);
            // Layer 2: Gemini's outer envelope. candidates[0].content.parts[0].text holds the inner JSON as a STRING VALUE so it must be escaped, not embedded raw.
            // JsonSerializer.Serialize(innerJson) turns the JSON string into a properly escaped JSON string literal (adds quotes + escapes internal quotes).
            string fakeGeminiResponseBody = $$"""
            {
              "candidates": [
                {
                  "content": {
                    "parts": [
                      { "text": {{JsonSerializer.Serialize(innerJson)}} }
                    ]
                  }
                }
              ]
            }
            """;
            // Fake HTTP handler returns the above body instead of calling the real network.
            // Build the HttpResponseMessage the fake handler will return
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(fakeGeminiResponseBody, Encoding.UTF8, "application/json")
            };

            // Fake HTTP handler returns the above response instead of calling the real network
            var fakeHandler = new FakeHttpMessageHandler(fakeResponse);
            var fakeHttpClient = new HttpClient(fakeHandler);
            // Inject fake client — same optional-HttpClient constructor pattern as DiffExplainerTest
            GeminiDiffExplainer explainer = new GeminiDiffExplainer("fake-key", fakeHttpClient);

            // Act
            List<ResourceChangeExplanation> actualExplanations = await explainer.DescribeResourceChanges(resourceChanges);

            // Assert — hardcoded (Added, Removed, NoId) + AI-sourced (Modified, Unchanged)
            // Order must match DescribeResourceChanges' internal merge order
            List<ResourceChangeExplanation> expectedExplanations = new List<ResourceChangeExplanation>
            {
                new ResourceChangeExplanation("Patient", "Patient-1", "Resource is added"),
                new ResourceChangeExplanation("Observation", "Observation-1", "Resource is removed"),
                new ResourceChangeExplanation("Condition", null, "Resource has no Id. Matching is inapplicable"),
                aiExplanations[0],
                aiExplanations[1]
            };

            Assert.Equal(expectedExplanations, actualExplanations);
        }
    }
}
