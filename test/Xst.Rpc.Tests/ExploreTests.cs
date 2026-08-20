using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xst.Rpc;
using Xst.Rpc.Models;
using Xunit;

namespace Xst.Rpc.Tests
{
    public class ExploreMappingTests
    {
        [Fact]
        public async Task Maps_getaddressinfo_including_the_hyphenated_field()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.RespondWithResult(@"{
                ""address"": ""Sabc"",
                ""balance"": 1234.567891,
                ""rank"": 42,
                ""transactions"": 17,
                ""outputs"": 20,
                ""received"": 5000.0,
                ""inputs"": 3,
                ""sent"": 3765.432109,
                ""unspent"": 17,
                ""in-outs"": 23,
                ""blocks"": 3456789
            }");

            var info = await client.GetAddressInfoAsync("Sabc");

            Assert.Equal("Sabc", info.Address);
            Assert.Equal(1234.567891m, info.Balance);
            Assert.Equal(42, info.Rank);
            Assert.Equal(23, info.InOuts);
            Assert.Equal(3765.432109m, info.Sent);
        }

        [Fact]
        public async Task Maps_a_paged_reply()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.RespondWithResult(@"{
                ""total"": 250,
                ""page"": 2,
                ""per_page"": 100,
                ""last_page"": 3,
                ""data"": [
                    {
                        ""txid"": ""aa"",
                        ""height"": 3456700,
                        ""vtx"": 1,
                        ""vout"": 0,
                        ""address"": ""Sabc"",
                        ""amount"": 10.5,
                        ""balance"": 10.5,
                        ""blockhash"": ""00bb"",
                        ""confirmations"": 89,
                        ""blocktime"": 1755000000,
                        ""isspent"": ""false""
                    }
                ]
            }");

            var page = await client.GetAddressInOutsPageAsync("Sabc", 2, 100);

            Assert.Equal(250, page.Total);
            Assert.Equal(2, page.Page);
            Assert.Equal(3, page.LastPage);
            Assert.True(page.HasMore);

