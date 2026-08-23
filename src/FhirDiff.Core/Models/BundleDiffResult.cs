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
        public int AddedCount => Changes.Count(c => c.ChangeType == ChangeType.Added);
        public int RemovedCount => Changes.Count(c => c.ChangeType == ChangeType.Removed);
        public int ModifiedCount => Changes.Count(c => c.ChangeType == ChangeType.Modified);
        public int UnchangedCount => Changes.Count(c => c.ChangeType == ChangeType.Unchanged);
        public int NoIdCount => Changes.Count(c => c.ChangeType == ChangeType.NoId);

    }
}
