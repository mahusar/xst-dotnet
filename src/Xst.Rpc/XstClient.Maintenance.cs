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
        public Task<JToken> CheckWalletAsync(CancellationToken cancellationToken = default)
        {
            return InvokeAsync("checkwallet", null, cancellationToken);
        }

        public Task<JToken> RepairWalletAsync(CancellationToken cancellationToken = default)
        {
            return InvokeAsync("repairwallet", null, cancellationToken);
        }

        public Task ResendTxAsync(CancellationToken cancellationToken = default)
        {
            return CallVoidAsync("resendtx", null, cancellationToken);
        }

        public Task<JToken> ClearWalletTransactionsAsync(CancellationToken cancellationToken = default)
        {
            return InvokeAsync("clearwallettransactions", null, cancellationToken);
        }

        public Task<JToken> ScanForAllTransactionsAsync(long? fromHeight = null,
                                                        CancellationToken cancellationToken = default)
        {
            var parameters = fromHeight.HasValue ? new object[] { fromHeight.Value } : null;
            return InvokeAsync("scanforalltxns", parameters, cancellationToken);
        }

        public Task<JToken> ScanForStealthTransactionsAsync(long? fromHeight = null,
                                                            CancellationToken cancellationToken = default)
        {
            var parameters = fromHeight.HasValue ? new object[] { fromHeight.Value } : null;
            return InvokeAsync("scanforstealthtxns", parameters, cancellationToken);
        }

        public Task<JToken> ReserveBalanceAsync(bool? reserve = null, decimal? amount = null,
                                                CancellationToken cancellationToken = default)
        {
            object[] parameters;
            if (!reserve.HasValue)
            {
                parameters = null;
            }
            else if (amount.HasValue)
            {
                parameters = new object[] { reserve.Value, XstAmount.Round(amount.Value) };
            }
            else
            {
                parameters = new object[] { reserve.Value };
            }

            return InvokeAsync("reservebalance", parameters, cancellationToken);
        }

        public Task<bool> SetTxFeeAsync(decimal fee, CancellationToken cancellationToken = default)
        {
            if (fee < 0m)
                throw new ArgumentOutOfRangeException(nameof(fee), "Fee cannot be negative.");

            return CallAsync<bool>("settxfee",
                new object[] { XstAmount.Round(fee) }, cancellationToken);
        }

        public Task<JToken> GetCheckpointAsync(CancellationToken cancellationToken = default)
        {
            return InvokeAsync("getcheckpoint", null, cancellationToken);
        }

        public Task<JToken> ListAddressGroupingsAsync(CancellationToken cancellationToken = default)
        {
            return InvokeAsync("listaddressgroupings", null, cancellationToken);
        }

        public Task<JToken> ListReceivedByAccountAsync(int minConfirmations = 1,
                                                       bool includeEmpty = false,
                                                       CancellationToken cancellationToken = default)
        {
            return InvokeAsync("listreceivedbyaccount",
                new object[] { minConfirmations, includeEmpty }, cancellationToken);
        }

        public Task<JToken> ListSinceBlockAsync(string blockHash = null,
                                                int targetConfirmations = 1,
                                                CancellationToken cancellationToken = default)
        {
            var parameters = blockHash == null
                ? null
                : new object[] { blockHash, targetConfirmations };

            return InvokeAsync("listsinceblock", parameters, cancellationToken);
        }

        public Task<JToken> GetTxVolumeAsync(long period, long windowSize, long windowSpacing,
                                             CancellationToken cancellationToken = default)
        {
            RequireWindow(period, windowSize, windowSpacing);
            return InvokeAsync("gettxvolume",
                new object[] { period, windowSize, windowSpacing }, cancellationToken);
        }

        public Task<JToken> GetXstVolumeAsync(long period, long windowSize, long windowSpacing,
                                              CancellationToken cancellationToken = default)
        {
            RequireWindow(period, windowSize, windowSpacing);
            return InvokeAsync("getxstvolume",
                new object[] { period, windowSize, windowSpacing }, cancellationToken);
        }

        public Task<JToken> GetBlockIntervalAsync(long period, long windowSize, long windowSpacing,
                                                  CancellationToken cancellationToken = default)
        {
            RequireWindow(period, windowSize, windowSpacing);
            return InvokeAsync("getblockinterval",
                new object[] { period, windowSize, windowSpacing }, cancellationToken);
        }

        public Task<JToken> GetBlockIntervalMeanAsync(long period, long windowSize, long windowSpacing,
                                                      CancellationToken cancellationToken = default)
        {
            RequireWindow(period, windowSize, windowSpacing);
            return InvokeAsync("getblockintervalmean",
                new object[] { period, windowSize, windowSpacing }, cancellationToken);
        }

        public Task<JToken> GetBlockIntervalRmsdAsync(long period, long windowSize, long windowSpacing,
                                                      CancellationToken cancellationToken = default)
        {
            RequireWindow(period, windowSize, windowSpacing);
            return InvokeAsync("getblockintervalrmsd",
                new object[] { period, windowSize, windowSpacing }, cancellationToken);
        }

        public Task<JToken> GetPicoPowerMeanAsync(long period, long windowSize, long windowSpacing,
                                                  CancellationToken cancellationToken = default)
        {
            RequireWindow(period, windowSize, windowSpacing);
            return InvokeAsync("getpicopowermean",
                new object[] { period, windowSize, windowSpacing }, cancellationToken);
        }

        public Task<JToken> GetHourlyMissedAsync(long? hours = null,
                                                 CancellationToken cancellationToken = default)
        {
            var parameters = hours.HasValue ? new object[] { hours.Value } : null;
            return InvokeAsync("gethourlymissed", parameters, cancellationToken);
        }

        public Task<JToken> GetRecentQueueAsync(int blocks,
                                                CancellationToken cancellationToken = default)
        {
            if (blocks < 1)
                throw new ArgumentOutOfRangeException(nameof(blocks), "Blocks must be at least 1.");

            return InvokeAsync("getrecentqueue", new object[] { blocks }, cancellationToken);
        }

        public Task<JToken> GetCertifiedNodesAsync(CancellationToken cancellationToken = default)
        {
            return InvokeAsync("getcertifiednodes", null, cancellationToken);
        }

        private static void RequireWindow(long period, long windowSize, long windowSpacing)
        {
            if (period < 1)
                throw new ArgumentOutOfRangeException(nameof(period), "Period must be at least 1.");
            if (windowSize < 1)
                throw new ArgumentOutOfRangeException(nameof(windowSize), "Window size must be at least 1.");
            if (windowSpacing < 1)
                throw new ArgumentOutOfRangeException(nameof(windowSpacing), "Window spacing must be at least 1.");
        }
    }
}
