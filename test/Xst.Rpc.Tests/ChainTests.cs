using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xst.Rpc;
using Xunit;

namespace Xst.Rpc.Tests
{
    public class ChainMappingTests
    {
        [Fact]
        public async Task Maps_a_qpos_block()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.RespondWithResult(@"{
                ""hash"": ""00aa"",
                ""confirmations"": 12,
                ""isinmainchain"": true,
                ""size"": 431,
                ""height"": 3456789,
                ""version"": 7,
                ""merkleroot"": ""bb"",
                ""staker_id"": 5,
                ""staker_alias"": ""dragon"",
                ""block_reward"": 1.5,
                ""moneysupply"": 43299999.999999,
                ""mint"": 0.000001,
                ""time"": 1755000000,
                ""nonce"": 0,
                ""bits"": ""1d00ffff"",
                ""difficulty"": 1.5,
                ""previousblockhash"": ""0099"",
                ""tx"": [""cc"", ""dd""]
            }");

            var block = await client.GetBlockAsync("00aa");

            Assert.Equal("00aa", block.Hash);
            Assert.Equal(3456789, block.Height);
            Assert.Equal(5, block.StakerId);
            Assert.Equal("dragon", block.StakerAlias);
            Assert.Equal(43299999.999999m, block.MoneySupply);
            Assert.Equal(0.000001m, block.Mint);
            Assert.Equal(2, block.Transactions.Count);
            Assert.Null(block.NextBlockHash);
        }

        [Fact]
        public async Task Maps_staker_authorities()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.RespondWithResult(@"{
                ""owner"":      {""address"":""Sowner"",""pubkey"":""02aa""},
                ""manager"":    {""address"":""Smanager"",""pubkey"":""02bb""},
                ""delegate"":   {""address"":""Sdelegate"",""pubkey"":""02cc""},
                ""controller"": {""address"":""Scontroller"",""pubkey"":""02dd""}
            }");

            var auth = await client.GetStakerAuthoritiesAsync("dragon");

            Assert.Equal("Sowner", auth.Owner.Address);
            Assert.Equal("02bb", auth.Manager.PubKey);
            Assert.Equal("Sdelegate", auth.Delegate.Address);
            Assert.Equal("02dd", auth.Controller.PubKey);
        }

        [Fact]
        public async Task Maps_peers()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.RespondWithResult(@"[
                {""addr"":""1.2.3.4:46501"",""version"":63000,""subver"":""/Stealth:3.3.5/"",
                 ""inbound"":false,""startingheight"":3456700,""conntime"":1755000000}
            ]");

            var peers = await client.GetPeerInfoAsync();

            var peer = Assert.Single(peers);
            Assert.Equal("1.2.3.4:46501", peer.Address);
            Assert.False(peer.Inbound);
            Assert.Equal(3456700, peer.StartingHeight);
        }

        [Fact]
        public async Task Staker_price_keeps_full_precision()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.RespondWithResult("31415.926535");

            Assert.Equal(31415.926535m, await client.GetStakerPriceAsync());
        }

        [Fact]
        public async Task Empty_mempool_returns_an_empty_list_not_null()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.RespondWithResult("[]");

            Assert.Empty(await client.GetRawMempoolAsync());
        }
    }

    public class ChainProtocolTests
    {
        [Fact]
        public async Task Getrawtransaction_sends_verbosity_as_an_integer()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.RespondWithResult("\"00aa\"");
            await client.GetRawTransactionAsync("ff", verbose: true);

            var parameters = (JArray)daemon.LastRequest["params"];
            Assert.Equal(1, parameters[1].Value<int>());

            await client.GetRawTransactionAsync("ff", verbose: false);
            Assert.Equal(0, ((JArray)daemon.LastRequest["params"])[1].Value<int>());
        }

        [Fact]
        public async Task Getsubsidy_omits_the_height_when_not_given()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.RespondWithResult("1.5");
            await client.GetSubsidyAsync();

            Assert.Empty((JArray)daemon.LastRequest["params"]);
        }

        [Fact]
        public async Task Liststakerunspent_sends_only_the_alias_by_default()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.RespondWithResult("[]");
            await client.ListStakerUnspentAsync("dragon");

            Assert.Single((JArray)daemon.LastRequest["params"]);
        }

        [Fact]
        public async Task Liststakerunspent_sends_the_full_set_when_authorities_are_given()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            daemon.RespondWithResult("[]");
            await client.ListStakerUnspentAsync("dragon", "owner", 3, 100);

            var parameters = (JArray)daemon.LastRequest["params"];
            Assert.Equal(4, parameters.Count);
            Assert.Equal("owner", parameters[1].Value<string>());
            Assert.Equal(3, parameters[2].Value<int>());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public async Task Alias_guards_reject_blank_input(string alias)
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            await Assert.ThrowsAsync<ArgumentException>(() => client.GetStakerIdAsync(alias));
            await Assert.ThrowsAsync<ArgumentException>(
                () => client.GetStakerAuthoritiesAsync(alias));
        }

        [Fact]
        public async Task Negative_heights_are_rejected_before_the_call()
        {
            using var daemon = new FakeDaemon();
            using var client = daemon.Client();

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => client.GetBlockHashAsync(-1));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => client.GetBlockByNumberAsync(-1));
        }
    }
}
