using FhirDiff.Ai.Services;
using FhirDiff.Ai.Tests.AiTestHelpers;
using FhirDiff.Core.Models;
using FhirDiff.Core.Services;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Text.Json;

namespace FhirDiff.Ai.Tests;

public class EndToEndTest
{
    private const string OldFileName = "Aaron697_Brekke496_2fa15bc7-8866-461a-9000-f739e425860a";
    private const string NewFileName = "Aaron697_Stiedemann542_41166989-975d-4d17-b9de-17f94cb3eec1";
    private static readonly JsonSerializerOptions _fhirJsonOptions = new JsonSerializerOptions().ForFhir(ModelInfo.ModelInspector);

    [Fact]
    public void Process_RealSyntheaBundles_CompletesWithoutError()
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
        var resourceChanges = processor.Process(oldBundle!, newBundle!);

        // Assert
        Assert.NotNull(resourceChanges);
    }


    [Trait("Category", "Integration")]
    [Fact]
    public void Process_RealApiCall_CompletesWithoutError()
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
        var resourceChanges = processor.Process(oldBundle!, newBundle!);

        // Assert
        Assert.NotNull(resourceChanges);
    }
    
}