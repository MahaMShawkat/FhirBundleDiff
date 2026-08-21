using FhirDiff.Core.Models;
using FhirDiff.Core.Services;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace FhirDiff.Core.Tests
{
    public class FindAddedFieldsChangesTests
    {
        private const string NestedArrayJson = "{ \"extra\": [ { \"a\": 5 } ] }";
        private const string NestedObjectJson = "{ \"extra\": { \"b\": { \"c\": 5 } } }";

        [Fact]
        public void FindAddedFieldsChanges_NestedObjectAndArray_DecomposesToLeafScalar()
        {
            //Arrange
            var differ = new BundleDiffer();
            var listFieldDiff = new List<FieldDiff>();
            var arrayValue = JsonDocument.Parse(NestedArrayJson).RootElement.GetProperty("extra");
            var objectValue = JsonDocument.Parse(NestedObjectJson).RootElement.GetProperty("extra");

            //Act
            differ.FindAddedFieldsChanges(listFieldDiff, arrayValue, "extra");
            differ.FindAddedFieldsChanges(listFieldDiff, objectValue, "extra");

            //Assert
            Assert.Contains(listFieldDiff, f => f.FieldPath == "extra.a" && f.OldValue == null && f.NewValue.Value.GetInt32() == 5);
            Assert.Contains(listFieldDiff, f => f.FieldPath == "extra.b.c" && f.OldValue == null && f.NewValue.Value.GetInt32() == 5);
        }
    }
}
