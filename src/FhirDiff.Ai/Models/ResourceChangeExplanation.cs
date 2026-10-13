
using FhirDiff.Core.Models;

namespace FhirDiff.Ai.Models
{
    public record ResourceChangeExplanation(string ResourceType, string? ResourceId, string Explanation, ChangeType? ChangeType = null);
}
