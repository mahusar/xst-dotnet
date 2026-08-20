using Newtonsoft.Json;

namespace Xst.Rpc.Models
{
    public sealed class XstUnspentOutput
    {
        [JsonProperty("txid")] public string TxId { get; set; }
        [JsonProperty("vout")] public int Vout { get; set; }

        [JsonProperty("scriptPubKey")] public string ScriptPubKey { get; set; }

        [JsonProperty("amount")] public decimal Amount { get; set; }
        [JsonProperty("confirmations")] public int Confirmations { get; set; }
        [JsonProperty("spendable")] public bool Spendable { get; set; }
    }
}
