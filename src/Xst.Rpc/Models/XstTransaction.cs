using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Xst.Rpc.Models
{
    public static class XstTransactionCategory
    {
        public const string Send = "send";
        public const string Receive = "receive";
        public const string Generate = "generate";
        public const string Immature = "immature";
        public const string Orphan = "orphan";
        public const string Move = "move";
    }

    public sealed class XstTransaction
    {
        [JsonProperty("txid")] public string TxId { get; set; }

        [JsonProperty("amount")] public decimal Amount { get; set; }

        [JsonProperty("fee")] public decimal? Fee { get; set; }

        [JsonProperty("confirmations")] public int Confirmations { get; set; }
        [JsonProperty("generated")] public bool Generated { get; set; }
        [JsonProperty("blockhash")] public string BlockHash { get; set; }
        [JsonProperty("blockindex")] public int? BlockIndex { get; set; }

        [JsonProperty("blocktime")] public long? BlockTime { get; set; }

        [JsonProperty("time")] public long Time { get; set; }

        [JsonProperty("timereceived")] public long TimeReceived { get; set; }

        [JsonProperty("datetime")] public string DateTimeText { get; set; }
        [JsonProperty("comment")] public string Comment { get; set; }
        [JsonProperty("to")] public string To { get; set; }

        [JsonProperty("details")] public List<XstTransactionDetail> Details { get; set; }

        [JsonProperty("version")] public int Version { get; set; }

        [JsonProperty("locktime")] public long LockTime { get; set; }

        [JsonProperty("vin")] public JArray Vin { get; set; }

        [JsonProperty("vout")] public JArray Vout { get; set; }
    }

    public sealed class XstTransactionDetail
    {
        [JsonProperty("account")] public string Account { get; set; }
        [JsonProperty("address")] public string Address { get; set; }
        [JsonProperty("category")] public string Category { get; set; }
        [JsonProperty("amount")] public decimal Amount { get; set; }
        [JsonProperty("fee")] public decimal? Fee { get; set; }
    }

    public sealed class XstTransactionSummary
    {
        [JsonProperty("account")] public string Account { get; set; }
        [JsonProperty("address")] public string Address { get; set; }

        [JsonProperty("category")] public string Category { get; set; }

        [JsonProperty("amount")] public decimal Amount { get; set; }

        [JsonProperty("fee")] public decimal? Fee { get; set; }
        [JsonProperty("confirmations")] public int Confirmations { get; set; }
        [JsonProperty("generated")] public bool Generated { get; set; }
        [JsonProperty("blockhash")] public string BlockHash { get; set; }
        [JsonProperty("blockindex")] public int? BlockIndex { get; set; }
        [JsonProperty("blocktime")] public long? BlockTime { get; set; }
        [JsonProperty("txid")] public string TxId { get; set; }
        [JsonProperty("time")] public long Time { get; set; }
        [JsonProperty("timereceived")] public long TimeReceived { get; set; }
        [JsonProperty("datetime")] public string DateTimeText { get; set; }

        [JsonProperty("otheraccount")] public string OtherAccount { get; set; }

        [JsonProperty("comment")] public string Comment { get; set; }
        [JsonProperty("to")] public string To { get; set; }
    }
}
