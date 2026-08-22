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

            var oldResourcesList = GetBundleResources(oldBundle);
            var newResourcesList = GetBundleResources(newBundle);

            foreach (var oldResource in oldResourcesList.ResourcesWithId)
            {
                ResourceKey key = oldResource.Key;
                if (!newResourcesList.ResourcesWithId.ContainsKey(key))
                    listRemovedResources.Add((key, oldResource.Value));
                else
                    listMatchedResources.Add((key, oldResource.Value, newResourcesList.ResourcesWithId[key]));
            }

            foreach (var newResource in newResourcesList.ResourcesWithId)
            {
                ResourceKey key = newResource.Key;
                if (!oldResourcesList.ResourcesWithId.ContainsKey(key))
                    listAddedResources.Add((key, newResource.Value));
            }

            return new BundleMatchResult(listAddedResources, listRemovedResources, listMatchedResources, oldResourcesList.ResourcesWithoutId, newResourcesList.ResourcesWithoutId);
        }
    }
}
