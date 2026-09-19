using FhirDiff.Ai.Models;
using FhirDiff.Core.Models;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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
        private const string ModelName = "gemini-3.6-flash";
        private static readonly string Url = $"https://generativelanguage.googleapis.com/v1beta/models/{ModelName}:generateContent";
        private HttpClient _httpClient;
        private readonly string _apiKey;

        public GeminiDiffExplainer(string apiKey, HttpClient? httpClient = null)
        {
            _httpClient = httpClient ?? new HttpClient();
            _apiKey = apiKey;
            _httpClient.DefaultRequestHeaders.Add("x-goog-api-key", _apiKey);
        }

        // Orchestrator — splits, delegates, merges
        public async Task<List<ResourceChangeExplanation>> DescribeResourceChanges(IReadOnlyList<ResourceChange> resourceChanges)
        {
            var listResourceChangeExplanation = BuildHardcodedExplanations(GetNonMatchedResources(resourceChanges));
            var geminiRequestJson = BuildGeminiRequestJson(GetMatchedResources(resourceChanges));
            var LlmResponse = await CallLlmApi(geminiRequestJson);
            List<ResourceChangeExplanation> listAiExplanation = await ParseLlmResponse(LlmResponse);
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
        public List<ResourceChangeExplanation> BuildHardcodedExplanations(IReadOnlyList<ResourceChange> nonMatchedChanges)
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
                        listResourcesWithExplanation.Add(new ResourceChangeExplanation(resourceChange.ResourceType, resourceChange.ResourceId, "Resource has no Id. Matching is inapplicable"));
                        break;
                    default:
                        throw new ArgumentException($"Unexpected changeType '{resourceChange.ChangeType}' in non-matched resources.");
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
                "\nTreat changes to identifier, gender, birthDate, and deceased fields as generally high-impact. " +
                "Treat telecom, address, and text as generally benign. Treat maritalStatus and name as context-dependent — judge based on actual values changed." +
                "\nKeep explanations free of padding, examples, filler, intros/closings, exclamation points, hedging. Here are the resource changes: \n" +
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

        private async Task<string> CallLlmApi(string requestJson)
        {
            StringContent content = new StringContent(requestJson, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await _httpClient.PostAsync(Url, content);

            return await ExtractResultsFromContents(response);
        }

        private static async Task<string> ExtractResultsFromContents(HttpResponseMessage response)
        {
            string result = string.Empty;

            switch (response.StatusCode)
            {
                case System.Net.HttpStatusCode.OK:
                    {
                        result = await response.Content.ReadAsStringAsync();
                        break;
                    }
                case System.Net.HttpStatusCode.TooManyRequests:
                    {
                        result = "Too many requests. Try again later";
                        break;
                    }
                default:
                    {
                        response.EnsureSuccessStatusCode();
                        break;
                    }
            }

            return result;
        }

        public async Task<List<ResourceChangeExplanation>> ParseLlmResponse(string responseText)
        {
            JsonElement jsonResponse = JsonDocument.Parse(responseText).RootElement;
            string? textJson = jsonResponse.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
            List<ResourceChangeExplanation> explanations = JsonSerializer.Deserialize<List<ResourceChangeExplanation>>(textJson);

            return explanations;
        }
    }
}
