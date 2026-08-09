using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;

namespace FhirDiff.Core.Tests
{
    public class BundleLoadingTests
    {
        private const string fileName = "Aaron697_Brekke496_2fa15bc7-8866-461a-9000-f739e425860a.json";

        [Fact]
        public void LoadBundle_ParsesSuccessfully()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "TestData", fileName);
            var parser = new FhirJsonParser();
            var json = File.ReadAllText(path);

            var bundle = parser.Parse<Bundle>(json);

            Assert.NotNull(bundle);
            Assert.True(bundle.Entry.Count > 0);
        }
    }
}
