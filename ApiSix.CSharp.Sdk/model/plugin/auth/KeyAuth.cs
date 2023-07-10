using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ApiSix.CSharp.model
{
    /// <summary>
    /// key-auth 插件用于向 Route 或 Service 添加身份验证密钥（API key）。
    /// 它需要与 Consumer 一起配合才能工作，通过 Consumer 将其密钥添加到查询字符串参数或标头中以验证其请求。
    /// https://apisix.apache.org/zh/docs/apisix/plugins/key-auth/
    /// </summary>
    public class KeyAuth : Plugin
    {
        /// <summary>
        /// 不同的 Consumer 应有不同的 key，它应当是唯一的。如果多个 Consumer 使用了相同的 key，
        /// 将会出现请求匹配异常。该字段支持使用 APISIX Secret 资源，将值保存在 Secret Manager 中。
        /// </summary>
        [JsonProperty("key")]
        public String key { get; set; }
        public bool disable { get; set; }

        [JsonProperty("_meta")]
        public Dictionary<String, object> meta { get; set; }
    }

}
