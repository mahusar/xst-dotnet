using System.Collections.Generic;
using Newtonsoft.Json;

namespace Xst.Rpc.Models
{
    public sealed class XstAddressInfo
    {
        [JsonProperty("address")] public string Address { get; set; }
        [JsonProperty("balance")] public decimal Balance { get; set; }

        [JsonProperty("rank")] public long Rank { get; set; }

        [JsonProperty("transactions")] public long Transactions { get; set; }
        [JsonProperty("outputs")] public long Outputs { get; set; }
        [JsonProperty("received")] public decimal Received { get; set; }
        [JsonProperty("inputs")] public long Inputs { get; set; }
        [JsonProperty("sent")] public decimal Sent { get; set; }
        [JsonProperty("unspent")] public long Unspent { get; set; }

        [JsonProperty("in-outs")] public long InOuts { get; set; }

        [JsonProperty("blocks")] public long Blocks { get; set; }
    }

    public sealed class XstAddressInput
    {
        [JsonProperty("vin")] public long Vin { get; set; }
        [JsonProperty("prev_txid")] public string PrevTxId { get; set; }
        [JsonProperty("prev_vout")] public long PrevVout { get; set; }
        [JsonProperty("amount")] public decimal Amount { get; set; }
    }

    public sealed class XstAddressOutput
    {
        [JsonProperty("vout")] public long Vout { get; set; }
        [JsonProperty("amount")] public decimal Amount { get; set; }

        [JsonProperty("isspent")] public bool IsSpent { get; set; }

        [JsonProperty("next_txid")] public string NextTxId { get; set; }

        [JsonProperty("next_vin")] public long? NextVin { get; set; }
    }

    public sealed class XstAddressInOut
    {
        [JsonProperty("txid")] public string TxId { get; set; }
        [JsonProperty("address")] public string Address { get; set; }
        [JsonProperty("amount")] public decimal Amount { get; set; }

        [JsonProperty("balance")] public decimal Balance { get; set; }

        [JsonProperty("height")] public long Height { get; set; }

        [JsonProperty("vtx")] public long Vtx { get; set; }

        [JsonProperty("blockhash")] public string BlockHash { get; set; }
        [JsonProperty("confirmations")] public long Confirmations { get; set; }

        [JsonProperty("blocktime")] public long BlockTime { get; set; }

        [JsonProperty("vout")] public long? Vout { get; set; }

        [JsonProperty("isspent")] public bool? IsSpent { get; set; }

        [JsonProperty("next_txid")] public string NextTxId { get; set; }

        [JsonProperty("next_in")] public long? NextIn { get; set; }

        [JsonProperty("vin")] public long? Vin { get; set; }

        [JsonProperty("prev_txid")] public string PrevTxId { get; set; }

        [JsonProperty("prev_vout")] public long? PrevVout { get; set; }

        [JsonIgnore] public bool IsOutput { get { return Vout.HasValue; } }

        [JsonIgnore] public bool IsInput { get { return Vin.HasValue; } }
    }

    public sealed class XstExploreDestination
    {
        [JsonProperty("addresses")] public List<string> Addresses { get; set; }
        [JsonProperty("reqSigs")] public long? RequiredSignatures { get; set; }
        [JsonProperty("amount")] public decimal Amount { get; set; }

        [JsonProperty("type")] public string Type { get; set; }
    }

    public sealed class XstExploreTx
    {
        [JsonProperty("blockhash")] public string BlockHash { get; set; }

        [JsonProperty("blocktime")] public long BlockTime { get; set; }

        [JsonProperty("height")] public long Height { get; set; }
        [JsonProperty("confirmations")] public long? Confirmations { get; set; }

        [JsonProperty("vtx")] public long Vtx { get; set; }

        [JsonProperty("sources")] public List<XstExploreDestination> Sources { get; set; }
        [JsonProperty("destinations")] public List<XstExploreDestination> Destinations { get; set; }
        [JsonProperty("txflags")] public List<string> TxFlags { get; set; }
        [JsonProperty("txtype")] public string TxType { get; set; }
    }

    public sealed class XstAddressTx
    {
        [JsonProperty("txid")] public string TxId { get; set; }

        [JsonProperty("balance")] public decimal Balance { get; set; }

        [JsonProperty("address_inputs")] public List<XstAddressInput> AddressInputs { get; set; }
        [JsonProperty("address_outputs")] public List<XstAddressOutput> AddressOutputs { get; set; }
        [JsonProperty("txinfo")] public XstExploreTx TxInfo { get; set; }
    }

    public sealed class XstChildKey
    {
        [JsonProperty("extended")] public string Extended { get; set; }
        [JsonProperty("pubkey")] public string PubKey { get; set; }
        [JsonProperty("address")] public string Address { get; set; }
    }

    public sealed class XstHdAddress
    {
        [JsonProperty("child")] public int Child { get; set; }
        [JsonProperty("pubkey")] public string PubKey { get; set; }
        [JsonProperty("address")] public string Address { get; set; }
        [JsonProperty("inouts")] public long InOuts { get; set; }
    }

    public sealed class XstHdAddresses
    {
        [JsonProperty("external")] public List<XstHdAddress> External { get; set; }
        [JsonProperty("change")] public List<XstHdAddress> Change { get; set; }
    }
}
