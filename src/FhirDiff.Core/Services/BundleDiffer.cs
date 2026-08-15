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
                FindAllFieldsChanges(listFieldChanged, newJson, oldField, oldField.Name);
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

        private void FindAllFieldsChanges(List<FieldDiff> listFieldChanged, JsonElement newJson, JsonProperty oldParentField, string path)
        {
            switch (oldParentField.Value.ValueKind)
            {
                case JsonValueKind.Object:
                    {
                        FindObjectFieldsChanges(listFieldChanged, newJson, oldParentField, path);
                        break;
                    }

                default:
                    {
                        if (newJson.TryGetProperty(oldParentField.Name, out var newValue))
                        {
                            if (!oldParentField.Value.GetRawText().Equals(newValue.GetRawText()))
                            {
                                listFieldChanged.Add(new FieldDiff(path, oldParentField.Value, newValue));
                            }
                        }
                        else
                        {
                            listFieldChanged.Add(new FieldDiff(path, oldParentField.Value, null));
                        }
                        break;
                    }
            }
        }

        private void FindObjectFieldsChanges(List<FieldDiff> listFieldChanged, JsonElement newJson, JsonProperty oldParentField, string path)
        {
            if (newJson.TryGetProperty(oldParentField.Name, out var newFieldValue))
                foreach (var oldChildField in oldParentField.Value.EnumerateObject())
                {
                    var childPath = path + "." + oldChildField.Name;
                    FindAllFieldsChanges(listFieldChanged, newFieldValue, oldChildField, childPath);
                }
            else
            {
                listFieldChanged.Add(new FieldDiff(path, oldParentField.Value, null));
            }
        }
    }
}