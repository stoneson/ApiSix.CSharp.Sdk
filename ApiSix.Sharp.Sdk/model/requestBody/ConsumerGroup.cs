using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ApiSix.Sharp.model
{
    /// <summary>
    /// 你可以使用该资源配置一组可以在 Consumer 间复用的插件。
    /// </summary>
    public class ConsumerGroup : BaseModel
    { /// <summary>
      /// 服务ID
      /// </summary>
        [JsonProperty("id")]
        public String id { get; set; }
        /// <summary>
        /// 服务名称
        /// </summary>
        [JsonProperty("name")]
        public String name { get; set; }
        /// <summary>
        /// 标识描述、使用场景等。
        /// </summary>
        [JsonProperty("desc")]
        public String desc { get; set; }
        /// <summary>
        /// Plugin 配置
        /// </summary>
        [JsonProperty("plugins")]
        public Dictionary<String, Plugin> plugins { get; set; }
        /// <summary>
        /// 标识附加属性的键值对
        /// {"version":"v2","build":"16","env":"production"}
        /// </summary>
        [JsonProperty("labels")]
        public Dictionary<String, String> labels { get; set; }
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
