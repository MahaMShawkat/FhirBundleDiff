using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace FhirDiff.Core.Models
{
    public record BundleMatchResult(
    IReadOnlyList<ResourceKey> Added,
    IReadOnlyList<ResourceKey> Removed,
    IReadOnlyList<(ResourceKey Key, Resource Old, Resource New)> Matched);
}
