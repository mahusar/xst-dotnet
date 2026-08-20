using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xst.Rpc.Models;

namespace Xst.Rpc
{
    public sealed partial class XstClient
    {
        public const int DefaultPageSize = 100;

        public Task<decimal> GetAddressBalanceAsync(string address,
                                                    CancellationToken cancellationToken = default)
        {
            RequireAddress(address);
            return CallAsync<decimal>("getaddressbalance",
                new object[] { address }, cancellationToken);
        }

        public Task<XstAddressInfo> GetAddressInfoAsync(string address,
                                                        CancellationToken cancellationToken = default)
        {
            RequireAddress(address);
            return CallAsync<XstAddressInfo>("getaddressinfo",
                new object[] { address }, cancellationToken);
        }

        public Task<IReadOnlyList<XstAddressInOut>> GetAddressInputsAsync(
            string address, int start = 1, int max = DefaultPageSize,
            CancellationToken cancellationToken = default)
        {
            return GetAddressRangeAsync("getaddressinputs", address, start, max, cancellationToken);
        }

        public Task<IReadOnlyList<XstAddressInOut>> GetAddressOutputsAsync(
            string address, int start = 1, int max = DefaultPageSize,
            CancellationToken cancellationToken = default)
        {
            return GetAddressRangeAsync("getaddressoutputs", address, start, max, cancellationToken);
        }

        public Task<IReadOnlyList<XstAddressInOut>> GetAddressInOutsAsync(
            string address, int start = 1, int max = DefaultPageSize,
            CancellationToken cancellationToken = default)
        {
            return GetAddressRangeAsync("getaddressinouts", address, start, max, cancellationToken);
        }

        public Task<XstPage<XstAddressTx>> GetAddressTxsPageAsync(
            string address, int page = 1, int perPage = DefaultPageSize, bool forward = true,
            CancellationToken cancellationToken = default)
        {
            return GetAddressPageAsync<XstAddressTx>("getaddresstxspg", address, page, perPage,
                forward, cancellationToken);
        }

        public Task<XstPage<XstAddressInOut>> GetAddressInOutsPageAsync(
            string address, int page = 1, int perPage = DefaultPageSize, bool forward = true,
            CancellationToken cancellationToken = default)
        {
            return GetAddressPageAsync<XstAddressInOut>("getaddressinoutspg", address, page,
                perPage, forward, cancellationToken);
        }

        public Task<long> GetRichListSizeAsync(decimal? minBalance = null,
                                               CancellationToken cancellationToken = default)
        {
            var parameters = minBalance.HasValue
                ? new object[] { XstAmount.Round(minBalance.Value) }
                : null;

            return CallAsync<long>("getrichlistsize", parameters, cancellationToken);
        }

        public Task<Newtonsoft.Json.Linq.JToken> GetRichListAsync(
            int start = 1, int max = DefaultPageSize,
            CancellationToken cancellationToken = default)
        {
            RequireRange(start, max);
            return InvokeAsync("getrichlist", new object[] { start, max }, cancellationToken);
        }

        public Task<XstPage<Newtonsoft.Json.Linq.JToken>> GetRichListPageAsync(
            int page = 1, int perPage = DefaultPageSize, bool forward = true,
            CancellationToken cancellationToken = default)
        {
            RequirePage(page, perPage);
            return CallAsync<XstPage<Newtonsoft.Json.Linq.JToken>>("getrichlistpg",
                new object[] { page, perPage, forward }, cancellationToken);
        }

        public Task<XstChildKey> GetChildKeyAsync(string extendedKey, int child,
                                                  int? networkByte = null,
                                                  CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(extendedKey))
                throw new ArgumentException("Extended key must be set.", nameof(extendedKey));
            if (child < 0)
                throw new ArgumentOutOfRangeException(nameof(child), "Child index cannot be negative.");

            var parameters = networkByte.HasValue
                ? new object[] { extendedKey, child, networkByte.Value }
                : new object[] { extendedKey, child };

            return CallAsync<XstChildKey>("getchildkey", parameters, cancellationToken);
        }

        public Task<XstHdAddresses> GetHdAddressesAsync(string extendedKey,
                                                        CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(extendedKey))
                throw new ArgumentException("Extended key must be set.", nameof(extendedKey));

            return CallAsync<XstHdAddresses>("gethdaddresses",
                new object[] { extendedKey }, cancellationToken);
        }

        public Task<Newtonsoft.Json.Linq.JToken> GetHdAccountAsync(
            string extendedKey, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(extendedKey))
                throw new ArgumentException("Extended key must be set.", nameof(extendedKey));

            return InvokeAsync("gethdaccount", new object[] { extendedKey }, cancellationToken);
        }

        public Task<XstPage<Newtonsoft.Json.Linq.JToken>> GetHdAccountPageAsync(
            string extendedKey, int page = 1, int perPage = DefaultPageSize, bool forward = true,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(extendedKey))
                throw new ArgumentException("Extended key must be set.", nameof(extendedKey));

            RequirePage(page, perPage);

            return CallAsync<XstPage<Newtonsoft.Json.Linq.JToken>>("gethdaccountpg",
                new object[] { extendedKey, page, perPage, forward }, cancellationToken);
        }

        private async Task<IReadOnlyList<XstAddressInOut>> GetAddressRangeAsync(
            string method, string address, int start, int max,
            CancellationToken cancellationToken)
        {
            RequireAddress(address);
            RequireRange(start, max);

            var rows = await CallAsync<List<XstAddressInOut>>(method,
                new object[] { address, start, max }, cancellationToken).ConfigureAwait(false);

            return rows ?? new List<XstAddressInOut>();
        }

        private Task<XstPage<T>> GetAddressPageAsync<T>(
            string method, string address, int page, int perPage, bool forward,
            CancellationToken cancellationToken)
        {
            RequireAddress(address);
            RequirePage(page, perPage);

            return CallAsync<XstPage<T>>(method,
                new object[] { address, page, perPage, forward }, cancellationToken);
        }

        private static void RequireRange(int start, int max)
        {
            if (start < 1)
                throw new ArgumentOutOfRangeException(nameof(start), "Start is one based.");
            if (max < 1)
                throw new ArgumentOutOfRangeException(nameof(max), "Max must be at least 1.");
        }

        private static void RequirePage(int page, int perPage)
        {
            if (page < 1)
                throw new ArgumentOutOfRangeException(nameof(page), "Page is one based.");
            if (perPage < 1)
                throw new ArgumentOutOfRangeException(nameof(perPage), "Per page must be at least 1.");
        }
    }
}
