using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ApiSix.Sharp.model
{
    public class Item<T>
    {
        [JsonProperty("value")]
        public T value { get; set; }

        [JsonProperty("key")]
        public string key { get; set; }

        [JsonProperty("createdIndex")]
        public int createdIndex { get; set; }

        [JsonProperty("modifiedIndex")]
        public int modifiedIndex { get; set; }

        public int code { get; set; } = 0;
        public string msg { get; set; }
    }
}
