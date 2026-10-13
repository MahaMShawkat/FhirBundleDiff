using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace FhirDiff.Core.Models
{
    public class BundleDiffResult
    {
        public BundleDiffResult(List<ResourceChange> changes)
        {
            Changes = changes;
        }

        public List<ResourceChange> Changes { get; set; }
    }
}
