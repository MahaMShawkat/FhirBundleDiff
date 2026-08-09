using System;
using System.Collections.Generic;
using System.Text;

namespace FhirDiff.Core.Models
{
    public class BundleDiffResult
    {
        private List<ResourceChange> Changes { get; set; }
        public int AddedCount => Changes.Count(c => c.ChangeType == ChangeType.Added);
        public int RemovedCount => Changes.Count(c => c.ChangeType == ChangeType.Removed);
        public int ModifiedCount => Changes.Count(c => c.ChangeType == ChangeType.Modified);
        public int UnchangedCount => Changes.Count(c => c.ChangeType == ChangeType.Unchanged);

    }
}
