using Newtonsoft.Json;
using System.Collections.Generic;

namespace ApiSix.Sharp.model
{
    public class Multi<T>
    {
        [JsonProperty("total")]
        public int total { get; set; }
        [JsonProperty("list")]
        public List<Item<T>> list { get; set; }

        public int code { get; set; } = 0;
        public string msg { get; set; }
    }
}
