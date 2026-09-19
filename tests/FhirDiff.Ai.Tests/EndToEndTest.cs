using FhirDiff.Ai.Services;
using FhirDiff.Ai.Tests.AiTestHelpers;
using FhirDiff.Core.Models;
using FhirDiff.Core.Services;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Text.Json;
using Task = System.Threading.Tasks.Task;

namespace FhirDiff.Ai.Tests;

public class EndToEndTest
{
    private const string OldFileName = "Aaron697_Brekke496_2fa15bc7-8866-461a-9000-f739e425860a";
    private const string NewFileName = "Aaron697_Stiedemann542_41166989-975d-4d17-b9de-17f94cb3eec1";
    private static readonly JsonSerializerOptions _fhirJsonOptions = new JsonSerializerOptions().ForFhir(ModelInfo.ModelInspector);

    [Fact]
    public async Task Process_RealSyntheaBundles_CompletesWithoutError()
    {
        // Arrange — load real bundles
        string oldFilePath = Path.Combine(AppContext.BaseDirectory, "TestData", OldFileName + ".json");
        string oldJson = File.ReadAllText(oldFilePath);
        Bundle? oldBundle = JsonSerializer.Deserialize<Bundle>(oldJson, _fhirJsonOptions);

        string newFilePath = Path.Combine(AppContext.BaseDirectory, "TestData", NewFileName + ".json");
        string newJson = File.ReadAllText(newFilePath);
        Bundle? newBundle = JsonSerializer.Deserialize<Bundle>(newJson, _fhirJsonOptions);

        // Arrange — fake Gemini response, in case Match() finds any real overlap
        string innerExplanations = """
        [{"ResourceType":"Patient","Id":"placeholder","Explanation":"test"}]
        """;
        string geminiEnvelope = $$"""
        {
          "candidates": [
            { "content": { "parts": [ { "text": {{JsonSerializer.Serialize(innerExplanations)}} } ] } }
          ]
        }
        """;
        var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(geminiEnvelope)
        };
        var fakeHandler = new FakeHttpMessageHandler(fakeResponse);
        var fakeHttpClient = new HttpClient(fakeHandler);

        var explainer = new GeminiDiffExplainer("fake-api-key", fakeHttpClient);
        var matcher = new BundlesMatcher();
        var differ = new BundleDiffer();
        var processor = new BundleDiffProcessor(matcher, differ, explainer);

        // Act
        var resourceChanges = await processor.Process(oldBundle!, newBundle!);

