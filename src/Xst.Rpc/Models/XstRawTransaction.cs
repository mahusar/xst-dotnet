using Newtonsoft.Json;

namespace Xst.Rpc.Models
{
    public sealed class XstOutPoint
    {
        [JsonProperty("txid")] public string TxId { get; set; }
        [JsonProperty("vout")] public int Vout { get; set; }

        public XstOutPoint()
        {
        }

        public XstOutPoint(string txid, int vout)
        {
            TxId = txid;
            Vout = vout;
        }
    }

    public sealed class XstPreviousOutput
    {
        [JsonProperty("txid")] public string TxId { get; set; }
        [JsonProperty("vout")] public int Vout { get; set; }
        [JsonProperty("scriptPubKey")] public string ScriptPubKey { get; set; }
    }

    public sealed class XstSignedTransaction
    {
        [JsonProperty("hex")] public string Hex { get; set; }

        [JsonProperty("complete")] public bool Complete { get; set; }
    }
}
