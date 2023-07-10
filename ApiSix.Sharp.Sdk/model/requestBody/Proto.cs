using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ApiSix.Sharp.model
{
    /// <summary>
    /// .proto 文件
    /// </summary>
    public class Proto : BaseModel
    {
        /// <summary>
        /// .proto 文件的 id
        /// </summary>
        [JsonProperty("id")]
        public String id { get; set; }

        [JsonProperty("desc")]
        public String desc { get; set; }
        /// <summary>
        /// .proto 文件的内容
        /// </summary>
        [JsonProperty("content")]
        public String content { get; set; }
        /// <summary>
        /// epoch 时间戳，单位为秒，如果不指定则自动创建
        /// </summary>
        [JsonProperty("create_time")]
        public long? createTime { get; set; }
        /// <summary>
        /// epoch 时间戳，单位为秒，如果不指定则自动创建
        /// </summary>
        [JsonProperty("update_time")]
        public long? updateTime { get; set; }
    }
}
