using Newtonsoft.Json;
using System;

namespace ApiSix.Sharp.model
{
    /// <summary>
    /// limit-count 插件使用固定时间窗口算法，主要用于限制单个客户端在指定的时间范围内对服务的总请求数，并且会在 HTTP 响应头中返回剩余可以请求的个数。
    /// 该插件原理与 GitHub API 的速率限制类似。
    /// 
    /// https://apisix.apache.org/zh/docs/apisix/plugins/limit-count/
    /// </summary>
    public class LimitCount : Plugin
    {
        /// <summary>
        /// 每个客户端在指定时间窗口内的总请求数量阈值。
        /// </summary>
        [JsonProperty("count")]
        public int count { get; set; }
        /// <summary>
        /// 时间窗口的大小（以秒为单位）。超过该属性定义的时间，则会重新开始计数。
        /// time_window > 0
        /// </summary>
        [JsonProperty("time_window")]
        public int timeWindow { get; set; }
        /// <summary>
        /// 当请求超过阈值被拒绝时，返回的 HTTP 状态码。
        /// </summary>
        [JsonProperty("rejected_code")]
        public int rejectedCode { get; set; } = 503;
        /// <summary>
        /// 当请求超过阈值被拒绝时，返回的响应体
        /// </summary>
        [JsonProperty("rejected_msg")]
        public String rejectedMsg { get; set; }
        /// <summary>
        /// 用来做请求计数的依据。如果 key_type 为 constant，那么 key 会被当作常量；
        /// 如果 key_type 为 var，那么 key 会被当作变量；
        /// 如果 key_type 为 var_combination，那么 key 会被当作变量组合，如 $remote_addr $consumer_name，插件会同时受 $remote_addr 和 $consumer_name 两个变量的约束；
        /// 如果 key 的值为空，$remote_addr 会被作为默认 key。
        /// 有效值["remote_addr", "server_addr", "http_x_real_ip", "http_x_forwarded_for", "consumer_name"]
        /// </summary>
        [JsonProperty("key")]
        public String key { get; set; } = "remote_addr";
        /// <summary>
        /// 关键字类型，支持：var（单变量）和 var_combination（组合变量）
        /// 有效值["var", "var_combination", "constant"]
        /// </summary>
        [JsonProperty("key_type")]
        public String keyType { get; set; } = "var";
        /// <summary>
        /// 用于检索和增加限制计数的策略。当设置为 local 时，计数器被以内存方式保存在节点本地；
        /// 当设置为 redis 时，计数器保存在 Redis 服务节点上，从而可以跨节点共享结果，通常用它来完成全局限速；
        /// 当设置为 redis-cluster 时，使用 Redis 集群而不是单个实例。
        /// 有效值["local", "redis", "redis-cluster"]
        /// </summary>
        [JsonProperty("policy")]
        public String policy { get; set; } = "local";
        /// <summary>
        /// 当插件功能临时不可用时（例如 Redis 超时），当设置为 true 时，则表示可以允许插件降级并进行继续请求的操作。
        /// </summary>
        [JsonProperty("allow_degradation")]
        public bool allowDegradation { get; set; }
        /// <summary>
        /// 当设置为 true 时，在响应头中显示 X-RateLimit-Limit（限制的总请求数）和 X-RateLimit-Remaining（剩余还可以发送的请求数）字段。
        /// </summary>
        [JsonProperty("show_limit_quota_header")]
        public bool showLimitQuotaHeader { get; set; } = true;

        /// <summary>
        /// 配置相同 group 的路由将共享相同的限流计数器。
        /// </summary>
        public String group { get; set; }
        /// <summary>
        /// 当使用 redis 限速策略时，Redis 服务节点的地址。当 policy 属性设置为 redis 时必选。
        /// </summary>
        [JsonProperty("redis_host")]
        public String redisHost { get; set; }
        /// <summary>
        /// 当使用 redis 限速策略时，Redis 服务节点的端口。
        /// </summary>
        [JsonProperty("redis_port")]
        public int redisPort { get; set; }
        /// <summary>
        /// Redis 服务器的用户名。当 policy 设置为 redis 时使用。
        /// </summary>
        [JsonProperty("redis_username")]
        public String redisUsername { get; set; }
        /// <summary>
        /// 当使用 redis 或者 redis-cluster 限速策略时，Redis 服务节点的密码。
        /// </summary>
        [JsonProperty("redis_password")]
        public String redisPassword { get; set; }
        /// <summary>
        /// 当使用 redis 限速策略时，如果设置为 true，则使用 SSL 连接到 redis
        /// </summary>
        [JsonProperty("redis_ssl")]
        public bool redisSsl { get; set; }
        /// <summary>
        /// 当使用 redis 限速策略时，如果设置为 true，则验证服务器 SSL 证书的有效性, 具体请参考 tcpsock:sslhandshake.
        /// </summary>
        [JsonProperty("redis_ssl_verify")]
        public bool redisSslVerify { get; set; }
        /// <summary>
        /// 当使用 redis 限速策略时，Redis 服务节点中使用的 database，并且只针对非 Redis 集群模式（单实例模式或者提供单入口的 Redis 公有云服务）生效
        /// </summary>
        [JsonProperty("redis_database")]
        public int redisDatabase { get; set; }
        /// <summary>
        /// 当 policy 设置为 redis 或 redis-cluster 时，Redis 服务节点的超时时间（以毫秒为单位）。
        /// </summary>
        [JsonProperty("redis_timeout")]
        public int redisTimeout { get; set; }

        /// <summary>
        /// 当使用 redis-cluster 限速策略时，Redis 集群服务节点的地址列表（至少需要两个地址）。当 policy 属性设置为 redis-cluster 时必选。
        /// </summary>
        [JsonProperty("redis_cluster_nodes")]
        public System.Collections.Generic.List<String> redis_cluster_nodes { get; set; }
        /// <summary>
        /// 当使用 redis-cluster 限速策略时，Redis 集群服务节点的名称。当 policy 设置为 redis-cluster 时必选。
        /// </summary>
        [JsonProperty("redis_cluster_name")]
        public String redisClusterName { get; set; }
        /// <summary>
        /// 当使用 redis-cluster 限速策略时， 如果设置为 true，则使用 SSL 连接到 redis-cluster
        /// </summary>
        [JsonProperty("redis_cluster_ssl")]
        public bool redisClusterSsl { get; set; }
        /// <summary>
        /// 当使用 redis-cluster 限速策略时，如果设置为 true，则验证服务器 SSL 证书的有效性
        /// </summary>
        [JsonProperty("redis_cluster_ssl_verify")]
        public bool redisClusterSslVerify { get; set; }
    }
}
