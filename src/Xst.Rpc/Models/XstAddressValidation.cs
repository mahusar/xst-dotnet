using Newtonsoft.Json;

namespace Xst.Rpc.Models
{
    public sealed class XstAddressValidation
    {
        [JsonProperty("isvalid")] public bool IsValid { get; set; }
        [JsonProperty("address")] public string Address { get; set; }
        [JsonProperty("ismine")] public bool IsMine { get; set; }
        [JsonProperty("watchonly")] public bool WatchOnly { get; set; }
        [JsonProperty("isscript")] public bool IsScript { get; set; }

        [JsonProperty("pubkey")] public string PubKey { get; set; }

        [JsonProperty("iscompressed")] public bool? IsCompressed { get; set; }
        [JsonProperty("account")] public string Account { get; set; }
    }
}
