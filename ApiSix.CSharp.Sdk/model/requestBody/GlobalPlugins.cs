using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ApiSix.CSharp.model
{
    public class GlobalPlugins : BaseBody
    {
        /// <summary>
        /// Plugin 配置
        /// </summary>
        [JsonProperty("plugins")]
        public Dictionary<String, Plugin> plugins { get; set; }
    }
}
