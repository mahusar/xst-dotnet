using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xst.Rpc;
using Xst.Rpc.Models;
using Xunit;

namespace Xst.Rpc.Tests
{
    public class WalletMappingTests
    {
        [Fact]
        public async Task Maps_gettransaction_including_details()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.RespondWithResult(@"{
                ""amount"": -1.5,
                ""fee"": -0.01,
                ""confirmations"": 12,
                ""blockhash"": ""00aa"",
                ""blockindex"": 3,
                ""blocktime"": 1755000000,
                ""txid"": ""ff01"",
                ""time"": 1754999000,
                ""timereceived"": 1754999001,
                ""details"": [
                    {""account"":""main"",""address"":""Sdest"",""category"":""send"",""amount"":-1.5,""fee"":-0.01}
                ]
            }");

            var tx = await client.GetTransactionAsync("ff01");

            Assert.Equal("ff01", tx.TxId);
            Assert.Equal(-1.5m, tx.Amount);
            Assert.Equal(-0.01m, tx.Fee);
            Assert.Equal(12, tx.Confirmations);
            Assert.Single(tx.Details);
            Assert.Equal(XstTransactionCategory.Send, tx.Details[0].Category);
            Assert.Equal("Sdest", tx.Details[0].Address);
        }

        [Fact]
        public async Task Maps_listtransactions_rows()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.RespondWithResult(@"[
                {""account"":"""",""address"":""Sone"",""category"":""receive"",
                 ""amount"":2.000001,""confirmations"":5,""txid"":""aa"",""time"":1755000000},
                {""account"":""main"",""category"":""move"",""amount"":-1.0,
                 ""otheraccount"":""savings"",""time"":1755000001,""comment"":""shuffle""}
            ]");

            var rows = await client.ListTransactionsAsync();

            Assert.Equal(2, rows.Count);
            Assert.Equal(2.000001m, rows[0].Amount);
            Assert.Equal(XstTransactionCategory.Receive, rows[0].Category);
            Assert.Equal(XstTransactionCategory.Move, rows[1].Category);
            Assert.Equal("savings", rows[1].OtherAccount);
        }

        [Fact]
        public async Task Maps_listunspent_rows()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.RespondWithResult(@"[
                {""txid"":""aa"",""vout"":0,""scriptPubKey"":""76a914"",
                 ""amount"":0.000001,""confirmations"":100,""spendable"":true}
            ]");

            var rows = await client.ListUnspentAsync();

            Assert.Single(rows);
            Assert.Equal("aa", rows[0].TxId);
            Assert.Equal(0, rows[0].Vout);
            Assert.Equal(0.000001m, rows[0].Amount);
            Assert.True(rows[0].Spendable);
        }

        [Fact]
        public async Task Maps_listaccounts_into_a_dictionary()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.RespondWithResult(@"{"""":10.5,""main"":0.000001}");

            var accounts = await client.ListAccountsAsync();

            Assert.Equal(2, accounts.Count);
            Assert.Equal(10.5m, accounts[""]);
            Assert.Equal(0.000001m, accounts["main"]);
        }
    }

    public class WalletProtocolTests
    {
        [Fact]
        public async Task Walletpassphrase_sends_seconds_and_the_mintonly_flag()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.RespondWithResult("null");
            await client.WalletPassphraseAsync("secret", TimeSpan.FromMinutes(5), mintOnly: true);

            var parameters = (JArray)daemon.LastRequest["params"];

            Assert.Equal("walletpassphrase", daemon.LastRequest["method"].Value<string>());
            Assert.Equal("secret", parameters[0].Value<string>());
            Assert.Equal(300L, parameters[1].Value<long>());
            Assert.True(parameters[2].Value<bool>());
        }

        [Fact]
        public async Task Walletpassphrase_rejects_a_non_positive_timeout()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => client.WalletPassphraseAsync("secret", TimeSpan.Zero));
        }

        [Fact]
        public async Task Walletlock_takes_no_parameters()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.RespondWithResult("null");
            await client.WalletLockAsync();

            Assert.Empty((JArray)daemon.LastRequest["params"]);
        }

        [Fact]
        public async Task Sendmany_rounds_every_recipient()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.RespondWithResult("\"txid\"");
            await client.SendManyAsync("main", new Dictionary<string, decimal>
            {
                ["Sone"] = 1.23456789m,
                ["Stwo"] = 0.5m
            });

            var recipients = (JObject)((JArray)daemon.LastRequest["params"])[1];

            Assert.Equal(1.234568m, recipients["Sone"].Value<decimal>());
            Assert.Equal(0.5m, recipients["Stwo"].Value<decimal>());
        }

        [Fact]
        public async Task Sendmany_refuses_an_empty_recipient_set()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            await Assert.ThrowsAsync<ArgumentException>(
                () => client.SendManyAsync("main", new Dictionary<string, decimal>()));
        }

        [Fact]
        public async Task Listunspent_omits_the_address_filter_when_empty()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.RespondWithResult("[]");
            await client.ListUnspentAsync();

            Assert.Equal(2, ((JArray)daemon.LastRequest["params"]).Count);
        }

        [Fact]
        public async Task Listunspent_passes_the_address_filter_when_given()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.RespondWithResult("[]");
            await client.ListUnspentAsync(1, 9999999, new[] { "Sone", "Stwo" });

            var parameters = (JArray)daemon.LastRequest["params"];
            Assert.Equal(3, parameters.Count);
            Assert.Equal(2, ((JArray)parameters[2]).Count);
        }

        [Fact]
        public async Task Sendtostealthaddress_passes_narration()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.RespondWithResult("\"txid\"");
            await client.SendToStealthAddressAsync("Sxstealth", 2m, narration: "for the deck");

            var parameters = (JArray)daemon.LastRequest["params"];
            Assert.Equal("Sxstealth", parameters[0].Value<string>());
            Assert.Equal(2m, parameters[1].Value<decimal>());
            Assert.Equal("for the deck", parameters[2].Value<string>());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Address_guards_reject_blank_input(string address)
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            await Assert.ThrowsAsync<ArgumentException>(
                () => client.ValidateAddressAsync(address));
            await Assert.ThrowsAsync<ArgumentException>(
                () => client.GetReceivedByAddressAsync(address));
        }
    }
}
