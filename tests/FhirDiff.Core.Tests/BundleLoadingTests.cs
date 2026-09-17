using FhirDiff.Core.Tests.CoreTestHelpers;
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
            Bundle bundle = BundlesFetcher.GetBundleFromFile(fileName);

            Assert.NotNull(bundle);
            Assert.True(bundle.Entry.Count > 0);
        }
    }
}
