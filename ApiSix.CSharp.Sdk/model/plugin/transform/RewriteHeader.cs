using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ApiSix.CSharp.model
{
    public class RewriteHeader
    {
        /// <summary>
        /// 改写请求头，如果请求头不存在，则会添加这个请求头。格式为 {"name": "value", ...}。
        /// 这个值能够以 $var 的格式包含 NGINX 变量，比如 $remote_addr $balancer_ip。
        /// 也支持以变量的形式引用 regex_uri 的匹配结果，比如 $1-$2-$3。
        /// </summary>
        [JsonProperty("set")]
        public Dictionary<String, String> set { get; set; }
        /// <summary>
        /// 添加新的请求头，如果头已经存在，会追加到末尾。格式为 {"name": "value", ...}。
        /// 这个值能够以 $var 的格式包含 NGINX 变量，比如 $remote_addr $balancer_ip。
        /// 也支持以变量的形式引用 regex_uri 的匹配结果，比如 $1-$2-$3。
        /// </summary>
        [JsonProperty("add")]
        public Dictionary<String, String> add { get; set; }
        /// <summary>
        /// 移除响应头。格式为 ["name", ...]。
        /// </summary>
        [JsonProperty("remove")]
        public List<String> remove { get; set; }
    }

}
