using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xst.Rpc.Models;
using Xunit;

namespace Xst.Rpc.IntegrationTests
{
    public class SchemaGuardTests
    {
        private sealed class Sample
        {
            [JsonProperty("known")] public string Known { get; set; }
            [JsonProperty("nested")] public Nested Nested { get; set; }
            [JsonProperty("rows")] public List<Nested> Rows { get; set; }
            [JsonProperty("opaque")] public JToken Opaque { get; set; }
            [JsonIgnore] public string Derived { get; set; }
        }

        private sealed class Nested
        {
            [JsonProperty("inner")] public int Inner { get; set; }
        }

        [Fact]
        public void Passes_when_every_field_is_mapped()
        {
            var raw = JObject.Parse(@"{""known"":""a"",""nested"":{""inner"":1}}");

            SchemaGuard.AssertFullyMapped<Sample>(raw);
        }

        [Fact]
        public void Fails_on_an_unmapped_top_level_field()
        {
            var raw = JObject.Parse(@"{""known"":""a"",""surprise"":42}");

            var ex = Assert.ThrowsAny<Xunit.Sdk.XunitException>(
                () => SchemaGuard.AssertFullyMapped<Sample>(raw));

            Assert.Contains("surprise", ex.Message);
        }

        [Fact]
        public void Fails_on_an_unmapped_nested_field()
        {
            var raw = JObject.Parse(@"{""nested"":{""inner"":1,""deep"":true}}");

            var ex = Assert.ThrowsAny<Xunit.Sdk.XunitException>(
                () => SchemaGuard.AssertFullyMapped<Sample>(raw));

            Assert.Contains("nested.deep", ex.Message);
        }

        [Fact]
        public void Fails_on_an_unmapped_field_inside_a_list()
        {
            var raw = JObject.Parse(@"{""rows"":[{""inner"":1},{""inner"":2,""extra"":""x""}]}");

            var ex = Assert.ThrowsAny<Xunit.Sdk.XunitException>(
                () => SchemaGuard.AssertFullyMapped<Sample>(raw));

            Assert.Contains("extra", ex.Message);
        }

        [Fact]
        public void Checks_a_top_level_array()
        {
            var raw = JArray.Parse(@"[{""inner"":1},{""inner"":2,""extra"":""x""}]");

            var ex = Assert.ThrowsAny<Xunit.Sdk.XunitException>(
                () => SchemaGuard.AssertFullyMapped<List<Nested>>(raw));

            Assert.Contains("extra", ex.Message);
        }

        [Fact]
        public void Does_not_descend_into_opaque_types()
        {
            var raw = JObject.Parse(@"{""opaque"":{""anything"":{""at"":""all""}}}");

            SchemaGuard.AssertFullyMapped<Sample>(raw);
        }

        [Fact]
        public void Does_not_descend_into_dictionaries()
        {
            var raw = JObject.Parse(@"{"""":10.5,""main"":0.000001,""anything"":1}");

            SchemaGuard.AssertFullyMapped<Dictionary<string, decimal>>(raw);
        }

        [Fact]
        public void Ignored_properties_do_not_count_as_mapped()
        {
            var raw = JObject.Parse(@"{""Derived"":""a""}");

            Assert.ThrowsAny<Xunit.Sdk.XunitException>(
                () => SchemaGuard.AssertFullyMapped<Sample>(raw));
        }

        [Fact]
        public void Handles_a_null_result_without_throwing()
        {
            SchemaGuard.AssertFullyMapped<Sample>(JValue.CreateNull());
            SchemaGuard.AssertFullyMapped<Sample>(null);
        }

        [Fact]
        public void Recognises_the_hyphenated_field_on_addressinfo()
        {
            var raw = JObject.Parse(@"{""address"":""S"",""balance"":1.0,""rank"":1,
                ""transactions"":1,""outputs"":1,""received"":1.0,""inputs"":0,
                ""sent"":0.0,""unspent"":1,""in-outs"":1,""blocks"":1}");

            SchemaGuard.AssertFullyMapped<XstAddressInfo>(raw);
        }
    }
}
