using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Xst.Rpc.Models
{
    public sealed class XstBlock
    {
        [JsonProperty("hash")] public string Hash { get; set; }
        [JsonProperty("confirmations")] public long Confirmations { get; set; }
        [JsonProperty("isinmainchain")] public bool? IsInMainChain { get; set; }

        [JsonProperty("depth")] public long? Depth { get; set; }

        [JsonProperty("size")] public long Size { get; set; }
        [JsonProperty("height")] public long Height { get; set; }
        [JsonProperty("version")] public int Version { get; set; }
        [JsonProperty("merkleroot")] public string MerkleRoot { get; set; }

        [JsonProperty("staker_id")] public long? StakerId { get; set; }

        [JsonProperty("staker_alias")] public string StakerAlias { get; set; }
        [JsonProperty("block_reward")] public decimal? BlockReward { get; set; }
        [JsonProperty("moneysupply")] public decimal? MoneySupply { get; set; }
        [JsonProperty("mint")] public decimal? Mint { get; set; }
        [JsonProperty("pico_power")] public decimal? PicoPower { get; set; }

        [JsonProperty("time")] public long Time { get; set; }

        [JsonProperty("datetime")] public string DateTimeText { get; set; }
        [JsonProperty("nonce")] public ulong Nonce { get; set; }
        [JsonProperty("bits")] public string Bits { get; set; }
        [JsonProperty("difficulty")] public decimal Difficulty { get; set; }
        [JsonProperty("previousblockhash")] public string PreviousBlockHash { get; set; }
        [JsonProperty("nextblockhash")] public string NextBlockHash { get; set; }
        [JsonProperty("flags")] public string Flags { get; set; }
        [JsonProperty("proofhash")] public string ProofHash { get; set; }
        [JsonProperty("modifier")] public string Modifier { get; set; }

        [JsonProperty("trust")] public string Trust { get; set; }

        [JsonProperty("signature")] public string Signature { get; set; }

        [JsonProperty("tx")] public JArray Transactions { get; set; }
    }

    public sealed class XstStakerAuthority
    {
        [JsonProperty("address")] public string Address { get; set; }
        [JsonProperty("pubkey")] public string PubKey { get; set; }
    }

    public sealed class XstStakerAuthorities
    {
        [JsonProperty("owner")] public XstStakerAuthority Owner { get; set; }
        [JsonProperty("manager")] public XstStakerAuthority Manager { get; set; }
        [JsonProperty("delegate")] public XstStakerAuthority Delegate { get; set; }
        [JsonProperty("controller")] public XstStakerAuthority Controller { get; set; }
    }

    public sealed class XstPeer
    {
        [JsonProperty("addr")] public string Address { get; set; }
        [JsonProperty("services")] public string Services { get; set; }
        [JsonProperty("lastsend")] public long LastSend { get; set; }
        [JsonProperty("lastrecv")] public long LastReceive { get; set; }
        [JsonProperty("conntime")] public long ConnectionTime { get; set; }
        [JsonProperty("version")] public int Version { get; set; }
        [JsonProperty("subver")] public string SubVersion { get; set; }
        [JsonProperty("inbound")] public bool Inbound { get; set; }

        [JsonProperty("releasetime")] public long ReleaseTime { get; set; }

        [JsonProperty("startingheight")] public long StartingHeight { get; set; }
        [JsonProperty("banscore")] public int? BanScore { get; set; }
    }
}
