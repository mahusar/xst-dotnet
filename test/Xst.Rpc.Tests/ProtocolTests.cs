using System;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xst.Rpc;
using Xunit;

namespace Xst.Rpc.Tests
{
    public class ProtocolTests
    {
        [Fact]
        public async Task Sends_a_json_rpc_envelope_the_daemon_understands()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.RespondWithResult("42");
            await client.GetBlockCountAsync();

            var request = daemon.LastRequest;
            Assert.Equal("1.0", request["jsonrpc"].Value<string>());
            Assert.Equal("getblockcount", request["method"].Value<string>());
            Assert.Equal(JTokenType.Array, request["params"].Type);
            Assert.NotNull(request["id"]);
        }

        [Fact]
        public async Task Sends_http_basic_authentication()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client("alice", "hunter2");

            daemon.RespondWithResult("0");
            await client.GetBlockCountAsync();

            var expected = "Basic " + Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes("alice:hunter2"));

            Assert.Equal(expected, daemon.LastAuthorizationHeader);
        }

        [Fact]
        public async Task Trims_trailing_nulls_because_the_daemon_branches_on_param_count()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.RespondWithResult("\"deadbeef\"");
            await client.SendToAddressAsync("Saddress", 1.5m);

            var parameters = (JArray)daemon.LastRequest["params"];

            Assert.Equal(2, parameters.Count);
            Assert.Equal("Saddress", parameters[0].Value<string>());
            Assert.Equal(1.5m, parameters[1].Value<decimal>());
        }

        [Fact]
        public async Task Feeless_flag_reaches_the_daemon_as_the_fifth_parameter()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.RespondWithResult("\"deadbeef\"");
            await client.SendToAddressAsync("Saddress", 1m, feeless: true);

            var parameters = (JArray)daemon.LastRequest["params"];

            Assert.Equal(5, parameters.Count);
            Assert.True(parameters[4].Value<bool>());
        }

        [Fact]
        public async Task Hex_data_reaches_the_daemon_as_the_sixth_parameter()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.RespondWithResult("\"deadbeef\"");
            await client.SendToAddressAsync(
                "Saddress", 1m, feeless: true, hexData: new[] { "aabbcc", "ddeeff" });

            var parameters = (JArray)daemon.LastRequest["params"];

            Assert.Equal(6, parameters.Count);
            Assert.True(parameters[4].Value<bool>());

            var data = (JArray)parameters[5];
            Assert.Equal(2, data.Count);
            Assert.Equal("aabbcc", data[0].Value<string>());
            Assert.Equal("ddeeff", data[1].Value<string>());
        }

        [Fact]
        public async Task Amount_is_rounded_to_six_decimals_before_it_leaves()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.RespondWithResult("\"deadbeef\"");
            await client.SendToAddressAsync("Saddress", 1.23456789m);

            var parameters = (JArray)daemon.LastRequest["params"];
            Assert.Equal(1.234568m, parameters[1].Value<decimal>());
        }

        [Fact]
        public async Task Rejects_an_amount_the_daemon_would_refuse()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => client.SendToAddressAsync("Saddress", 0m));

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => client.SendToAddressAsync("Saddress", -5m));
        }
    }

    public class ErrorTests
    {
        [Fact]
        public async Task Surfaces_the_daemon_error_code_and_message()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.RespondWithError(-5, "Invalid XST address");

            var ex = await Assert.ThrowsAsync<XstRpcException>(
                () => client.GetBalanceAsync());

            Assert.Equal(-5, ex.Code);
            Assert.Equal("getbalance", ex.Method);
            Assert.Contains("Invalid XST address", ex.Message);
        }

        [Fact]
        public async Task Bad_credentials_raise_a_named_exception()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.ResponseStatus = HttpStatusCode.Unauthorized;
            daemon.ResponseBody = string.Empty;

            var ex = await Assert.ThrowsAsync<XstAuthenticationException>(
                () => client.GetBalanceAsync());

            Assert.Contains("rpcuser", ex.Message);
        }

        [Fact]
        public async Task A_non_json_reply_does_not_surface_as_a_parser_crash()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.ResponseStatus = HttpStatusCode.OK;
            daemon.ResponseBody = "<html>not the daemon</html>";

            var ex = await Assert.ThrowsAsync<XstRpcException>(
                () => client.GetBalanceAsync());

            Assert.Contains("not JSON", ex.Message);
        }

        [Fact]
        public async Task An_unreachable_daemon_says_so_plainly()
        {
            var options = new XstClientOptions
            {
                Host = "127.0.0.1",
                Port = 1,
                Username = "u",
                Password = "p",
                Timeout = TimeSpan.FromSeconds(5)
            };

            using var client = new XstClient(options);

            var ex = await Assert.ThrowsAsync<XstRpcException>(
                () => client.GetBlockCountAsync());

            Assert.Contains("Could not reach", ex.Message);
        }
    }

    public class MappingTests
    {
        [Fact]
        public async Task Maps_getinfo_onto_the_model()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.RespondWithResult(@"{
                ""version"": ""v3.3.5.0"",
                ""protocolversion"": 63000,
                ""walletversion"": 60000,
                ""balance"": 1234.567891,
                ""blocks"": 3456789,
                ""moneysupply"": 43299999.999999,
                ""connections"": 8,
                ""difficulty"": 1.5,
                ""testnet"": false,
                ""keypoolsize"": 101,
                ""paytxfee"": 0.01,
                ""errors"": """"
            }");

            var info = await client.GetInfoAsync();

            Assert.Equal("v3.3.5.0", info.Version);
            Assert.Equal(1234.567891m, info.Balance);
            Assert.Equal(43299999.999999m, info.MoneySupply);
            Assert.Equal(3456789, info.Blocks);
            Assert.False(info.Testnet);
            Assert.Null(info.UnlockedUntil);
        }

        [Fact]
        public async Task Maps_validateaddress_onto_the_model()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.RespondWithResult(
                @"{""isvalid"":true,""address"":""Sabc"",""ismine"":true,""account"":""main""}");

            var result = await client.ValidateAddressAsync("Sabc");

            Assert.True(result.IsValid);
            Assert.Equal("Sabc", result.Address);
            Assert.True(result.IsMine);
            Assert.Equal("main", result.Account);
        }

        [Fact]
        public async Task Maps_listreceivedbyaddress_onto_the_model()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.RespondWithResult(@"[
                {""address"":""Sone"",""account"":""a"",""amount"":1.5,""confirmations"":10},
                {""address"":""Stwo"",""account"":"""",""amount"":0.000001,""confirmations"":0}
            ]");

            var rows = await client.ListReceivedByAddressAsync();

            Assert.Equal(2, rows.Count);
            Assert.Equal(1.5m, rows[0].Amount);
            Assert.Equal(0.000001m, rows[1].Amount);
            Assert.Equal(0, rows[1].Confirmations);
        }

        [Fact]
        public async Task A_null_result_does_not_throw()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.RespondWithResult("null");

            Assert.Null(await client.GetNewAddressAsync());
        }
    }
}
