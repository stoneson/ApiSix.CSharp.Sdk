using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ApiSix.Sharp.model
{
    /// <summary>
    /// limit-req 插件使用漏桶算法限制单个客户端对服务的请求速率
    /// 
    /// https://apisix.apache.org/zh/docs/apisix/plugins/limit-req/
    /// </summary>
    public class LimitReq : Plugin
    {
        /// <summary>
        /// 指定的请求速率（以秒为单位），请求速率超过 rate 但没有超过（rate + burst）的请求会被延时处理。
        /// </summary>
        [JsonProperty("rate")]
        public int rate { get; set; }
        /// <summary>
        /// 请求速率超过（rate + burst）的请求会被直接拒绝。
        /// </summary>
        [JsonProperty("burst")]
        public int burst { get; set; }

        /// <summary>
        /// 用来做请求计数的依据，当前接受的 key 有：remote_addr（客户端 IP 地址），server_addr（服务端 IP 地址）,
        /// 请求头中的 X-Forwarded-For 或 X-Real-IP，consumer_name（Consumer 的 username）。
        /// 有效值["remote_addr", "server_addr", "http_x_real_ip", "http_x_forwarded_for", "consumer_name"]
        /// </summary>
        [JsonProperty("key")]
        public String key { get; set; } = "remote_addr";
        /// <summary>
        /// 要使用的用户指定 key 的类型。
        /// 有效值["var", "var_combination"]
        /// </summary>
        [JsonProperty("key_type")]
        public String keyType { get; set; } = "var";

        /// <summary>
        /// 当超过阈值的请求被拒绝时，返回的 HTTP 状态码
        /// </summary>
        [JsonProperty("rejected_code")]
        public int rejectedCode { get; set; } = 503;
        /// <summary>
        /// 当请求超过阈值被拒绝时，返回的响应体
        /// </summary>
        [JsonProperty("rejected_msg")]
        public String rejectedMsg { get; set; }

        /// <summary>
        /// 当设置为 true 时，请求速率超过 rate 但没有超过（rate + burst）的请求不会加上延迟；
        /// 当设置为 false，则会加上延迟
        /// </summary>
        [JsonProperty("nodelay")]
        public bool nodelay { get; set; }
        /// <summary>
        /// 当设置为 true 时，如果限速插件功能临时不可用，将会自动允许请求继续。
        /// </summary>
        [JsonProperty("allow_degradation")]
        public bool allowDegradation { get; set; }
    }
}
