using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xst.Rpc.Models;

namespace Xst.Rpc
{
    public sealed partial class XstClient
    {
        public Task<decimal> GetBalanceAsync(string account = null, int minConfirmations = 1,
                                             CancellationToken cancellationToken = default)
        {
            var parameters = account == null
                ? null
                : new object[] { account, minConfirmations };

            return CallAsync<decimal>("getbalance", parameters, cancellationToken);
        }

        public Task<string> GetNewAddressAsync(string account = null,
                                               CancellationToken cancellationToken = default)
        {
            var parameters = account == null ? null : new object[] { account };
            return CallAsync<string>("getnewaddress", parameters, cancellationToken);
        }

        public Task<string> GetAccountAddressAsync(string account,
                                                   CancellationToken cancellationToken = default)
        {
            return CallAsync<string>("getaccountaddress",
                new object[] { account ?? string.Empty }, cancellationToken);
        }

        public Task<string> GetAccountAsync(string address,
                                            CancellationToken cancellationToken = default)
        {
            RequireAddress(address);
            return CallAsync<string>("getaccount", new object[] { address }, cancellationToken);
        }

        public async Task<IReadOnlyList<string>> GetAddressesByAccountAsync(
            string account, CancellationToken cancellationToken = default)
        {
            var rows = await CallAsync<List<string>>("getaddressesbyaccount",
                new object[] { account ?? string.Empty }, cancellationToken).ConfigureAwait(false);

            return rows ?? new List<string>();
        }

        public async Task<IReadOnlyDictionary<string, decimal>> ListAccountsAsync(
            int minConfirmations = 1, CancellationToken cancellationToken = default)
        {
            var rows = await CallAsync<Dictionary<string, decimal>>("listaccounts",
                new object[] { minConfirmations }, cancellationToken).ConfigureAwait(false);

            return rows ?? new Dictionary<string, decimal>();
        }

        public Task SetAccountAsync(string address, string account,
                                    CancellationToken cancellationToken = default)
        {
            RequireAddress(address);
            return CallVoidAsync("setaccount",
                new object[] { address, account ?? string.Empty }, cancellationToken);
        }

        public Task<XstAddressValidation> ValidateAddressAsync(string address,
                                                               CancellationToken cancellationToken = default)
        {
            RequireAddress(address);
            return CallAsync<XstAddressValidation>(
                "validateaddress", new object[] { address }, cancellationToken);
        }

        public Task<decimal> GetReceivedByAddressAsync(string address, int minConfirmations = 1,
                                                       CancellationToken cancellationToken = default)
        {
            RequireAddress(address);
            return CallAsync<decimal>("getreceivedbyaddress",
                new object[] { address, minConfirmations }, cancellationToken);
        }

        public Task<decimal> GetReceivedByAccountAsync(string account, int minConfirmations = 1,
                                                       CancellationToken cancellationToken = default)
        {
            return CallAsync<decimal>("getreceivedbyaccount",
                new object[] { account ?? string.Empty, minConfirmations }, cancellationToken);
        }

        public async Task<IReadOnlyList<XstReceivedByAddress>> ListReceivedByAddressAsync(
            int minConfirmations = 1, bool includeEmpty = false,
            CancellationToken cancellationToken = default)
        {
            var rows = await CallAsync<List<XstReceivedByAddress>>(
                "listreceivedbyaddress",
                new object[] { minConfirmations, includeEmpty },
                cancellationToken).ConfigureAwait(false);

            return rows ?? new List<XstReceivedByAddress>();
        }

        public Task<XstTransaction> GetTransactionAsync(string txid,
                                                        CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(txid))
                throw new ArgumentException("Transaction id must be set.", nameof(txid));

            return CallAsync<XstTransaction>("gettransaction",
                new object[] { txid }, cancellationToken);
        }

        public async Task<IReadOnlyList<XstTransactionSummary>> ListTransactionsAsync(
            string account = "*", int count = 10, int from = 0,
            CancellationToken cancellationToken = default)
        {
            var rows = await CallAsync<List<XstTransactionSummary>>("listtransactions",
                new object[] { account ?? "*", count, from },
                cancellationToken).ConfigureAwait(false);

            return rows ?? new List<XstTransactionSummary>();
        }

        public async Task<IReadOnlyList<XstUnspentOutput>> ListUnspentAsync(
            int minConfirmations = 1, int maxConfirmations = 9999999,
            IEnumerable<string> addresses = null,
            CancellationToken cancellationToken = default)
        {
            var filter = addresses == null ? null : new List<string>(addresses);

            var parameters = filter != null && filter.Count > 0
                ? new object[] { minConfirmations, maxConfirmations, filter }
                : new object[] { minConfirmations, maxConfirmations };

            var rows = await CallAsync<List<XstUnspentOutput>>("listunspent", parameters,
                cancellationToken).ConfigureAwait(false);

            return rows ?? new List<XstUnspentOutput>();
        }

