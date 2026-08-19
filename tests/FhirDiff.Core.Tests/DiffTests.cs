using FhirDiff.Core.Models;
using FhirDiff.Core.Services;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using System.Text.Json;

namespace FhirDiff.Core.Tests;

public class DiffTests
{
    private const string OldFileName = "diff-test-old-bundle.json";
    private const string NewFileName = "diff-test-new-bundle.json";

    [Fact]
    public void Diff_MixedBundles_DetectsAddedRemovedModifiedAndUnchangedCorrectly()
    {
        var parser = new FhirJsonParser();
        var differ = new BundleDiffer();
        var oldFilePath = Path.Combine(AppContext.BaseDirectory, "TestData", OldFileName);
        var newFilePath = Path.Combine(AppContext.BaseDirectory, "TestData", NewFileName);
        var oldJson = File.ReadAllText(oldFilePath);
        var newJson = File.ReadAllText(newFilePath);
        var oldBundle = parser.Parse<Bundle>(oldJson);
        var newBundle = parser.Parse<Bundle>(newJson);

        var diff = differ.Diff((new ResourceKey("Patient", "patient-1"), oldBundle.Entry[0].Resource, newBundle.Entry[0].Resource));
        var changes = diff.FieldChanges;

        var expectedGenderOld = JsonDocument.Parse("\"female\"").RootElement;
        var expectedGenderNew = JsonDocument.Parse("\"male\"").RootElement;

        // Assert - gender is modified
        Assert.Contains(changes, f =>
            f.FieldPath == "gender" &&
            f.OldValue.Value.GetRawText() == expectedGenderOld.GetRawText() &&
            f.NewValue.Value.GetRawText() == expectedGenderNew.GetRawText());

        var expectedBirthdateOld = JsonDocument.Parse("\"1980-01-01\"").RootElement;

        // Assert - birthdate removed
        Assert.Contains(changes, f =>
            f.FieldPath == "birthDate" &&
            f.OldValue.Value.GetRawText() == expectedBirthdateOld.GetRawText() &&
            f.NewValue == null);

        var expectedDeceasedBooleanNew = JsonDocument.Parse("false").RootElement;

        // Assert - deceasedBoolean added
        Assert.Contains(changes, f =>
            f.FieldPath == "deceasedBoolean" &&
            f.OldValue == null &&
            f.NewValue.Value.GetRawText() == expectedDeceasedBooleanNew.GetRawText());

        var expectedOldCoding = JsonDocument.Parse("{\"system\":\"http://terminology.hl7.org/CodeSystem/v3-MaritalStatus\",\"code\":\"S\",\"display\":\"Never Married\"}").RootElement;
        var expectedNewCoding = JsonDocument.Parse("{\"system\":\"http://terminology.hl7.org/CodeSystem/v3-MaritalStatus\",\"code\":\"M\",\"display\":\"Married\"}").RootElement;

        // Assert — old coding item recorded as removed
        Assert.Contains(changes, f =>
            f.FieldPath == "maritalStatus.coding" &&
            f.OldValue.Value.GetRawText() == expectedOldCoding.GetRawText() &&
            f.NewValue == null);

        // Assert — new coding item recorded added
        Assert.Contains(changes, f =>
            f.FieldPath == "maritalStatus.coding" &&
            f.OldValue == null &&
            f.NewValue.Value.GetRawText() == expectedNewCoding.GetRawText());

        var expectedOldManagingOrganization = JsonDocument.Parse("{\"reference\":\"Organization/org-1\"}").RootElement;

        // Assert — old ManagingOrganization item recorded removed
        Assert.Contains(changes, f =>
            f.FieldPath == "managingOrganization" &&
            f.OldValue.Value.GetRawText() == expectedOldManagingOrganization.GetRawText() &&
            f.NewValue == null);

        var expectedNewProfileItem = JsonDocument.Parse("\"http://hl7.org/fhir/StructureDefinition/Patient\"").RootElement;

        // Assert
        Assert.Contains(changes, f =>
            f.FieldPath == "meta.profile" &&
            f.OldValue == null &&
            f.NewValue.Value.GetRawText() == expectedNewProfileItem.GetRawText()
            );

        var expectedOldTelecom = JsonDocument.Parse("[{\"system\":\"phone\",\"value\":\"555-0100\"}]").RootElement;

        // Assert — telecom array removed entirely
        Assert.Contains(changes, f =>
            f.FieldPath == "telecom" &&
            f.OldValue.Value.GetRawText() == expectedOldTelecom.GetRawText() &&
            f.NewValue == null);

        var expectedNewCity = JsonDocument.Parse("\"Montreal\"").RootElement;
        var expectedNewCountry = JsonDocument.Parse("\"CA\"").RootElement;

        // Assert — address.city added
        Assert.Contains(changes, f =>
            f.FieldPath == "address.city" &&
            f.OldValue == null &&
            f.NewValue.Value.GetRawText() == expectedNewCity.GetRawText());

        // Assert — address.country added
        Assert.Contains(changes, f =>
            f.FieldPath == "address.country" &&
            f.OldValue == null &&
            f.NewValue.Value.GetRawText() == expectedNewCountry.GetRawText());
    }
}
