using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace FhirDiff.Core.Models
{
    public record BundleResources(
    IReadOnlyDictionary<ResourceKey, Resource> ResourcesWithId,
    IReadOnlyList<Resource> ResourcesWithoutId);
}
