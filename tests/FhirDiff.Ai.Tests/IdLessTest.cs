
using FhirDiff.Ai.Services;
using FhirDiff.Ai.Tests.AiTestHelpers;
using FhirDiff.Core.Models;
using FhirDiff.Core.Services;
using FhirDiff.TestHelpers;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Extensions.Configuration;
using System.Diagnostics.Metrics;
using System.Net;
using System.Text.Json;
using Task = System.Threading.Tasks.Task;

namespace FhirDiff.Ai.Tests
{
    public class IdLessTest
    {
        private const string OldFileName = "IdLess-test-old-bundle.json";
        private const string NewFileName = "IdLess-test-new-bundle.json";
        [Fact]
        public async Task Process_FakeApiCallOnSeveralIdLessResources_NoIdChangesGetExplanation()
        {
            // Arrange — load real bundles
            Bundle? oldBundle = BundlesFetcher.GetBundleFromFile(OldFileName);
            Bundle? newBundle = BundlesFetcher.GetBundleFromFile(NewFileName);
            string innerExplanations = """
                []
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
            BundlesMatcher matcher = new BundlesMatcher();
            BundleDiffer differ = new BundleDiffer();
            BundleDiffProcessor processor = new BundleDiffProcessor(matcher, differ, explainer);
            // Act
            BundleDiffResult resourceChanges = await processor.Process(oldBundle!, newBundle!);
            // Assert - Aggregates Counters
            Assert.Equal(3, resourceChanges.Changes.Where(c => c.ChangeType == ChangeType.NoId).Count());
            //Assert - NoId
            IEnumerable<ResourceChange> noIdChanges = resourceChanges.Changes.Where(c => c.ChangeType == ChangeType.NoId);
            Assert.All(noIdChanges, c => Assert.Equal("Resource has no Id. Matching is inapplicable", c.ChangeExplanation));
        }
    }
}
