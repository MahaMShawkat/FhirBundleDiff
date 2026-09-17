using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using System.Text.Json;

namespace FhirDiff.Core.Tests.CoreTestHelpers
{
    public class BundlesFetcher
    {
        private static readonly JsonSerializerOptions _fhirJsonOptions = new JsonSerializerOptions().ForFhir(ModelInfo.ModelInspector);

        public static Bundle GetBundleFromFile(string fileName)
        {
            string filePath = Path.Combine(AppContext.BaseDirectory, "TestData", fileName);
            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<Bundle>(json, _fhirJsonOptions);
        }
    }
}
