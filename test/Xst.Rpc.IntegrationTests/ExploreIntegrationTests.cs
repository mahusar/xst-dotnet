using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xst.Rpc;
using Xst.Rpc.Models;
using Xunit;
using Xunit.Abstractions;

namespace Xst.Rpc.IntegrationTests
{
    public class ExploreIntegrationTests
    {
        private readonly ITestOutputHelper _output;

        public ExploreIntegrationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private static async Task<string> FindActiveAddressAsync(XstClient client)
        {
            var received = await client.ListReceivedByAddressAsync(0, false);
            var row = received.FirstOrDefault(r => r.Amount > 0m);
            return row?.Address;
        }

        [ExploreFact]
        public async Task Addressinfo_maps_cleanly()
        {
            using var client = DaemonSettings.CreateClient();

            var address = await FindActiveAddressAsync(client);
            if (address == null)
            {
                _output.WriteLine("wallet has received nothing, nothing to check");
                return;
            }

            var raw = await client.InvokeAsync("getaddressinfo", address);
            SchemaGuard.AssertFullyMapped<XstAddressInfo>(raw, "getaddressinfo");

            var info = await client.GetAddressInfoAsync(address);

            Assert.Equal(address, info.Address);
            Assert.Equal(info.Inputs + info.Outputs, info.InOuts);

            _output.WriteLine("address   " + info.Address);
            _output.WriteLine("balance   " + XstAmount.Format(info.Balance));
            _output.WriteLine("received  " + XstAmount.Format(info.Received));
            _output.WriteLine("in-outs   " + info.InOuts);
            _output.WriteLine("rank      " + info.Rank);
        }

        [ExploreFact]
        public async Task Addressbalance_agrees_with_addressinfo()
        {
            using var client = DaemonSettings.CreateClient();

            var address = await FindActiveAddressAsync(client);
            if (address == null)
            {
                _output.WriteLine("wallet has received nothing, nothing to check");
                return;
            }

            var balance = await client.GetAddressBalanceAsync(address);
            var info = await client.GetAddressInfoAsync(address);

            Assert.Equal(info.Balance, balance);
        }

        [ExploreFact]
        public async Task Address_inouts_map_cleanly()
        {
            using var client = DaemonSettings.CreateClient();

            var address = await FindActiveAddressAsync(client);
            if (address == null)
            {
                _output.WriteLine("wallet has received nothing, nothing to check");
                return;
            }

            var raw = await client.InvokeAsync("getaddressinouts", address, 1, 5);
            SchemaGuard.AssertFullyMapped<List<XstAddressInOut>>(raw, "getaddressinouts");

            var rows = await client.GetAddressInOutsAsync(address, 1, 5);
            Assert.NotEmpty(rows);

            foreach (var row in rows)
            {
                Assert.True(row.IsInput ^ row.IsOutput);
                Assert.NotNull(row.TxId);

                if (row.IsInput)
                    Assert.NotNull(row.PrevTxId);
                else
                    Assert.NotNull(row.IsSpent);
            }

            foreach (var row in rows.Take(3))
            {
                _output.WriteLine((row.IsOutput ? "out " : "in  ") + row.TxId +
                                  "  balance " + XstAmount.Format(row.Balance));
            }
        }

        [ExploreFact]
        public async Task Address_txs_map_cleanly()
        {
            using var client = DaemonSettings.CreateClient();

            var address = await FindActiveAddressAsync(client);
            if (address == null)
            {
                _output.WriteLine("wallet has received nothing, nothing to check");
                return;
            }

            var raw = await client.InvokeAsync("getaddresstxspg", address, 1, 2, true);
            SchemaGuard.AssertFullyMapped<XstPage<XstAddressTx>>(raw, "getaddresstxspg");

            var page = await client.GetAddressTxsPageAsync(address, 1, 2);
            Assert.NotEmpty(page.Data);

            foreach (var row in page.Data)
            {
                Assert.NotNull(row.TxId);
                Assert.NotNull(row.TxInfo);

                var inputs = row.AddressInputs?.Count ?? 0;
                var outputs = row.AddressOutputs?.Count ?? 0;

                Assert.True(inputs + outputs > 0);

                _output.WriteLine(row.TxId + "  height " + row.TxInfo.Height +
                                  "  in " + inputs + "  out " + outputs +
                                  "  flags " + string.Join(",", row.TxInfo.TxFlags ?? new List<string>()));
            }
        }

