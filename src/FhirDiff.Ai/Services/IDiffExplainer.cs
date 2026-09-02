using FhirDiff.Ai.Models;
using FhirDiff.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FhirDiff.Ai.Services;

public interface IDiffExplainer
{
    Task<List<ResourceChangeExplanation>> DescribeResourceChanges(IReadOnlyList<ResourceChange> resourceChanges);
}