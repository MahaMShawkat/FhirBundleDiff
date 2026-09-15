using FhirDiff.Core.Models;
using System.Text.Json;

namespace FhirDiff.Ai.Tests.AiTestHelpers
{
    public class ResourceChangeFixtures
    {
        public static (ResourceChange Added, ResourceChange Removed, ResourceChange NoId, ResourceChange Modified, ResourceChange Unchanged) GetResourceChanges()
        {
            ResourceChange addedResourceChange = new ResourceChange("Patient", "Patient-1", ChangeType.Added, new List<FieldDiff>());
            FieldDiff addedFieldDiff = new FieldDiff("Patient", null, JsonDocument.Parse("\"Sam\"").RootElement);
            addedResourceChange.FieldChanges.Add(addedFieldDiff);

            ResourceChange removedResourceChange = new ResourceChange("Observation", "Observation-1", ChangeType.Removed, new List<FieldDiff>());
            FieldDiff removedFieldDiff = new FieldDiff("Observation", JsonDocument.Parse("\"obs\"").RootElement, null);
            removedResourceChange.FieldChanges.Add(removedFieldDiff);

            ResourceChange noIdResourceChange = new ResourceChange("Condition", null, ChangeType.NoId, new List<FieldDiff>());

            ResourceChange modifiedResourceChange = new ResourceChange("Organization", "Organization-1", ChangeType.Modified, new List<FieldDiff>());
            FieldDiff modifiedFieldDiff = new FieldDiff("Organization", JsonDocument.Parse("\"org\"").RootElement, JsonDocument.Parse("\"organization1\"").RootElement);
            modifiedResourceChange.FieldChanges.Add(modifiedFieldDiff);

            ResourceChange unchangedResourceChange = new ResourceChange("Device", "Device-1", ChangeType.Unchanged, new List<FieldDiff>());

            return (addedResourceChange, removedResourceChange, noIdResourceChange, modifiedResourceChange, unchangedResourceChange);
        }
    }
}
