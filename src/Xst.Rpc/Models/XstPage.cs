using System.Collections.Generic;
using Newtonsoft.Json;

namespace Xst.Rpc.Models
{
    public sealed class XstPage<T>
    {
        [JsonProperty("total")] public long Total { get; set; }

        [JsonProperty("page")] public int Page { get; set; }

        [JsonProperty("per_page")] public int PerPage { get; set; }

        [JsonProperty("last_page")] public int LastPage { get; set; }

        [JsonProperty("data")] public List<T> Data { get; set; }

        [JsonIgnore] public bool HasMore => Page < LastPage;
    }
}