        [ExploreFact]
        public async Task Paging_reports_a_consistent_envelope()
        {
            using var client = DaemonSettings.CreateClient();

            var address = await FindActiveAddressAsync(client);
            if (address == null)
            {
                _output.WriteLine("wallet has received nothing, nothing to check");
                return;
            }

            var raw = await client.InvokeAsync("getaddressinoutspg", address, 1, 2, true);
            SchemaGuard.AssertFullyMapped<XstPage<XstAddressInOut>>(raw, "getaddressinoutspg");

            var page = await client.GetAddressInOutsPageAsync(address, 1, 2);

            Assert.Equal(1, page.Page);
            Assert.Equal(2, page.PerPage);
            Assert.True(page.Total >= page.Data.Count);
            Assert.True(page.LastPage >= 1);
            Assert.Equal(page.Page < page.LastPage, page.HasMore);

            _output.WriteLine("total     " + page.Total);
            _output.WriteLine("last page " + page.LastPage);
            _output.WriteLine("has more  " + page.HasMore);
        }

        [ExploreFact]
        public async Task Rich_list_size_is_positive()
        {
            using var client = DaemonSettings.CreateClient();

            var size = await client.GetRichListSizeAsync();
            Assert.True(size > 0);

            _output.WriteLine("rich list " + size);
        }
    }

    public class SpendIntegrationTests
    {
        private readonly ITestOutputHelper _output;

        public SpendIntegrationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [SpendFact]
        public void Spend_amount_stays_within_the_cap()
        {
            var amount = DaemonSettings.RequireSpendAmount();

            Assert.InRange(amount, 0.000001m, DaemonSettings.MaxSpendAmount);
            Assert.Equal(amount, XstAmount.Round(amount));

            _output.WriteLine("configured amount " + XstAmount.Format(amount) +
                              " XST, cap " + XstAmount.Format(DaemonSettings.MaxSpendAmount));
        }

        [SpendFact]
        public async Task Sends_feeless_with_an_op_return_payload()
        {
            using var client = DaemonSettings.CreateClient();

            var info = await client.GetInfoAsync();

            if (!info.Testnet && !DaemonSettings.AllowsMainnetSpending)
            {
                _output.WriteLine("daemon is on mainnet. Set " +
                                  DaemonSettings.MainnetSpendVariable +
                                  "=1 to allow a real send.");
                return;
            }

            var amount = DaemonSettings.RequireSpendAmount();

            if (info.Balance < amount)
            {
                _output.WriteLine("balance is " + XstAmount.Format(info.Balance) +
                                  ", less than the " + XstAmount.Format(amount) + " to send");
                return;
            }

            var destination = await client.GetNewAddressAsync();

            var anchor = string.Concat(Enumerable.Repeat("ab", 20));

            _output.WriteLine("network   " + (info.Testnet ? "testnet" : "MAINNET"));
            _output.WriteLine("sending   " + XstAmount.Format(amount) + " XST to " + destination);

            var txid = await client.SendToAddressAsync(
                destination, amount, comment: "xst-dotnet integration",
                feeless: true, hexData: new[] { anchor });

            Assert.False(string.IsNullOrWhiteSpace(txid));

            var tx = await client.GetTransactionAsync(txid);
            Assert.Equal(txid, tx.TxId);

            var fee = tx.Fee.GetValueOrDefault();

            _output.WriteLine("txid      " + txid);
            _output.WriteLine("version   " + tx.Version);
            _output.WriteLine("amount    " + XstAmount.Format(tx.Amount));
            _output.WriteLine("fee       " + (tx.Fee.HasValue ? XstAmount.Format(fee) : "none"));

            Assert.NotNull(tx.Vout);
            Assert.Contains(anchor, tx.Vout.ToString());

            Assert.True(tx.Version >= 3, "unexpected transaction version " + tx.Version);

            Assert.True(fee == 0m,
                "the daemon charged " + XstAmount.Format(fee) +
                " XST despite feeless being requested");
        }
    }
}
