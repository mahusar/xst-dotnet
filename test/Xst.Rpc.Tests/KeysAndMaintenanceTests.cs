using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xst.Rpc;
using Xst.Rpc.Models;
using Xunit;

namespace Xst.Rpc.Tests
{
    public class RawTransactionTests
    {
        [Fact]
        public async Task Createrawtransaction_sends_inputs_and_rounded_outputs()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.RespondWithResult("\"0100\"");

            await client.CreateRawTransactionAsync(
                new[] { new XstOutPoint("aa", 0), new XstOutPoint("bb", 2) },
                new Dictionary<string, decimal> { ["Sdest"] = 1.23456789m });

            var parameters = (JArray)daemon.LastRequest["params"];
            var inputs = (JArray)parameters[0];
            var outputs = (JObject)parameters[1];

            Assert.Equal(2, inputs.Count);
            Assert.Equal("aa", inputs[0]["txid"].Value<string>());
            Assert.Equal(2, inputs[1]["vout"].Value<int>());
            Assert.Equal(1.234568m, outputs["Sdest"].Value<decimal>());
        }

        [Fact]
        public async Task Createrawtransaction_requires_inputs_and_outputs()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            await Assert.ThrowsAsync<ArgumentException>(() =>
                client.CreateRawTransactionAsync(
                    new XstOutPoint[0],
                    new Dictionary<string, decimal> { ["Sdest"] = 1m }));

            await Assert.ThrowsAsync<ArgumentException>(() =>
                client.CreateRawTransactionAsync(
                    new[] { new XstOutPoint("aa", 0) },
                    new Dictionary<string, decimal>()));
        }

        [Fact]
        public async Task Maps_signrawtransaction()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.RespondWithResult(@"{""hex"":""0100aabb"",""complete"":true}");

            var signed = await client.SignRawTransactionAsync("0100");

            Assert.Equal("0100aabb", signed.Hex);
            Assert.True(signed.Complete);
        }

        [Fact]
        public async Task Signrawtransaction_passes_previous_outputs()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.RespondWithResult(@"{""hex"":""01"",""complete"":false}");

            await client.SignRawTransactionAsync("0100", new[]
            {
                new XstPreviousOutput { TxId = "aa", Vout = 1, ScriptPubKey = "76a914" }
            });

            var prevs = (JArray)((JArray)daemon.LastRequest["params"])[1];
            var first = (JObject)prevs[0];

            Assert.Equal("aa", first["txid"].Value<string>());
            Assert.Equal(1, first["vout"].Value<int>());
            Assert.Equal("76a914", first["scriptPubKey"].Value<string>());

            Assert.Null(first["redeemScript"]);
        }

        [Fact]
        public async Task Addmultisigaddress_rejects_fewer_keys_than_signatures()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            await Assert.ThrowsAsync<ArgumentException>(
                () => client.AddMultiSigAddressAsync(3, new[] { "02aa", "02bb" }));

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => client.AddMultiSigAddressAsync(0, new[] { "02aa" }));
        }

        [Fact]
        public async Task Createfeework_omits_blocksize_when_not_given()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.RespondWithResult("{}");
            await client.CreateFeeworkAsync("0100", 3456789);

            Assert.Equal(2, ((JArray)daemon.LastRequest["params"]).Count);
        }
    }

    public class MaintenanceTests
    {
        [Fact]
        public async Task Reservebalance_reads_when_called_with_no_arguments()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.RespondWithResult("{}");
            await client.ReserveBalanceAsync();

            Assert.Empty((JArray)daemon.LastRequest["params"]);
        }

        [Fact]
        public async Task Reservebalance_sends_the_amount_when_switching_on()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.RespondWithResult("{}");
            await client.ReserveBalanceAsync(true, 100.5m);

            var parameters = (JArray)daemon.LastRequest["params"];
            Assert.Equal(2, parameters.Count);
            Assert.True(parameters[0].Value<bool>());
            Assert.Equal(100.5m, parameters[1].Value<decimal>());
        }

        [Fact]
        public async Task Settxfee_rejects_a_negative_fee()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => client.SetTxFeeAsync(-1m));
        }

        [Fact]
        public async Task Window_statistics_reject_non_positive_arguments()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => client.GetTxVolumeAsync(0, 10, 1));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => client.GetXstVolumeAsync(10, 0, 1));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => client.GetBlockIntervalAsync(10, 10, 0));
        }

        [Fact]
        public async Task Getrecentqueue_requires_at_least_one_block()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => client.GetRecentQueueAsync(0));
        }

        [Fact]
        public async Task Listsinceblock_omits_parameters_when_no_hash_is_given()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.RespondWithResult("{}");
            await client.ListSinceBlockAsync();

            Assert.Empty((JArray)daemon.LastRequest["params"]);
        }
    }
}
