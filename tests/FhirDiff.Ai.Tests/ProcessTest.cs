using FhirDiff.Core.Models;
using FhirDiff.Core.Services;
using FhirDiff.Ai.Services;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using System.Text.Json;

namespace FhirDiff.Ai.Tests
{
    public class ProcessTest
    {
        private const string OldFileName = "process-test-old-bundle.json";
        private const string NewFileName = "process-test-new-bundle.json";
        [Fact]
        public void Process_MixBundle_CheckBundleDiffResults()
        {
            var parser = new FhirJsonParser();
            var matcher = new BundlesMatcher();
            var differ = new BundleDiffer();
            var processor = new BundleDiffProcessor(matcher,differ, null);
            var oldFilePath = Path.Combine(AppContext.BaseDirectory, "TestData", OldFileName);
            var newFilePath = Path.Combine(AppContext.BaseDirectory, "TestData", NewFileName);
            var oldJson = File.ReadAllText(oldFilePath);
            var newJson = File.ReadAllText(newFilePath);
            var oldBundle = parser.Parse<Bundle>(oldJson);
            var newBundle = parser.Parse<Bundle>(newJson);

            var resourceChanges = processor.Process(oldBundle, newBundle);

            //Assert - Aggregates Counters
            Assert.Equal(4, resourceChanges.Changes.Count);

            //Assert - Added
            //rebuild and reserialize instead of hand-writing JSON so the expected value matches exactly what FhirJsonSerializer actually produces — no guessing property order or formatting.
            var expectedAddedObservation = new Observation //Build Observation SDK object, same shape as the fixture entry
            {
                Id = "obs-added-1",
                Status = ObservationStatus.Final,
                Code = new CodeableConcept { Text = "Added-only observation" }
            };
            var expectedAddedJson = new FhirJsonSerializer().SerializeToString(expectedAddedObservation);//Serialize it.
            var expectedAddedRawText = JsonDocument.Parse(expectedAddedJson).RootElement.GetRawText();//Parse 

            Assert.Contains(resourceChanges.Changes, f =>
                f.ResourceType == "Observation" &&
                f.ChangeType == ChangeType.Added &&
                f.FieldChanges[0].FieldPath == "Observation" &&
                f.FieldChanges[0].OldValue == null &&
                f.FieldChanges[0].NewValue.Value.GetRawText() == expectedAddedRawText
            );

            //Assert - Removed
            var expectedRemovedObservation = new Observation //Build Observation SDK object, same shape as the fixture entry
            {
                Id = "obs-removed-1",
                Status = ObservationStatus.Final,
                Code = new CodeableConcept { Text = "Removed-only observation" }
            };
            var expectedRemovedJson = new FhirJsonSerializer().SerializeToString(expectedRemovedObservation);//Serialize it.
            var expectedRemovedRawText = JsonDocument.Parse(expectedRemovedJson).RootElement.GetRawText();//Parse 

            Assert.Contains(resourceChanges.Changes, f =>
                f.ResourceType == "Observation" &&
                f.ChangeType == ChangeType.Removed &&
                f.FieldChanges[0].FieldPath == "Observation" &&
                f.FieldChanges[0].OldValue.Value.GetRawText() == expectedRemovedRawText &&
                f.FieldChanges[0].NewValue == null
            );

            //Assert - Matched
            //Build
            var expectedOldName = new HumanName { Family = "Matched", Given = new List<string> { "Sam" } };
            var expectedNewName = new HumanName { Family = "Matched", Given = new List<string> { "Samuel" } };
            //Serialize
            var expectedOldNameJson = new FhirJsonSerializer().SerializeToString(expectedOldName);
            var expectedNewNameJson = new FhirJsonSerializer().SerializeToString(expectedNewName);
            //Parse
            var expectedOldNameRawText = JsonDocument.Parse(expectedOldNameJson).RootElement.GetRawText();
            var expectedNewNameRawText = JsonDocument.Parse(expectedNewNameJson).RootElement.GetRawText();
           
            // Find the matched Patient's ResourceChange once, then check its two FieldDiffs separately
            var patientChange = resourceChanges.Changes.Single(f => f.ResourceType == "Patient" && f.ChangeType == ChangeType.Modified);
            
            //Assert
            Assert.Contains(patientChange.FieldChanges, fd =>
                fd.FieldPath == "name" &&
                fd.OldValue.Value.GetRawText() == expectedOldNameRawText &&
                fd.NewValue == null
            );

            Assert.Contains(patientChange.FieldChanges, fd =>
                fd.FieldPath == "name" &&
                fd.OldValue == null &&
                fd.NewValue.Value.GetRawText() == expectedNewNameRawText
            );

            //Assert - NoId
            Assert.Contains(resourceChanges.Changes, f =>
                f.ResourceType == "Observation" &&
                f.ChangeType == ChangeType.NoId &&
                f.FieldChanges.Count == 0
            );
        }
    }
}
