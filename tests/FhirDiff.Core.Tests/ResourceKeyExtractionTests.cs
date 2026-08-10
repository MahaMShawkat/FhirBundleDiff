using FhirDiff.Core.Models;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;

namespace FhirDiff.Core.Tests
{
    public class ResourceKeyExtractionTests
    {
        private const string fileNameWithNullID = "Aaron697_Stiedemann542_41166989-975d-4d17-b9de-17f94cb3eec1.json";
        private const string fileName = "Aaron697_Brekke496_2fa15bc7-8866-461a-9000-f739e425860a.json";

        [Fact]
        public void GetBundleResources_CorrectlyExtractResourceKey()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "TestData", fileName);
            var parser = new FhirJsonParser();
            var json = File.ReadAllText(path);

            var bundle = parser.Parse<Bundle>(json);
            var matcher = new BundlesMatcher();
            var keyDic = matcher.GetBundleResources(bundle);
            var firstEntry = bundle.Entry.First();
            var expectedKey = new ResourceKey(firstEntry.Resource.TypeName, firstEntry.Resource.Id);

            Assert.Equal(bundle.Entry.Count, keyDic.Count);
            Assert.True(keyDic.ContainsKey(expectedKey));
            Assert.Same(firstEntry.Resource, keyDic[expectedKey]);
        }

        [Fact]
        public void GetBundleResources_ThroughsOnNullId()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "TestData", fileNameWithNullID);
            var parser = new FhirJsonParser();
            var json = File.ReadAllText(path);

            var bundle = parser.Parse<Bundle>(json);
            var matcher = new BundlesMatcher();

            Assert.Throws<InvalidOperationException>(() => matcher.GetBundleResources(bundle));
        }
    }
}
