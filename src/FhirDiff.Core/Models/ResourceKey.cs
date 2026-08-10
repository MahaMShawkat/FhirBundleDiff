using System;
using System.Collections.Generic;
using System.Text;

namespace FhirDiff.Core.Models
{
    public record ResourceKey(string ResourceType, string? Id);
}
