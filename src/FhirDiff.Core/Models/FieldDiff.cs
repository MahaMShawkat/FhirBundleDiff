using System.Text.Json;

namespace FhirDiff.Core.Models
{
    public class FieldDiff
    {
        public FieldDiff(string fieldPath, JsonElement? oldValue, JsonElement? newValue)
        {
            FieldPath = fieldPath;
            OldValue = oldValue;
            NewValue = newValue;
        }

        public string FieldPath { get; set; }           // "" = whole resource (used for Added/Removed)
        public JsonElement? OldValue { get; set; }
        public JsonElement? NewValue { get; set; }
    }
}
