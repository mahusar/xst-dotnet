using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xst.Rpc;
using Xst.Rpc.Models;
using Xunit;
using Xunit.Abstractions;

namespace Xst.Rpc.IntegrationTests
{
    public class NodeIntegrationTests
    {
        private readonly ITestOutputHelper _output;

        public NodeIntegrationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [DaemonFact]
        public async Task Connects_and_reports_a_synced_chain()
        {
            using var client = DaemonSettings.CreateClient();

            var info = await client.GetInfoAsync();

            Assert.False(string.IsNullOrWhiteSpace(info.Version));
            Assert.True(info.Blocks > 0, "Daemon reports height " + info.Blocks + ".");

            _output.WriteLine("version   " + info.Version);
            _output.WriteLine("height    " + info.Blocks);
            _output.WriteLine("testnet   " + info.Testnet);
            _output.WriteLine("peers     " + info.Connections);
            _output.WriteLine("balance   " + XstAmount.Format(info.Balance));
        }

        [DaemonFact]
        public async Task Getinfo_has_no_fields_the_model_misses()
        {
            using var client = DaemonSettings.CreateClient();

            var raw = await client.InvokeAsync("getinfo");
            SchemaGuard.AssertFullyMapped<XstInfo>(raw, "getinfo");
        }

        [DaemonFact]
        public async Task Block_round_trips_from_hash_and_height()
        {
            using var client = DaemonSettings.CreateClient();

            var height = await client.GetBlockCountAsync();
            var hash = await client.GetBlockHashAsync(height);

            var byHash = await client.GetBlockAsync(hash);
            var byNumber = await client.GetBlockByNumberAsync(height);

            Assert.Equal(hash, byHash.Hash);
            Assert.Equal(byHash.Hash, byNumber.Hash);
            Assert.Equal(height, byHash.Height);

            _output.WriteLine("block     " + byHash.Height + " " + byHash.Hash);
            _output.WriteLine("staker    " + byHash.StakerAlias + " (" + byHash.StakerId + ")");
            _output.WriteLine("supply    " + (byHash.MoneySupply.HasValue
                ? XstAmount.Format(byHash.MoneySupply.Value) : "n/a"));
        }

        [DaemonFact]
        public async Task Block_has_no_fields_the_model_misses()
        {
            using var client = DaemonSettings.CreateClient();

            var hash = await client.GetBestBlockHashAsync();
            var raw = await client.InvokeAsync("getblock", hash, false);

            SchemaGuard.AssertFullyMapped<XstBlock>(raw, "getblock");
        }

        [DaemonFact]
        public async Task Money_supply_keeps_full_precision_through_the_client()
        {
            using var client = DaemonSettings.CreateClient();

            var raw = await client.InvokeAsync("getinfo");
            var rawSupply = raw["moneysupply"];
            var typed = (await client.GetInfoAsync()).MoneySupply;

            Assert.Equal(JTokenType.Float, rawSupply.Type);
            Assert.Equal(rawSupply.Value<decimal>(), typed);

            _output.WriteLine("supply    " + XstAmount.Format(typed));
            _output.WriteLine("units     " + XstAmount.ToUnits(typed));
        }

        [DaemonFact]
        public async Task Validateaddress_agrees_with_a_freshly_generated_address()
        {
            using var client = DaemonSettings.CreateClient();

            var address = await client.GetNewAddressAsync();
            Assert.False(string.IsNullOrWhiteSpace(address));

            var result = await client.ValidateAddressAsync(address);

            Assert.True(result.IsValid);
            Assert.True(result.IsMine);
            Assert.Equal(address, result.Address);

            var raw = await client.InvokeAsync("validateaddress", address);
            SchemaGuard.AssertFullyMapped<XstAddressValidation>(raw, "validateaddress");

            _output.WriteLine("address   " + address);
        }

        [DaemonFact]
        public async Task Validateaddress_rejects_nonsense()
        {
            using var client = DaemonSettings.CreateClient();

            var result = await client.ValidateAddressAsync("NotAnXstAddressAtAll");

            Assert.False(result.IsValid);
        }

        [DaemonFact]
        public async Task Peers_map_cleanly()
        {
            using var client = DaemonSettings.CreateClient();

            var peers = await client.GetPeerInfoAsync();
            var raw = await client.InvokeAsync("getpeerinfo");

            SchemaGuard.AssertFullyMapped<System.Collections.Generic.List<XstPeer>>(raw, "getpeerinfo");

            _output.WriteLine("peers     " + peers.Count);
            foreach (var peer in peers.Take(3))
            {
                _output.WriteLine("  " + peer.Address + "  " + peer.SubVersion);
            }
        }

        [DaemonFact]
        public async Task Wallet_listings_map_cleanly_even_when_empty()
        {
            using var client = DaemonSettings.CreateClient();

            var transactions = await client.InvokeAsync("listtransactions", "*", 10, 0);
            SchemaGuard.AssertFullyMapped<System.Collections.Generic.List<XstTransactionSummary>>(
                transactions, "listtransactions");

            var received = await client.InvokeAsync("listreceivedbyaddress", 1, true);
            SchemaGuard.AssertFullyMapped<System.Collections.Generic.List<XstReceivedByAddress>>(
                received, "listreceivedbyaddress");

            var unspent = await client.InvokeAsync("listunspent", 1, 9999999);
            SchemaGuard.AssertFullyMapped<System.Collections.Generic.List<XstUnspentOutput>>(
                unspent, "listunspent");

            _output.WriteLine("txs       " + (await client.ListTransactionsAsync()).Count);
            _output.WriteLine("unspent   " + (await client.ListUnspentAsync()).Count);
        }

        [DaemonFact]
        public async Task Gettransaction_maps_cleanly_when_the_wallet_has_history()
        {
            using var client = DaemonSettings.CreateClient();

            var recent = await client.ListTransactionsAsync("*", 1);
            if (recent.Count == 0)
            {
                _output.WriteLine("wallet has no transactions, nothing to check");
                return;
            }

            var raw = await client.InvokeAsync("gettransaction", recent[0].TxId);
            SchemaGuard.AssertFullyMapped<XstTransaction>(raw, "gettransaction");

            var tx = await client.GetTransactionAsync(recent[0].TxId);
            Assert.Equal(recent[0].TxId, tx.TxId);

            _output.WriteLine("txid      " + tx.TxId);
            _output.WriteLine("amount    " + XstAmount.Format(tx.Amount));
        }

        [DaemonFact]
        public async Task Staker_price_is_positive_and_exact()
        {
            using var client = DaemonSettings.CreateClient();

            var price = await client.GetStakerPriceAsync();
            Assert.True(price > 0m);

            _output.WriteLine("staker price " + XstAmount.Format(price));
        }

        [DaemonFact]
        public async Task Bad_credentials_are_reported_as_an_authentication_failure()
        {
            using var client = new XstClient(new XstClientOptions
            {
                Host = DaemonSettings.Host,
                Port = DaemonSettings.Port,
                Username = "definitely-not-the-user",
                Password = "definitely-not-the-password",
                Timeout = TimeSpan.FromSeconds(10)
            });

            await Assert.ThrowsAsync<XstAuthenticationException>(
                () => client.GetBlockCountAsync());
        }

        [DaemonFact]
        public async Task An_unknown_method_surfaces_the_daemon_error()
        {
            using var client = DaemonSettings.CreateClient();

            var ex = await Assert.ThrowsAsync<XstRpcException>(
                () => client.InvokeAsync("thismethoddoesnotexist"));

            _output.WriteLine("code      " + ex.Code);
            _output.WriteLine("message   " + ex.Message);
        }
    }
}
