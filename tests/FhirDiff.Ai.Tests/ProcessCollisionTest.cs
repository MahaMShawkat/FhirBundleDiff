
using FhirDiff.Ai.Services;
using FhirDiff.Ai.Tests.AiTestHelpers;
using FhirDiff.Core.Models;
using FhirDiff.Core.Services;
using FhirDiff.TestHelpers;
using Hl7.Fhir.Model;
using System.Net;
using System.Text.Json;
using Task = System.Threading.Tasks.Task;

namespace FhirDiff.Ai.Tests
{
    public class ProcessCollisionTest
    {
        private const string OldFileName = "Process-collision-test-old-bundle.json";
        private const string NewFileName = "Process-collision-test-new-bundle.json";
        [Fact]
        public async Task Process_ModifiedAndDuplicateSameKey_BothChangesReturned()
        {
            // Arrange — load real bundles
            Bundle? oldBundle = BundlesFetcher.GetBundleFromFile(OldFileName);
            Bundle? newBundle = BundlesFetcher.GetBundleFromFile(NewFileName);
            string innerExplanations = """
                    [{"resourceType":"Observation","resourceId":"obs-dup-1","explanation":"Status changed from preliminary to final"}]
                    """;
            string geminiEnvelope = $$"""
                {
                  "candidates": [
                    { "content": { "parts": [ { "text": {{JsonSerializer.Serialize(innerExplanations)}} } ] } }
                  ]
                }
                """;
            HttpResponseMessage fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(geminiEnvelope)
            };
            FakeHttpMessageHandler fakeHandler = new FakeHttpMessageHandler(fakeResponse);
            HttpClient fakeHttpClient = new HttpClient(fakeHandler);

            GeminiDiffExplainer explainer = new GeminiDiffExplainer("fake-api-key", fakeHttpClient);
            BundleMatcher matcher = new BundleMatcher();
            BundleDiffer differ = new BundleDiffer();
            BundleDiffProcessor processor = new BundleDiffProcessor(matcher, differ, explainer);
            // Act
            BundleDiffResult resourceChanges = await processor.Process(oldBundle!, newBundle!);
            // Assert - Aggregates Counters
            Assert.Single(resourceChanges.Changes, c => c.ChangeType == ChangeType.Modified);
            Assert.Single(resourceChanges.Changes, c => c.ChangeType == ChangeType.Duplicate);
            //Assert - Modified and Duplicate Change Explanations
            ResourceChange modified = Assert.Single(resourceChanges.Changes, c => c.ChangeType == ChangeType.Modified);
            ResourceChange duplicate = Assert.Single(resourceChanges.Changes, c => c.ChangeType == ChangeType.Duplicate);
            Assert.Equal("Status changed from preliminary to final", modified.ChangeExplanation);
            Assert.Equal("Resource is a duplicate. Matching is inapplicable", duplicate.ChangeExplanation);
        }
    }
}
