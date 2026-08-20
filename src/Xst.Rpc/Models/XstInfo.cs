using Newtonsoft.Json;

namespace Xst.Rpc.Models
{
    public sealed class XstInfo
    {
        [JsonProperty("version")] public string Version { get; set; }
        [JsonProperty("buildversion")] public string BuildVersion { get; set; }
        [JsonProperty("protocolversion")] public int ProtocolVersion { get; set; }
        [JsonProperty("walletversion")] public int WalletVersion { get; set; }
        [JsonProperty("balance")] public decimal Balance { get; set; }
        [JsonProperty("newmint")] public decimal NewMint { get; set; }
        [JsonProperty("stake")] public decimal Stake { get; set; }
        [JsonProperty("blocks")] public int Blocks { get; set; }
        [JsonProperty("blockhash")] public string BlockHash { get; set; }
        [JsonProperty("moneysupply")] public decimal MoneySupply { get; set; }
        [JsonProperty("connections")] public int Connections { get; set; }
        [JsonProperty("proxy")] public string Proxy { get; set; }
        [JsonProperty("ip")] public string Ip { get; set; }
        [JsonProperty("difficulty")] public decimal Difficulty { get; set; }
        [JsonProperty("testnet")] public bool Testnet { get; set; }
        [JsonProperty("keypoololdest")] public long KeyPoolOldest { get; set; }
        [JsonProperty("keypoolsize")] public int KeyPoolSize { get; set; }
        [JsonProperty("paytxfee")] public decimal PayTxFee { get; set; }

        [JsonProperty("unlocked_until")] public long? UnlockedUntil { get; set; }

        [JsonProperty("errors")] public string Errors { get; set; }
    }
}
