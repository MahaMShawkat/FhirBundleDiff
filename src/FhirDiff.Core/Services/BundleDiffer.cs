using FhirDiff.Core.Models;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using System.Text.Json;

namespace FhirDiff.Core.Services
{
    internal class BundleDiffer
    {
        private const string ID_FIELD_NAME = "id";
        private const string TYPE_FIELD_NAME = "resourceType";

        public ResourceChange BundleDiffProcess((ResourceKey Key, Resource Old, Resource New) resource)
        {
            var listFieldChanged = new List<FieldDiff>();
            var serializer = new FhirJsonSerializer();

            var oldJson = JsonDocument.Parse(serializer.SerializeToString(resource.Old)).RootElement;
            var newJson = JsonDocument.Parse(serializer.SerializeToString(resource.New)).RootElement;

            foreach (var oldField in oldJson.EnumerateObject().Where(e => e.Name != ID_FIELD_NAME && e.Name != TYPE_FIELD_NAME))
            {
                var path = oldField.Name;

                if (newJson.TryGetProperty(oldField.Name, out var newValue))
                {
                    if (!oldField.Value.GetRawText().Equals(newValue.GetRawText()))
                    {
                        listFieldChanged.Add(new FieldDiff(path, oldField.Value, newValue));
                    }
                }
                else
                {
                    listFieldChanged.Add(new FieldDiff(path, oldField.Value, null));
                }
            }

            foreach (var newField in newJson.EnumerateObject().Where(e => e.Name != ID_FIELD_NAME && e.Name != TYPE_FIELD_NAME))
            {
                var path = newField.Name;

                if (!oldJson.TryGetProperty(newField.Name, out _))
                {
                    listFieldChanged.Add(new FieldDiff(path, null, newField.Value));
                }
            }

            var changeType = listFieldChanged.Any() ? ChangeType.Modified : ChangeType.Unchanged;

            return new ResourceChange(resource.Key.ResourceType, resource.Key.Id, changeType, listFieldChanged);
        }
    }
}