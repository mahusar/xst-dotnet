using Newtonsoft.Json;

namespace Xst.Rpc.Models
{
    public sealed class XstReceivedByAddress
    {
        [JsonProperty("address")] public string Address { get; set; }
        [JsonProperty("account")] public string Account { get; set; }
        [JsonProperty("amount")] public decimal Amount { get; set; }
        [JsonProperty("confirmations")] public int Confirmations { get; set; }
    }
}
