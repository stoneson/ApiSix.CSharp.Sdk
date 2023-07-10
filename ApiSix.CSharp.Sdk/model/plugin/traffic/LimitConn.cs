using Newtonsoft.Json;
using System;

namespace ApiSix.CSharp.model
{
    /// <summary>
    /// limit-conn 插件用于限制客户端对单个服务的并发请求数。
    /// 当客户端对路由的并发请求数达到限制时，可以返回自定义的状态码和响应信息。
    /// 
    /// https://apisix.apache.org/zh/docs/apisix/plugins/limit-conn/
    /// </summary>
    public class LimitConn : Plugin
    {
        /// <summary>
        /// 允许的最大并发请求数。超过 conn 的限制、但是低于 conn + burst 的请求，将被延迟处理。
        /// conn > 0
        /// </summary>
        [JsonProperty("conn")]
        public int conn { get; set; }
        /// <summary>
        /// 每秒允许被延迟处理的额外并发请求数。
        /// burst >= 0
        /// </summary>
        [JsonProperty("burst")]
        public int burst { get; set; }
        /// <summary>
        /// 默认的典型连接（或请求）的处理延迟时间。
        /// default_conn_delay > 0
        /// </summary>
        [JsonProperty("default_conn_delay")]
        public decimal defaultConnDelay { get; set; }
        /// <summary>
        /// 延迟时间的严格模式。当设置为 true 时，将会严格按照设置的 default_conn_delay 时间来进行延迟处理。
        /// </summary>
        [JsonProperty("only_use_default_delay")]
        public decimal onlyUseDefaultDelay { get; set; }
        /// <summary>
        /// 当请求数超过 conn + burst 阈值时，返回的 HTTP 状态码。
        /// </summary>
        [JsonProperty("rejected_code")]
        public int rejectedCode { get; set; } = 503;
        /// <summary>
        /// 当请求数超过 conn + burst 阈值时，返回的信息
        /// </summary>
        [JsonProperty("rejected_msg")]
        public String rejectedMsg { get; set; }
        /// <summary>
        /// 用来做请求计数的依据。如果 key_type 为 "var"，那么 key 会被当作变量名称，如 remote_addr 和 consumer_name；
        /// 如果 key_type 为 "var_combination"，那么 key 会当作变量组合，如 $remote_addr $consumer_name；
        /// 如果 key 的值为空，$remote_addr 会被作为默认 key。
        /// </summary>
        [JsonProperty("key")]
        public String key { get; set; }
        /// <summary>
        /// 关键字类型，支持：var（单变量）和 var_combination（组合变量）
        /// ["var", "var_combination"]
        /// </summary>
        [JsonProperty("key_type")]
        public String keyType { get; set; } = "var";
        /// <summary>
        /// 用于检索和增加限制的速率限制策略。可选的值有：local(计数器被以内存方式保存在节点本地，默认选项) 
        /// 和 redis(计数器保存在 Redis 服务节点上，从而可以跨节点共享结果，通常用它来完成全局限速)；
        /// 以及redis-cluster，跟 redis 功能一样，只是使用 redis 集群方式。
        /// </summary>
        [JsonProperty("policy")]
        public String policy { get; set; }
        /// <summary>
        /// 当设置为 true 时，启用插件降级并自动允许请求继续。
        /// </summary>
        [JsonProperty("allow_degradation")]
        public bool allowDegradation { get; set; }
        /// <summary>
        /// 是否在响应头中显示 X-RateLimit-Limit 和 X-RateLimit-Remaining （限制的总请求数和剩余还可以发送的请求数）
        /// </summary>
        [JsonProperty("show_limit_quota_header")]
        public bool showLimitQuotaHeader { get; set; }
    }
}
