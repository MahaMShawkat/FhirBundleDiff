using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace FhirDiff.Core.Models
{
    public class BundlesMatcher
    {
        public Dictionary<ResourceKey, Resource> GetBundleResources(Bundle bundle)
        {
            Dictionary<ResourceKey, Resource> bundleResources = new Dictionary<ResourceKey, Resource>();

            foreach (var entry in bundle.Entry)
            {
                if (entry.Resource.Id is null)
                    throw new InvalidOperationException($"{entry.Resource.TypeName} Entry has no Id.Cannot match.");
                else
                {
                    var resourceKey = new ResourceKey(entry.Resource.TypeName, entry.Resource.Id);
                    bundleResources.Add(resourceKey, entry.Resource);
                }
            }
            return bundleResources;
        }

    }
}
