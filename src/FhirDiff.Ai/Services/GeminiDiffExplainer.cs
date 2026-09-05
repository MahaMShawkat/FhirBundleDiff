using FhirDiff.Ai.Models;
using FhirDiff.Core.Models;
using System.Threading.Tasks;
using System.Text.Json;

namespace FhirDiff.Ai.Services
{
    public class GeminiDiffExplainer : IDiffExplainer
    {
        private static readonly string JsonContentPart = "{{\"contents\":[{{\"parts\":[{{\"text\": {0} }}]}}]";
        private static readonly string JsonGenerationConfigPart = ",\"generationConfig\": {{\"responseMimeType\": \"application/json\", \"responseSchema\": {0}}}}}";
        private static readonly string Config = "{\"type\": \"ARRAY\"," +
                                                "\"items\": {" +
                                                "\"type\": \"OBJECT\"," +
                                                "\"properties\": {" +
                                                "\"resourceType\": { \"type\": \"STRING\" }," +
                                                "\"resourceId\": { \"type\": \"STRING\" }," +
                                                "\"explanation\": { \"type\": \"STRING\" }}," +
                                                "\"required\": [\"resourceType\", \"resourceId\", \"explanation\"]" +
                                                "}}";

        // Orchestrator — splits, delegates, merges
        public async Task<List<ResourceChangeExplanation>> DescribeResourceChanges(IReadOnlyList<ResourceChange> resourceChanges)
        {
            var listResourceChangeExplanation = BuildHardcodedExplanations(GetNonMatchedResources(resourceChanges));
            var geminiRequestJson = BuildGeminiRequestJson(GetMatchedResources(resourceChanges));
            var listAiExplanation = await CallGeminiApi(geminiRequestJson);
            listResourceChangeExplanation.AddRange(listAiExplanation);

            return listResourceChangeExplanation;
        }

        // Filter — Matched only (public, reusable)
        public List<ResourceChange> GetMatchedResources(IReadOnlyList<ResourceChange> resourceChanges)
        {
            return resourceChanges.Where(r => r.ChangeType == ChangeType.Modified || r.ChangeType == ChangeType.Unchanged).ToList();
        }

        public List<ResourceChange> GetNonMatchedResources(IReadOnlyList<ResourceChange> resourceChanges)
        {
            return resourceChanges.Where(r => r.ChangeType == ChangeType.Added || r.ChangeType == ChangeType.Removed || r.ChangeType == ChangeType.NoId).ToList();
        }

        // Non-Matched (Added/Removed/NoId) → hardcoded template explanations
        private List<ResourceChangeExplanation> BuildHardcodedExplanations(IReadOnlyList<ResourceChange> nonMatchedChanges)
        {
            var listResourcesWithExplanation = new List<ResourceChangeExplanation>();

            foreach (var resourceChange in nonMatchedChanges)
            {
                switch (resourceChange.ChangeType)
                {
                    case ChangeType.Added:
                        listResourcesWithExplanation.Add(new ResourceChangeExplanation(resourceChange.ResourceType, resourceChange.ResourceId, "Resource is added"));
                        break;
                    case ChangeType.Removed:
                        listResourcesWithExplanation.Add(new ResourceChangeExplanation(resourceChange.ResourceType, resourceChange.ResourceId, "Resource is removed"));
                        break;
                    case ChangeType.NoId:
                        listResourcesWithExplanation.Add(new ResourceChangeExplanation(resourceChange.ResourceType, resourceChange.ResourceId, "Resource has no Id. Matching is Inapplicable"));
                        break;
                    default:
                        throw new ArgumentException($"Unexpected ChangeType '{resourceChange.ChangeType}' in non-Matched resources.");
                }
            }
            return listResourcesWithExplanation;
        }

        private string BuildGeminiRequestJson(IReadOnlyList<ResourceChange> matchedChanges)
        {
            string resourceDataJson = JsonSerializer.Serialize(matchedChanges);
            var promptWithResources = "You are assisting a healthcare software developer or QA tester validating changes between two versions of FHIR resources. " +
                "You will be given a list of resource changes, each with field-level differences." +
                " For each resource, assess whether its changes are likely meaningful/risky to downstream systems or likely benign — do not just restate the changes." +
                "\r\n\r\nTreat changes to identifier, gender, birthDate, and deceased fields as generally high-impact. " +
                "Treat telecom, address, and text as generally benign. Treat maritalStatus and name as context-dependent — judge based on actual values changed." +
                "\r\n\r\nKeep explanations free of padding, examples, filler, intros/closings, exclamation points, hedging. Here are the resource changes: \r\n\r\n" +
                resourceDataJson;
            string escaped = JsonSerializer.Serialize(promptWithResources);
            string content = string.Format(JsonContentPart, escaped);

            return AmendGenerationConfigJson(content);
        }

        private string AmendGenerationConfigJson(string contentJson)
        {
            string config = string.Format(JsonGenerationConfigPart, Config);
            return contentJson + config;
        }

        private async Task<List<ResourceChangeExplanation>> CallGeminiApi(string requestJson)
        {
            // To be implemented: HTTP call — sends prompt JSON, parses Gemini response
            return null;
        }
    }
}
