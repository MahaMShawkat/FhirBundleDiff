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
                var resourceKey = new ResourceKey(entry.Resource.TypeName, entry.Resource.Id);
                bundleResources.Add(resourceKey, entry.Resource);

            }
            return bundleResources;
        }

    }
}
