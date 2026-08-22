using FhirDiff.Core.Models;
using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace FhirDiff.Core.Services
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
            List<(ResourceKey key, Resource resource)> listAddedResources = new List<(ResourceKey key, Resource resource)>();
            List<(ResourceKey key, Resource resource)> listRemovedResources = new List<(ResourceKey key, Resource resource)>();
            List<(ResourceKey Key, Resource Old, Resource New)> listMatchedResources = new List<(ResourceKey Key, Resource Old, Resource New)>();

            IReadOnlyDictionary<ResourceKey, Resource> oldResourcesWithIdList = GetBundleResources(oldBundle).ResourcesWithId;
            IReadOnlyDictionary<ResourceKey, Resource> newResourcesWithIdList = GetBundleResources(newBundle).ResourcesWithId;

            foreach (var oldResource in oldResourcesWithIdList)
            {
                ResourceKey key = oldResource.Key;
                if (!newResourcesWithIdList.ContainsKey(key))
                    listRemovedResources.Add((key,oldResource.Value));
                else
                    listMatchedResources.Add((key, oldResource.Value, newResourcesWithIdList[key]));
            }

            foreach (var newResource in newResourcesWithIdList)
            {
                ResourceKey key = newResource.Key;
                if (!oldResourcesWithIdList.ContainsKey(key))
                    listAddedResources.Add((key,newResource.Value));
            }

            return new BundleMatchResult(listAddedResources, listRemovedResources, listMatchedResources);
        }
    }
}
