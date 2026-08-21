using FhirDiff.Core.Models;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using System.Text.Json;

namespace FhirDiff.Core.Services
{
    public class BundleDiffer
    {
        private const string IdFieldName = "id";
        private const string TypeFieldName = "resourceType";
        private const string MetaVersionIdFieldName = "meta.versionId";
        private const string MetaLastUpdatedFieldName = "meta.lastUpdated";

        public ResourceChange Diff((ResourceKey Key, Resource Old, Resource New) resource)
        {
            var listFieldChanged = new List<FieldDiff>();
            var serializer = new FhirJsonSerializer();

            var oldJson = JsonDocument.Parse(serializer.SerializeToString(resource.Old)).RootElement;
            var newJson = JsonDocument.Parse(serializer.SerializeToString(resource.New)).RootElement;

            foreach (var oldField in oldJson.EnumerateObject().Where(e => e.Name != IdFieldName && e.Name != TypeFieldName))
            {
                FindAllFieldsChanges(listFieldChanged, newJson, oldField, oldField.Name);
            }

            foreach (var newField in newJson.EnumerateObject().Where(e => e.Name != IdFieldName && e.Name != TypeFieldName))
            {
                var path = newField.Name;

                if (!oldJson.TryGetProperty(newField.Name, out _))
                {
                    FindAddedFieldsChanges(listFieldChanged, newField.Value, path);
                }
            }

            var changeType = listFieldChanged.Any() ? ChangeType.Modified : ChangeType.Unchanged;

            return new ResourceChange(resource.Key.ResourceType, resource.Key.Id, changeType, listFieldChanged);
        }

        public void FindAddedFieldsChanges(List<FieldDiff> listFieldChanged, JsonElement parentValue, string path)
        {
            switch (parentValue.ValueKind)
            {
                case JsonValueKind.Object:
                    {
                        foreach (var childField in parentValue.EnumerateObject())
                        {
                            FindAddedFieldsChanges(listFieldChanged, childField.Value, path + '.' + childField.Name);
                        }
                        break;
                    }
                case JsonValueKind.Array:
                    {
                        List<JsonElement> listChildrenField = parentValue.EnumerateArray().ToList();
                        foreach (var childField in listChildrenField)
                        {
                            FindAddedFieldsChanges(listFieldChanged, childField, path);
                        }
                        break;
                    }
                default:
                    {
                        if (path != MetaVersionIdFieldName && path != MetaLastUpdatedFieldName)
                        {
                            listFieldChanged.Add(new FieldDiff(path, null, parentValue));
                        }
                        break;
                    }
            }
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

                case JsonValueKind.Array:
                    {
                        FindArrayFieldsChanges(listFieldChanged, newJson, oldParentField, path);
                        break;
                    }

                default:
                    {
                        if (path != MetaVersionIdFieldName && path != MetaLastUpdatedFieldName)
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

        private void FindArrayFieldsChanges(List<FieldDiff> listFieldChanged, JsonElement newJson, JsonProperty oldParentField, string path)
        {
            if (newJson.TryGetProperty(oldParentField.Name, out var newFieldValue))
            {
                // Mutable pool of new-side items; items are removed from here once matched -> no new item can be matched twice.
                List<JsonElement> listNewFieldChildren = newFieldValue.EnumerateArray().ToList();

                // Pass 1: for each old item, look for an exact (GetRawText) match in the new pool.
                foreach (var oldArrayItem in oldParentField.Value.EnumerateArray())
                {
                    var match = listNewFieldChildren.FirstOrDefault(n => n.GetRawText().Equals(oldArrayItem.GetRawText()));

                    if (match.ValueKind != JsonValueKind.Undefined)
                    {
                        // Exact match found -> unchanged item, not a diff -> Remove from pool so it can't be reused as a match for another old item.
                        listNewFieldChildren.Remove(match);
                    }
                    else
                    {
                        // No match anywhere in new -> this old item was removed.
                        listFieldChanged.Add(new FieldDiff(path, oldArrayItem, null));
                    }
                }

                // Pass 2: whatever is left in the pool never matched an old item -> added.
                foreach (var newChild in listNewFieldChildren)
                {
                    listFieldChanged.Add(new FieldDiff(path, null, newChild));
                }
            }
            else
            {
                // The whole array field doesn't exist in new at all -> entire array removed as one unit.
                listFieldChanged.Add(new FieldDiff(path, oldParentField.Value, null));
            }
        }
    }
}