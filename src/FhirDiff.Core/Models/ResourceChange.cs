namespace FhirDiff.Core.Models
{
    public enum ChangeType
    {
        Added,
        Removed,
        Modified,
        Unchanged
    }

    public class ResourceChange
    {
        public ResourceChange() { }

        public ResourceChange(string resourceType, string resourceId, ChangeType changeType, List<FieldDiff> fieldChanges)
        {
            ResourceType = resourceType;
            ResourceId = resourceId;
            ChangeType = changeType;
            FieldChanges = fieldChanges;
        }

        public string ResourceType { get; set; }
        public string ResourceId { get; set; }
        public ChangeType ChangeType { get; set; }
        public List<FieldDiff> FieldChanges { get; set; }  // empty for Unchanged; single "" entry for Added/Removed; multiple for Modified
    }

}
