using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ApiSix.CSharp.model
{
    /// <summary>
    /// uri-blocker 插件通过指定一系列 block_rules 来拦截用户请求。
    /// 
    /// https://apisix.apache.org/zh/docs/apisix/plugins/uri-blocker/
    /// </summary>
    public class uriBlocker  : Plugin
    {
        /// <summary>
        /// 正则过滤数组。它们都是正则规则，如果当前请求 URI 命中其中任何一个，
        /// 则将响应代码设置为 rejected_code 以退出当前用户请求。
        /// 例如：["root.exe", "root.m+"]。
        /// </summary>
        [JsonProperty("block_rules")]
        public List<String> blockRules { get; set; }
        /// <summary>
        /// 当请求 URI 命中 block_rules 中的任何一个时，将返回的 HTTP 状态代码。
        /// </summary>
        [JsonProperty("rejected_code")]
        public int rejectedCode { get; set; } = 403;
        /// <summary>
        /// 当请求 URI 命中 block_rules 中的任何一个时，将返回的 HTTP 响应体。
        /// </summary>
        [JsonProperty("rejected_msg")]
        public String rejectedMsg { get; set; }

        /// <summary>
        /// 是否忽略大小写。当设置为 true 时，在匹配请求 URI 时将忽略大小写。
        /// </summary>
        [JsonProperty("case_insensitive")]
        public bool caseInsensitive { get; set; }
    }
}
