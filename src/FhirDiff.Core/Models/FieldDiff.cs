using System.Text.Json;

namespace FhirDiff.Core.Models
{
    public class FieldDiff
    {
        public string FieldPath { get; set; }           // "" = whole resource (used for Added/Removed)
        public JsonElement? OldValue { get; set; }
        public JsonElement? NewValue { get; set; }
    }
}
