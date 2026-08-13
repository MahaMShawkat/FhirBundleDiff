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

        public BundleMatchResult Match(Bundle oldBundle, Bundle newBundle)
        {
            List<ResourceKey> listAddedResources = new List<ResourceKey>();
            List<ResourceKey> listRemovedResources = new List<ResourceKey>();
            List<(ResourceKey Key, Resource Old, Resource New)> listMatchedResources = new List<(ResourceKey Key, Resource Old, Resource New)>();

            IReadOnlyDictionary<ResourceKey, Resource> oldResourcesWithIdList = GetBundleResources(oldBundle).ResourcesWithId;
            IReadOnlyDictionary<ResourceKey, Resource> newResourcesWithIdList = GetBundleResources(newBundle).ResourcesWithId;

            foreach (var oldResource in oldResourcesWithIdList)
            {
                ResourceKey key = oldResource.Key;
                if (!newResourcesWithIdList.ContainsKey(key))
                    listRemovedResources.Add(key);
                else
                    listMatchedResources.Add((key, oldResource.Value, newResourcesWithIdList[key]));
            }

            foreach (var newResource in newResourcesWithIdList)
            {
                ResourceKey key = newResource.Key;
                if (!oldResourcesWithIdList.ContainsKey(key))
                    listAddedResources.Add(key);
            }

            return new BundleMatchResult(listAddedResources, listRemovedResources, listMatchedResources);
        }
    }
}
