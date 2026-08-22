using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace FhirDiff.Core.Models
{
    public record BundleMatchResult(
    IReadOnlyList<(ResourceKey key, Resource resource)> Added,
    IReadOnlyList<(ResourceKey key, Resource resource)> Removed,
    IReadOnlyList<(ResourceKey Key, Resource Old, Resource New)> Matched,
    IReadOnlyList<Resource> OldResourcesWithoutId,
    IReadOnlyList<Resource> NewResourcesWithoutId);
}
