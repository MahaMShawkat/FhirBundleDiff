using FhirDiff.Ai.Models;
using FhirDiff.Core.Models;
using FhirDiff.Core.Services;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace FhirDiff.Ai.Services
{
    public class BundleDiffProcessor
    {
        private readonly BundlesMatcher _matcher;
        private readonly BundleDiffer _differ;
        private readonly IDiffExplainer _explainer;

        public BundleDiffProcessor(BundlesMatcher matcher, BundleDiffer differ, IDiffExplainer explainer)
        {
            _matcher = matcher;
            _differ = differ;
            _explainer = explainer;
        }

        public async Task<BundleDiffResult> Process(Bundle oldBundle, Bundle newBundle)
        {
            var resourceChanges = new List<ResourceChange>();
            var matchResults = _matcher.Match(oldBundle, newBundle);
            
            foreach (var addedResource in matchResults.Added) 
            {
                var newValue = JsonDocument.Parse(new FhirJsonSerializer().SerializeToString(addedResource.resource)).RootElement;
                var fieldChange = new FieldDiff(addedResource.resource.TypeName, null, newValue);
                resourceChanges.Add(new ResourceChange(addedResource.resource.TypeName, addedResource.resource.Id, ChangeType.Added, new List<FieldDiff> { fieldChange }));
            }

            foreach (var removedResource in matchResults.Removed) 
            {
                var oldValue = JsonDocument.Parse(new FhirJsonSerializer().SerializeToString(removedResource.resource)).RootElement;
                var fieldChange = new FieldDiff(removedResource.resource.TypeName, oldValue, null);
                resourceChanges.Add(new ResourceChange(removedResource.resource.TypeName, removedResource.resource.Id, ChangeType.Removed, new List<FieldDiff> { fieldChange }));
            }

            foreach (var newNonIdResource in matchResults.NewResourcesWithoutId) //non-id to be inserted as is. no diff to be called there. AddedCount increased by one
            {
                resourceChanges.Add(new ResourceChange(newNonIdResource.TypeName, null, ChangeType.NoId, new List<FieldDiff>()));
            }

            foreach (var oldNonIdResource in matchResults.OldResourcesWithoutId) //non-id to be inserted as is. no diff to be called there. AddedCount increased by one
            {
                resourceChanges.Add(new ResourceChange(oldNonIdResource.TypeName, null, ChangeType.NoId, new List<FieldDiff>()));
            }

            foreach (var matchedResource in matchResults.Matched)
            {
                resourceChanges.Add(_differ.Diff((matchedResource.Key, matchedResource.Old, matchedResource.New)));
            }

            var diffResults = new BundleDiffResult(resourceChanges);
            List<ResourceChangeExplanation> explanations = await _explainer.DescribeResourceChanges(resourceChanges);
            foreach (ResourceChangeExplanation explanation in explanations)
            {
                var resource = resourceChanges.Where(e => e.ResourceId == explanation.ResourceId && e.ResourceType == explanation.ResourceType).Single();
                resource.ChangeExplanation = explanation.Explanation;
            }

            return diffResults;
        }
    }
}
