using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ApiSix.CSharp.model
{
    /// <summary>
    /// Global Rule 可以设置全局运行的插件，设置为全局规则的插件将在所有路由级别的插件之前优先运行
    /// </summary>
    public class GlobalRule : BaseModel
    {
        /// <summary>
        /// Plugin 配置
        /// </summary>
        [JsonProperty("plugins")]
        public Dictionary<String, Plugin> plugins { get; set; }
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
