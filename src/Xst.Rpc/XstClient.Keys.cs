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
        public Task<string> DumpPrivKeyAsync(string address,
                                             CancellationToken cancellationToken = default)
        {
            RequireAddress(address);
            return CallAsync<string>("dumpprivkey", new object[] { address }, cancellationToken);
        }

        public Task ImportPrivKeyAsync(string privateKey, string label = null,
                                       CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(privateKey))
                throw new ArgumentException("Private key must be set.", nameof(privateKey));

            return CallVoidAsync("importprivkey",
                new object[] { privateKey, label }, cancellationToken);
        }

        public Task ImportAddressAsync(string address, string label = null, bool rescan = true,
                                       CancellationToken cancellationToken = default)
        {
            RequireAddress(address);
            return CallVoidAsync("importaddress",
                new object[] { address, label ?? string.Empty, rescan }, cancellationToken);
        }

        public Task ImportStealthAddressAsync(string scanSecret, string spendSecret,
                                              string label = null,
                                              CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(scanSecret))
                throw new ArgumentException("Scan secret must be set.", nameof(scanSecret));
            if (string.IsNullOrWhiteSpace(spendSecret))
                throw new ArgumentException("Spend secret must be set.", nameof(spendSecret));

            return CallVoidAsync("importstealthaddress",
                new object[] { scanSecret, spendSecret, label }, cancellationToken);
        }

        public Task<JToken> GetNewPubKeyAsync(string account = null,
                                              CancellationToken cancellationToken = default)
        {
            var parameters = account == null ? null : new object[] { account };
            return InvokeAsync("getnewpubkey", parameters, cancellationToken);
        }

        public Task<JToken> ValidatePubKeyAsync(string pubKey,
                                                CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(pubKey))
                throw new ArgumentException("Public key must be set.", nameof(pubKey));

            return InvokeAsync("validatepubkey", new object[] { pubKey }, cancellationToken);
        }

        public Task<JToken> MakeKeyPairAsync(string prefix = null,
                                             CancellationToken cancellationToken = default)
        {
            var parameters = prefix == null ? null : new object[] { prefix };
            return InvokeAsync("makekeypair", parameters, cancellationToken);
        }

        public Task KeyPoolRefillAsync(int? newSize = null,
                                       CancellationToken cancellationToken = default)
        {
            var parameters = newSize.HasValue ? new object[] { newSize.Value } : null;
            return CallVoidAsync("keypoolrefill", parameters, cancellationToken);
        }

        public Task<string> EncryptWalletAsync(string passphrase,
                                               CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(passphrase))
                throw new ArgumentException("Passphrase must be set.", nameof(passphrase));

            return CallAsync<string>("encryptwallet",
                new object[] { passphrase }, cancellationToken);
        }

        public Task<string> AddMultiSigAddressAsync(int required, IEnumerable<string> keys,
                                                    string account = null,
                                                    CancellationToken cancellationToken = default)
        {
            if (keys == null) throw new ArgumentNullException(nameof(keys));

            var list = new List<string>(keys);
            if (required < 1)
                throw new ArgumentOutOfRangeException(nameof(required), "At least one signature is required.");
            if (list.Count < required)
                throw new ArgumentException("Fewer keys than required signatures.", nameof(keys));

            return CallAsync<string>("addmultisigaddress",
                new object[] { required, list, account }, cancellationToken);
        }

        public Task<string> CreateRawTransactionAsync(
            IEnumerable<XstOutPoint> inputs, IDictionary<string, decimal> outputs,
            CancellationToken cancellationToken = default)
        {
            if (inputs == null) throw new ArgumentNullException(nameof(inputs));
            if (outputs == null) throw new ArgumentNullException(nameof(outputs));

            var vin = new List<XstOutPoint>(inputs);
            if (vin.Count == 0)
                throw new ArgumentException("At least one input is required.", nameof(inputs));
            if (outputs.Count == 0)
                throw new ArgumentException("At least one output is required.", nameof(outputs));

            var rounded = new Dictionary<string, decimal>(outputs.Count);
            foreach (var pair in outputs)
            {
                RequireAddress(pair.Key);
                rounded[pair.Key] = RequireSendable(pair.Value);
            }

            return CallAsync<string>("createrawtransaction",
                new object[] { vin, rounded }, cancellationToken);
        }

        public Task<XstSignedTransaction> SignRawTransactionAsync(
            string hex,
            IEnumerable<XstPreviousOutput> previousOutputs = null,
            IEnumerable<string> privateKeys = null,
            string sigHashType = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(hex))
                throw new ArgumentException("Raw transaction hex must be set.", nameof(hex));

            var prevs = previousOutputs == null ? null : new List<XstPreviousOutput>(previousOutputs);
            var keys = privateKeys == null ? null : new List<string>(privateKeys);

            return CallAsync<XstSignedTransaction>("signrawtransaction",
                new object[] { hex, prevs, keys, sigHashType }, cancellationToken);
        }

        public Task<JToken> CreateFeeworkAsync(string rawTransaction, long height,
                                               int? blockSize = null,
                                               CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(rawTransaction))
                throw new ArgumentException("Raw transaction hex must be set.", nameof(rawTransaction));
            if (height < 0)
                throw new ArgumentOutOfRangeException(nameof(height), "Height cannot be negative.");

            var parameters = blockSize.HasValue
                ? new object[] { rawTransaction, height, blockSize.Value }
                : new object[] { rawTransaction, height };

            return InvokeAsync("createfeework", parameters, cancellationToken);
        }
    }
}
