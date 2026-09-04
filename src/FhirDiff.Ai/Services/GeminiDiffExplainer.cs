using FhirDiff.Ai.Models;
using FhirDiff.Core.Models;
using System.Threading.Tasks;

namespace FhirDiff.Ai.Services
{
    public class GeminiDiffExplainer : IDiffExplainer
    {
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
            // To be implemented: Matched → prompt JSON (per Log#18 shape: field hints, schema)
            return null;
        }
       
        private async Task<List<ResourceChangeExplanation>> CallGeminiApi(string requestJson)
        {
            // To be implemented: HTTP call — sends prompt JSON, parses Gemini response
            return null;
        }
    }
}