            var row = Assert.Single(page.Data);
            Assert.Equal("aa", row.TxId);
            Assert.Equal(10.5m, row.Balance);
            Assert.Equal(3456700, row.Height);
            Assert.True(row.IsOutput);
            Assert.False(row.IsInput);
            Assert.False(row.IsSpent);
            Assert.Null(row.NextTxId);
        }

        [Fact]
        public async Task Maps_the_nested_shape_the_txs_endpoints_return()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.RespondWithResult(@"{
                ""total"": 2,
                ""page"": 1,
                ""per_page"": 2,
                ""last_page"": 1,
                ""data"": [
                    {
                        ""txid"": ""aa"",
                        ""balance"": 0.1,
                        ""address_inputs"": [],
                        ""address_outputs"": [
                            {""vout"":0,""amount"":0.1,""isspent"":""true"",
                             ""next_txid"":""bb"",""next_vin"":0}
                        ],
                        ""txinfo"": {
                            ""blockhash"": ""00cc"",
                            ""blocktime"": 1785972102,
                            ""height"": 35369374,
                            ""confirmations"": 241594,
                            ""vtx"": 0,
                            ""sources"": [
                                {""addresses"":[""Sxyz""],""reqSigs"":1,
                                 ""amount"":0.1,""type"":""pubkeyhash""}
                            ],
                            ""destinations"": [
                                {""addresses"":[""Sabc""],""reqSigs"":1,
                                 ""amount"":0.1,""type"":""pubkeyhash""},
                                {""amount"":0.0,""type"":""feework""}
                            ],
                            ""txflags"": [""feework""]
                        }
                    }
                ]
            }");

            var page = await client.GetAddressTxsPageAsync("Sabc", 1, 2);

            var row = Assert.Single(page.Data);
            Assert.Equal("aa", row.TxId);
            Assert.Empty(row.AddressInputs);
            Assert.Equal(35369374, row.TxInfo.Height);

            Assert.Contains("feework", row.TxInfo.TxFlags);
            Assert.Null(row.TxInfo.Destinations[1].Addresses);

            var output = Assert.Single(row.AddressOutputs);
            Assert.True(output.IsSpent);
            Assert.Equal("bb", output.NextTxId);
            Assert.Equal(0, output.NextVin);
        }

        [Fact]
        public async Task HasMore_is_false_on_the_last_page()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.RespondWithResult(
                @"{""total"":10,""page"":1,""per_page"":100,""last_page"":1,""data"":[]}");

            var page = await client.GetAddressTxsPageAsync("Sabc");

            Assert.False(page.HasMore);
            Assert.Empty(page.Data);
        }

        [Fact]
        public async Task Maps_a_spent_output_with_its_successor()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.RespondWithResult(@"[
                {
                    ""txid"": ""aa"",
                    ""height"": 35369374,
                    ""vtx"": 0,
                    ""vout"": 1,
                    ""address"": ""Sabc"",
                    ""amount"": 2.5,
                    ""balance"": 2.5,
                    ""blockhash"": ""00cc"",
                    ""confirmations"": 12,
                    ""blocktime"": 1785972102,
                    ""isspent"": ""true"",
                    ""next_txid"": ""bb"",
                    ""next_in"": 0
                }
            ]");

            var output = Assert.Single(await client.GetAddressOutputsAsync("Sabc"));

            Assert.True(output.IsOutput);
            Assert.Equal(1, output.Vout);

            Assert.True(output.IsSpent);
            Assert.Equal("bb", output.NextTxId);
            Assert.Equal(0, output.NextIn);

            Assert.Null(output.Vin);
            Assert.Null(output.PrevTxId);
        }

        [Fact]
        public async Task Maps_an_input_with_its_predecessor()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.RespondWithResult(@"[
                {
                    ""txid"": ""cc"",
                    ""height"": 35369376,
                    ""vtx"": 1,
                    ""vin"": 0,
                    ""address"": ""Sabc"",
                    ""amount"": 2.5,
                    ""balance"": 1.0,
                    ""blockhash"": ""00dd"",
                    ""confirmations"": 10,
                    ""blocktime"": 1785972112,
                    ""prev_txid"": ""bb"",
                    ""prev_vout"": 1
                }
            ]");

            var input = Assert.Single(await client.GetAddressInputsAsync("Sabc"));

            Assert.True(input.IsInput);
            Assert.Equal(0, input.Vin);
            Assert.Equal("bb", input.PrevTxId);
            Assert.Equal(1, input.PrevVout);
            Assert.Equal(2.5m, input.Amount);
            Assert.Equal(1.0m, input.Balance);

            Assert.Null(input.Vout);
            Assert.False(input.IsOutput);
        }

        [Fact]
        public async Task Maps_gethdaddresses_into_external_and_change()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.RespondWithResult(@"{
                ""external"": [{""child"":0,""pubkey"":""02aa"",""address"":""Sone"",""inouts"":4}],
                ""change"":   [{""child"":0,""pubkey"":""02bb"",""address"":""Stwo"",""inouts"":0}]
            }");

            var addresses = await client.GetHdAddressesAsync("xpubdeadbeef");

            Assert.Equal("Sone", Assert.Single(addresses.External).Address);
            Assert.Equal(4, addresses.External[0].InOuts);
            Assert.Equal("Stwo", Assert.Single(addresses.Change).Address);
        }
    }

    public class ExploreProtocolTests
    {
        [Fact]
        public async Task Paged_calls_send_page_perpage_and_ordering()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.RespondWithResult(
                @"{""total"":0,""page"":1,""per_page"":20,""last_page"":1,""data"":[]}");

            await client.GetAddressTxsPageAsync("Sabc", 3, 20, forward: false);

            var parameters = (JArray)daemon.LastRequest["params"];

            Assert.Equal("getaddresstxspg", daemon.LastRequest["method"].Value<string>());
            Assert.Equal("Sabc", parameters[0].Value<string>());
            Assert.Equal(3, parameters[1].Value<int>());
            Assert.Equal(20, parameters[2].Value<int>());
            Assert.False(parameters[3].Value<bool>());
        }

        [Fact]
        public async Task Range_calls_are_one_based()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => client.GetAddressInOutsAsync("Sabc", start: 0));

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => client.GetAddressInOutsAsync("Sabc", start: 1, max: 0));
        }

        [Fact]
        public async Task Page_numbers_are_one_based()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => client.GetAddressTxsPageAsync("Sabc", page: 0));

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => client.GetAddressTxsPageAsync("Sabc", page: 1, perPage: 0));
        }

        [Fact]
        public async Task Richlistsize_omits_the_threshold_when_not_given()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.RespondWithResult("1234");
            await client.GetRichListSizeAsync();

            Assert.Empty((JArray)daemon.LastRequest["params"]);
        }

        [Fact]
        public async Task Richlistsize_rounds_the_threshold()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.RespondWithResult("12");
            await client.GetRichListSizeAsync(1.23456789m);

            Assert.Equal(1.234568m, ((JArray)daemon.LastRequest["params"])[0].Value<decimal>());
        }

        [Fact]
        public async Task Getchildkey_omits_the_network_byte_when_not_given()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.RespondWithResult(
                @"{""extended"":""xpub"",""pubkey"":""02aa"",""address"":""Sone""}");

            await client.GetChildKeyAsync("xpubdeadbeef", 5);

            Assert.Equal(2, ((JArray)daemon.LastRequest["params"]).Count);
        }

        [Fact]
        public async Task Getchildkey_rejects_a_negative_child_index()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => client.GetChildKeyAsync("xpubdeadbeef", -1));
        }

        [Fact]
        public async Task A_daemon_without_the_explore_api_surfaces_as_an_rpc_error()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.RespondWithError(-1, "** ERROR: Explore API only **");

            var ex = await Assert.ThrowsAsync<XstRpcException>(
                () => client.GetAddressInfoAsync("Sabc"));

            Assert.Contains("Explore API only", ex.Message);
        }
    }
}