        // Assert
        Assert.NotNull(resourceChanges);
    }


    [Trait("Category", "Integration")]
    [Fact]
    public async Task Process_RealApiCall_CompletesWithoutError()
    {
        // Arrange — load real bundles
        string oldFilePath = Path.Combine(AppContext.BaseDirectory, "TestData", OldFileName + ".json");
        string oldJson = File.ReadAllText(oldFilePath);
        Bundle? oldBundle = JsonSerializer.Deserialize<Bundle>(oldJson, _fhirJsonOptions);

        string newFilePath = Path.Combine(AppContext.BaseDirectory, "TestData", NewFileName + ".json");
        string newJson = File.ReadAllText(newFilePath);
        Bundle? newBundle = JsonSerializer.Deserialize<Bundle>(newJson, _fhirJsonOptions);

        //Arrange - Explainer with real apiKey
        var config = new ConfigurationBuilder().AddJsonFile("appsettings.Development.json", optional: false).Build();
        string apiKey = config["Gemini:ApiKey"] ?? throw new InvalidOperationException("Missing Gemini:ApiKey");
        GeminiDiffExplainer explainer = new GeminiDiffExplainer(apiKey);
        var matcher = new BundlesMatcher();
        var differ = new BundleDiffer();
        var processor = new BundleDiffProcessor(matcher, differ, explainer);

        // Act
        var resourceChanges = await processor.Process(oldBundle!, newBundle!);

        // Assert
        Assert.NotNull(resourceChanges);
    }

    [Trait("Category", "Integration")]
    [Fact]
    public async Task Process_RealApiCallOnManualFixture_CompletesWithoutError()
    {
        // Arrange — load real bundles
        string oldFilePath = Path.Combine(AppContext.BaseDirectory, "TestData", "process-test-old-bundle.json");
        string oldJson = File.ReadAllText(oldFilePath);
        Bundle? oldBundle = JsonSerializer.Deserialize<Bundle>(oldJson, _fhirJsonOptions);

        string newFilePath = Path.Combine(AppContext.BaseDirectory, "TestData", "process-test-new-bundle.json");
        string newJson = File.ReadAllText(newFilePath);
        Bundle? newBundle = JsonSerializer.Deserialize<Bundle>(newJson, _fhirJsonOptions);

        //Arrange - Explainer with real apiKey
        IConfigurationRoot config = new ConfigurationBuilder().AddJsonFile("appsettings.Development.json", optional: false).Build();
        string apiKey = config["Gemini:ApiKey"] ?? throw new InvalidOperationException("Missing Gemini:ApiKey");
        GeminiDiffExplainer explainer = new GeminiDiffExplainer(apiKey);
        BundlesMatcher matcher = new BundlesMatcher();
        BundleDiffer differ = new BundleDiffer();
        BundleDiffProcessor processor = new BundleDiffProcessor(matcher, differ, explainer);

        // Act
        BundleDiffResult resourceChanges = await processor.Process(oldBundle!, newBundle!);

        // Asserts
        //Assert - Aggregates Counters
        Assert.Equal(4, resourceChanges.Changes.Count);

        //Assert - Added
        //rebuild and reserialize instead of hand-writing JSON so the expected value matches exactly what FhirJsonSerializer actually produces — no guessing property order or formatting.
        Observation expectedAddedObservation = new Observation //Build Observation SDK object, same shape as the fixture entry
        {
            Id = "obs-added-1",
            Status = ObservationStatus.Final,
            Code = new CodeableConcept { Text = "Added-only observation" }
        };
        string expectedAddedJson = new FhirJsonSerializer().SerializeToString(expectedAddedObservation);//Serialize it.
        string expectedAddedRawText = JsonDocument.Parse(expectedAddedJson).RootElement.GetRawText();//Parse 

        Assert.Contains(resourceChanges.Changes, f =>
            f.ResourceType == "Observation" &&
            f.ChangeType == ChangeType.Added &&
            f.FieldChanges[0].FieldPath == "Observation" &&
            f.FieldChanges[0].OldValue == null &&
            f.FieldChanges[0].NewValue.Value.GetRawText() == expectedAddedRawText &&
            f.ChangeExplanation == "Resource is added"
        );

        //Assert - Removed
        Observation expectedRemovedObservation = new Observation //Build Observation SDK object, same shape as the fixture entry
        {
            Id = "obs-removed-1",
            Status = ObservationStatus.Final,
            Code = new CodeableConcept { Text = "Removed-only observation" }
        };
        string expectedRemovedJson = new FhirJsonSerializer().SerializeToString(expectedRemovedObservation);//Serialize it.
        string expectedRemovedRawText = JsonDocument.Parse(expectedRemovedJson).RootElement.GetRawText();//Parse 

        Assert.Contains(resourceChanges.Changes, f =>
            f.ResourceType == "Observation" &&
            f.ChangeType == ChangeType.Removed &&
            f.FieldChanges[0].FieldPath == "Observation" &&
            f.FieldChanges[0].OldValue.Value.GetRawText() == expectedRemovedRawText &&
            f.FieldChanges[0].NewValue == null &&
            f.ChangeExplanation == "Resource is removed"
        );

        //Assert - Matched
        //Build
        HumanName expectedOldName = new HumanName { Family = "Matched", Given = new List<string> { "Sam" } };
        HumanName expectedNewName = new HumanName { Family = "Matched", Given = new List<string> { "Samuel" } };
        //Serialize
        string expectedOldNameJson = new FhirJsonSerializer().SerializeToString(expectedOldName);
        string expectedNewNameJson = new FhirJsonSerializer().SerializeToString(expectedNewName);
        //Parse
        string expectedOldNameRawText = JsonDocument.Parse(expectedOldNameJson).RootElement.GetRawText();
        string expectedNewNameRawText = JsonDocument.Parse(expectedNewNameJson).RootElement.GetRawText();

        // Find the matched Patient's ResourceChange once, then check its two FieldDiffs separately
        ResourceChange patientChange = resourceChanges.Changes.Single(f => f.ResourceType == "Patient" && f.ChangeType == ChangeType.Modified);

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

        Assert.False(string.IsNullOrWhiteSpace(patientChange.ChangeExplanation));

        //Assert - NoId
        Assert.Contains(resourceChanges.Changes, f =>
            f.ResourceType == "Observation" &&
            f.ChangeType == ChangeType.NoId &&
            f.FieldChanges.Count == 0 &&
            f.ChangeExplanation == "Resource has no Id. Matching is inapplicable"
        );
    }
}