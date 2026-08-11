using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace FhirDiff.Core.Models
{
    public class BundlesMatcher
    {
        public BundleResources GetBundleResources(Bundle bundle)
        {
            var resourcesWithId = new Dictionary<ResourceKey, Resource>();
            var resourcesWithoutId = new List<Resource>();

            foreach (var entry in bundle.Entry)
            {
                if (entry.Resource.Id is null)
                {
                    resourcesWithoutId.Add(entry.Resource);
                }
                else
                {
                    var resourceKey = new ResourceKey(entry.Resource.TypeName, entry.Resource.Id);
                    resourcesWithId.Add(resourceKey, entry.Resource);
                }
            }
            return new BundleResources(resourcesWithId, resourcesWithoutId);
        }

    }
}
