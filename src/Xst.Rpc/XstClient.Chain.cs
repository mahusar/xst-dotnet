using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xst.Rpc.Models;

namespace Xst.Rpc
{
    public sealed partial class XstClient
    {
        public Task<XstBlock> GetBlockAsync(string hash, bool verboseTransactions = false,
                                            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(hash))
                throw new ArgumentException("Block hash must be set.", nameof(hash));

            return CallAsync<XstBlock>("getblock",
                new object[] { hash, verboseTransactions }, cancellationToken);
        }

        public Task<XstBlock> GetBlockByNumberAsync(long height, bool verboseTransactions = false,
                                                    CancellationToken cancellationToken = default)
        {
            if (height < 0)
                throw new ArgumentOutOfRangeException(nameof(height), "Height cannot be negative.");

            return CallAsync<XstBlock>("getblockbynumber",
                new object[] { height, verboseTransactions }, cancellationToken);
        }

        public Task<string> GetBlockHashAsync(long height,
                                              CancellationToken cancellationToken = default)
        {
            if (height < 0)
                throw new ArgumentOutOfRangeException(nameof(height), "Height cannot be negative.");

            return CallAsync<string>("getblockhash", new object[] { height }, cancellationToken);
        }

        public Task<JToken> GetNewestBlockBeforeTimeAsync(long unixSeconds,
                                                          CancellationToken cancellationToken = default)
        {
            return InvokeAsync("getnewestblockbeforetime",
                new object[] { unixSeconds }, cancellationToken);
        }

        public Task<decimal> GetDifficultyAsync(CancellationToken cancellationToken = default)
        {
            return CallAsync<decimal>("getdifficulty", null, cancellationToken);
        }

        public Task<JToken> GetSubsidyAsync(long? height = null,
                                            CancellationToken cancellationToken = default)
        {
            var parameters = height.HasValue ? new object[] { height.Value } : null;
            return InvokeAsync("getsubsidy", parameters, cancellationToken);
        }

        public async Task<IReadOnlyList<string>> GetRawMempoolAsync(
            CancellationToken cancellationToken = default)
        {
            var rows = await CallAsync<List<string>>("getrawmempool", null, cancellationToken)
                .ConfigureAwait(false);

            return rows ?? new List<string>();
        }

        public Task<JToken> GetRawTransactionAsync(string txid, bool verbose = false,
                                                   CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(txid))
                throw new ArgumentException("Transaction id must be set.", nameof(txid));

            return InvokeAsync("getrawtransaction",
                new object[] { txid, verbose ? 1 : 0 }, cancellationToken);
        }

        public Task<JToken> DecodeRawTransactionAsync(string hex,
                                                      CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(hex))
                throw new ArgumentException("Raw transaction hex must be set.", nameof(hex));

            return InvokeAsync("decoderawtransaction", new object[] { hex }, cancellationToken);
        }

        public Task<string> SendRawTransactionAsync(string hex,
                                                    CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(hex))
                throw new ArgumentException("Raw transaction hex must be set.", nameof(hex));

            return CallAsync<string>("sendrawtransaction", new object[] { hex }, cancellationToken);
        }

        public async Task<IReadOnlyList<XstPeer>> GetPeerInfoAsync(
            CancellationToken cancellationToken = default)
        {
            var rows = await CallAsync<List<XstPeer>>("getpeerinfo", null, cancellationToken)
                .ConfigureAwait(false);

            return rows ?? new List<XstPeer>();
        }

        public Task<decimal> GetStakerPriceAsync(CancellationToken cancellationToken = default)
        {
            return CallAsync<decimal>("getstakerprice", null, cancellationToken);
        }

        public Task<long> GetStakerIdAsync(string alias,
                                           CancellationToken cancellationToken = default)
        {
            RequireAlias(alias);
            return CallAsync<long>("getstakerid", new object[] { alias }, cancellationToken);
        }

        public Task<XstStakerAuthorities> GetStakerAuthoritiesAsync(
            string alias, CancellationToken cancellationToken = default)
        {
            RequireAlias(alias);
            return CallAsync<XstStakerAuthorities>("getstakerauthorities",
                new object[] { alias }, cancellationToken);
        }

        public Task<decimal> GetQPoSBalanceAsync(string pubKey,
                                                 CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(pubKey))
                throw new ArgumentException("Public key must be set.", nameof(pubKey));

            return CallAsync<decimal>("getqposbalance", new object[] { pubKey }, cancellationToken);
        }

        public async Task<IReadOnlyList<XstUnspentOutput>> ListStakerUnspentAsync(
            string alias, string authorities = null,
            int minConfirmations = 1, int maxConfirmations = 999999999,
            CancellationToken cancellationToken = default)
        {
            RequireAlias(alias);

            var parameters = authorities == null
                ? new object[] { alias }
                : new object[] { alias, authorities, minConfirmations, maxConfirmations };

            var rows = await CallAsync<List<XstUnspentOutput>>("liststakerunspent", parameters,
                cancellationToken).ConfigureAwait(false);

            return rows ?? new List<XstUnspentOutput>();
        }

        public Task<JToken> GetStakerInfoAsync(string alias,
                                               CancellationToken cancellationToken = default)
        {
            RequireAlias(alias);
            return InvokeAsync("getstakerinfo", new object[] { alias }, cancellationToken);
        }

        public Task<JToken> GetQPoSInfoAsync(CancellationToken cancellationToken = default)
        {
            return InvokeAsync("getqposinfo", null, cancellationToken);
        }

        public Task<JToken> GetStakerSummaryAsync(CancellationToken cancellationToken = default)
        {
            return InvokeAsync("getstakersummary", null, cancellationToken);
        }

        public Task<JToken> GetBlockScheduleAsync(long? height = null,
                                                  CancellationToken cancellationToken = default)
        {
            var parameters = height.HasValue ? new object[] { height.Value } : null;
            return InvokeAsync("getblockschedule", parameters, cancellationToken);
        }

        public Task<JToken> GetQueueSummaryAsync(CancellationToken cancellationToken = default)
        {
            return InvokeAsync("getqueuesummary", null, cancellationToken);
        }

        public Task<JToken> GetStakersByIdAsync(CancellationToken cancellationToken = default)
        {
            return InvokeAsync("getstakersbyid", null, cancellationToken);
        }

        public Task<JToken> GetStakersByWeightAsync(CancellationToken cancellationToken = default)
        {
            return InvokeAsync("getstakersbyweight", null, cancellationToken);
        }

        private static void RequireAlias(string alias)
        {
            if (string.IsNullOrWhiteSpace(alias))
                throw new ArgumentException("Staker alias must be set.", nameof(alias));
        }
    }
}
