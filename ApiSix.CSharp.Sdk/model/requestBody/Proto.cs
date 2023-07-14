using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ApiSix.CSharp.model
{
    /// <summary>
    /// .proto 文件
    /// </summary>
    public class Proto : BaseBody
    {
        [JsonProperty("desc")]
        public String desc { get; set; }
        /// <summary>
        /// .proto 文件的内容
        /// </summary>
        [JsonProperty("content")]
        public String content { get; set; }
       
    }
}