        public Task<string> SendToAddressAsync(string address, decimal amount,
                                               string comment = null, string commentTo = null,
                                               bool feeless = false,
                                               IEnumerable<string> hexData = null,
                                               CancellationToken cancellationToken = default)
        {
            RequireAddress(address);
            var rounded = RequireSendable(amount);

            var data = hexData == null ? null : new List<string>(hexData);
            var hasData = data != null && data.Count > 0;

            object[] parameters;
            if (hasData)
            {
                parameters = new object[]
                {
                    address, rounded, comment ?? string.Empty, commentTo ?? string.Empty,
                    feeless, data
                };
            }
            else if (feeless)
            {
                parameters = new object[]
                {
                    address, rounded, comment ?? string.Empty, commentTo ?? string.Empty, true
                };
            }
            else
            {
                parameters = new object[] { address, rounded, comment, commentTo };
            }

            return CallAsync<string>("sendtoaddress", parameters, cancellationToken);
        }

        public Task<string> SendFromAsync(string account, string address, decimal amount,
                                          int minConfirmations = 1,
                                          string comment = null, string commentTo = null,
                                          CancellationToken cancellationToken = default)
        {
            RequireAddress(address);
            var rounded = RequireSendable(amount);

            return CallAsync<string>("sendfrom",
                new object[] { account ?? string.Empty, address, rounded, minConfirmations, comment, commentTo },
                cancellationToken);
        }

        public Task<string> SendManyAsync(string account,
                                          IDictionary<string, decimal> amounts,
                                          int minConfirmations = 1, string comment = null,
                                          CancellationToken cancellationToken = default)
        {
            if (amounts == null) throw new ArgumentNullException(nameof(amounts));
            if (amounts.Count == 0)
                throw new ArgumentException("At least one recipient is required.", nameof(amounts));

            var rounded = new Dictionary<string, decimal>(amounts.Count);
            foreach (var pair in amounts)
            {
                RequireAddress(pair.Key);
                rounded[pair.Key] = RequireSendable(pair.Value);
            }

            return CallAsync<string>("sendmany",
                new object[] { account ?? string.Empty, rounded, minConfirmations, comment },
                cancellationToken);
        }

        public Task<bool> MoveAsync(string fromAccount, string toAccount, decimal amount,
                                    int minConfirmations = 1, string comment = null,
                                    CancellationToken cancellationToken = default)
        {
            var rounded = RequireSendable(amount);

            return CallAsync<bool>("move",
                new object[] { fromAccount ?? string.Empty, toAccount ?? string.Empty, rounded, minConfirmations, comment },
                cancellationToken);
        }

        public Task<string> GetNewStealthAddressAsync(string label = null,
                                                      CancellationToken cancellationToken = default)
        {
            var parameters = label == null ? null : new object[] { label };
            return CallAsync<string>("getnewstealthaddress", parameters, cancellationToken);
        }

        public Task<Newtonsoft.Json.Linq.JToken> ListStealthAddressesAsync(
            bool showSecrets = false, CancellationToken cancellationToken = default)
        {
            return InvokeAsync("liststealthaddresses",
                new object[] { showSecrets }, cancellationToken);
        }

        public Task<string> SendToStealthAddressAsync(string stealthAddress, decimal amount,
                                                      string narration = null,
                                                      string comment = null, string commentTo = null,
                                                      CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(stealthAddress))
                throw new ArgumentException("Stealth address must be set.", nameof(stealthAddress));

            var rounded = RequireSendable(amount);

            return CallAsync<string>("sendtostealthaddress",
                new object[] { stealthAddress, rounded, narration, comment, commentTo },
                cancellationToken);
        }

        public Task WalletPassphraseAsync(string passphrase, TimeSpan timeout,
                                          bool mintOnly = false,
                                          CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(passphrase))
                throw new ArgumentException("Passphrase must be set.", nameof(passphrase));

            var seconds = (long)timeout.TotalSeconds;
            if (seconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be positive.");

            return CallVoidAsync("walletpassphrase",
                new object[] { passphrase, seconds, mintOnly }, cancellationToken);
        }

        public Task WalletLockAsync(CancellationToken cancellationToken = default)
        {
            return CallVoidAsync("walletlock", null, cancellationToken);
        }

        public Task WalletPassphraseChangeAsync(string oldPassphrase, string newPassphrase,
                                                CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(oldPassphrase))
                throw new ArgumentException("Old passphrase must be set.", nameof(oldPassphrase));
            if (string.IsNullOrEmpty(newPassphrase))
                throw new ArgumentException("New passphrase must be set.", nameof(newPassphrase));

            return CallVoidAsync("walletpassphrasechange",
                new object[] { oldPassphrase, newPassphrase }, cancellationToken);
        }

        public Task<string> SignMessageAsync(string address, string message,
                                             CancellationToken cancellationToken = default)
        {
            RequireAddress(address);
            return CallAsync<string>("signmessage",
                new object[] { address, message ?? string.Empty }, cancellationToken);
        }

        public Task<bool> VerifyMessageAsync(string address, string signature, string message,
                                             CancellationToken cancellationToken = default)
        {
            RequireAddress(address);
            return CallAsync<bool>("verifymessage",
                new object[] { address, signature, message ?? string.Empty }, cancellationToken);
        }

        public Task BackupWalletAsync(string destination,
                                      CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(destination))
                throw new ArgumentException("Destination must be set.", nameof(destination));

            return CallVoidAsync("backupwallet", new object[] { destination }, cancellationToken);
        }

        private static void RequireAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("Address must be set.", nameof(address));
        }

        private static decimal RequireSendable(decimal amount)
        {
            if (!XstAmount.IsSendable(amount))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    "Amount must be greater than zero and no more than "
                        + XstAmount.MaxMoney + " XST.");
            }

            return XstAmount.Round(amount);
        }
    }
}
