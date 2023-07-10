using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiSix.Sharp.model
{
    /// <summary>
    /// 上游
    /// Upstream 是虚拟主机抽象，对给定的多个服务节点按照配置规则进行负载均衡。
    /// Upstream 的地址信息可以直接配置到 Route（或 Service) 上，当 Upstream 有重复时，需要用“引用”方式避免重复。
    /// </summary>
    public class Upstream : BaseModel
    {
        [JsonProperty("id")]
        public String id { get; set; }
        /// <summary>
        /// 标识上游服务名称、使用场景等
        /// </summary>
        [JsonProperty("name")]
        public String name { get; set; }
        /// <summary>
        /// 上游服务描述、使用场景等。
        /// </summary>
        [JsonProperty("desc")]
        public String desc { get; set; }
        /// <summary>
        /// 负载均衡算法，默认值是roundrobin。
        /// </summary>
        [JsonProperty("type")]
        public String type { get; set; } = "roundrobin";
        /// <summary>
        /// 哈希表或数组。当它是哈希表时，内部元素的 key 是上游机器地址列表，
        /// 格式为地址 +（可选的）端口，其中地址部分可以是 IP 也可以是域名，比如 192.168.1.100:80、foo.com:80等。对于哈希表的情况，
        /// 如果 key 是 IPv6 地址加端口，则必须用中括号将 IPv6 地址括起来。
        /// value 则是节点的权重。当它是数组时，数组中每个元素都是一个哈希表，其中包含 host、weight 以及可选的 port、priority。
        /// nodes 可以为空，这通常用作占位符。客户端命中这样的上游会返回 502。
        /// service_name 二选一
        /// </summary>
        [JsonProperty("nodes")]
        public Dictionary<String, int> nodes { get; set; }
        /// <summary>
        /// 服务发现时使用的服务名，请参考 集成服务发现注册中心。
        /// 与 nodes 二选一
        /// </summary>
        [JsonProperty("service_name")]
        public string serviceName { get; set; }
        /// <summary>
        /// 服务发现类型，请参考 集成服务发现注册中心。如：eureka
        /// 与 service_name 配合使用
        /// </summary>
        [JsonProperty("discovery_type")]
        public string discoveryType { get; set; }
        /// <summary>
        /// 该选项只有类型是 chash 才有效。根据 key 来查找对应的节点 id，相同的 key 在同一个对象中，则返回相同 id。
        /// 目前支持的 NGINX 内置变量有 uri, server_name, server_addr, request_uri, remote_port, remote_addr, query_string, host, hostname, arg_***，
        /// 其中 arg_*** 是来自 URL 的请求参数，详细信息请参考 NGINX 变量列表。
        /// </summary>
        public string key { get; set; }
        /// <summary>
        /// 跟上游通信时使用的 scheme。对于 7 层代理，可选值为 [http, https, grpc, grpcs]。
        /// 对于 4 层代理，可选值为 [tcp, udp, tls]。默认值为 http，详细信息请参考下文。
        /// </summary>
        [JsonProperty("scheme")]
        public String scheme { get; set; }
        /// <summary>
        /// hash_on 支持的类型有 vars（NGINX 内置变量），header（自定义 header），cookie，consumer，默认值为 vars。
        /// </summary>
        [JsonProperty("hash_on")]
        public String hashOn { get; set; }
        /// <summary>
        /// 请求发给上游时的 host 设置选型。 [pass，node，rewrite] 之一，
        /// 默认是 pass。pass: 将客户端的 host 透传给上游；
        /// node: 使用 upstream node 中配置的 host； rewrite: 使用配置项 upstream_host 的值。
        /// </summary>
        [JsonProperty("pass_host")]
        public String passHost { get; set; }
        /// <summary>
        /// 指定上游请求的 host，只在 pass_host 配置为 rewrite 时有效。
        /// </summary>
        [JsonProperty("upstream_host")]
        public String upstreamHost { get; set; }
        /// <summary>
        /// 使用 NGINX 重试机制将请求传递给下一个上游，
        /// 默认启用重试机制且次数为后端可用的节点数量。如果指定了具体重试次数，它将覆盖默认值。当设置为 0 时，表示不启用重试机制
        /// </summary>
        [JsonProperty("retries")]
        public int? retries { get; set; }
        /// <summary>
        /// 限制是否继续重试的时间，若之前的请求和重试请求花费太多时间就不再继续重试。当设置为 0 时，表示不启用重试超时机制。
        /// </summary>
        [JsonProperty("retry_timeout")]
        public decimal? retryTimeout { get; set; }
        /// <summary>
        /// 设置连接、发送消息、接收消息的超时时间，以秒为单位
        /// </summary>
        [JsonProperty("timeout")]
        public timeoutInfo timeout { get; set; }
        /// <summary>
        /// 配置健康检查的参数，详细信息请参考 health-check。
        /// </summary>
        [JsonProperty("checks")]
        public checksInfo checks { get; set; }

        [JsonProperty("keepalive_pool")]
        public keepalivePoolInfo keepalivePool { get; set; }

        [JsonProperty("k8s_deployment_info")]
        public K8sDeploymentInfo k8sDeploymentInfo { get; set; }
        /// <summary>
        /// 标识附加属性的键值对
        /// {"version":"v2","build":"16","env":"production"}
        /// </summary>
        [JsonProperty("labels")]
        public Dictionary<String, String> labels { get; set; }
        [JsonProperty("create_time")]
        public long? createTime { get; set; }

        [JsonProperty("update_time")]
        public long? updateTime { get; set; }

        [JsonProperty("tls")]
        public tlsInfo tls { get; set; }
    }
    /**
     * type 详细信息如下：
        roundrobin: 带权重的 Round Robin。
        chash: 一致性哈希。
        ewma: 选择延迟最小的节点，请参考 EWMA_chart。
        least_conn: 选择 (active_conn + 1) / weight 最小的节点。此处的 active connection 概念跟 NGINX 的相同，它是当前正在被请求使用的连接。
        用户自定义的 balancer，需要可以通过 require("apisix.balancer.your_balancer") 来加载。
    hash_on 详细信息如下：
        设为 vars 时，key 为必传参数，目前支持的 NGINX 内置变量有 uri, server_name, server_addr, request_uri, remote_port, remote_addr, query_string, host, hostname, arg_***，其中 arg_*** 是来自 URL 的请求参数。详细信息请参考 NGINX 变量列表。
        设为 header 时，key 为必传参数，其值为自定义的 Header name，即 "http_key"。
        设为 cookie 时，key 为必传参数，其值为自定义的 cookie name，即 "cookie_key"。请注意 cookie name 是区分大小写字母的。例如：cookie_x_foo 与 cookie_X_Foo 表示不同的 cookie。
        设为 consumer 时，key 不需要设置。此时哈希算法采用的 key 为认证通过的 consumer_name。
        如果指定的 hash_on 和 key 获取不到值时，使用默认值：remote_addr。
    以下特性需要 APISIX 运行于 APISIX-Base：
        scheme 可以设置成 tls，表示 TLS over TCP。
        tls.client_cert/key 可以用来跟上游进行 mTLS 通信。他们的格式和 SSL 对象的 cert 和 key 一样。
        tls.client_cert_id 可以用来指定引用的 SSL 对象。只有当 SSL 对象的 type 字段为 client 时才能被引用，否则请求会被 APISIX 拒绝。另外，SSL 对象中只有 cert 和 key 会被使用。
        keepalive_pool 允许 Upstream 有自己单独的连接池。它下属的字段，比如 requests，可以用于配置上游连接保持的参数。
     * 
     * 
     * Upstream 对象 JSON 配置示例：
        {
            "id": "1",                  # id
            "retries": 1,               # 请求重试次数
            "timeout": {                # 设置连接、发送消息、接收消息的超时时间，每项都为 15 秒
                "connect":15,
                "send":15,
                "read":15
            },
            "nodes": {"host:80": 100},  # 上游机器地址列表，格式为`地址 + 端口`
                                        # 等价于 "nodes": [ {"host":"host", "port":80, "weight": 100} ],
            "type":"roundrobin",
            "checks": {},               # 配置健康检查的参数
            "hash_on": "",
            "key": "",
            "name": "upstream-xxx",     # upstream 名称
            "desc": "hello world",      # upstream 描述
            "scheme": "http"            # 跟上游通信时使用的 scheme，默认是 `http`
        }


    创建 Upstream：
        curl http://127.0.0.1:9180/apisix/admin/upstreams/100  \
        -H 'X-API-KEY: edd1c9f034335f136f87ad84b625c8f1' -i -X PUT -d '
        {
            "type":"roundrobin",
            "nodes":{
                "127.0.0.1:1980": 1
            }
        }'

    在 Upstream 中添加一个节点：
        curl http://127.0.0.1:9180/apisix/admin/upstreams/100 \
        -H 'X-API-KEY: edd1c9f034335f136f87ad84b625c8f1' -X PATCH -i -d '
        {
            "nodes": {
                "127.0.0.1:1981": 1
            }
        }'
     * **/
    /// <summary>
    /// 超时时间对象
    /// </summary>
    public class timeoutInfo
    {
        /// <summary>
        /// 发送消息的超时时间
        /// </summary>
        [JsonProperty("send")]
        public int? send { get; set; }
        /// <summary>
        /// 接收消息的超时时间
        /// </summary>
        [JsonProperty("read")]
        public int? read { get; set; }
        /// <summary>
        /// 连接的超时时间
        /// </summary>
        [JsonProperty("connect")]
        public int? connect { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class keepalivePoolInfo
    {
        /// <summary>
        /// 	动态设置 keepalive_timeout 指令，详细信息请参考下文。
        /// </summary>
        [JsonProperty("idle_timeout")]
        public int? idleTimeout { get; set; } = 60;
        /// <summary>
        /// 动态设置 keepalive_requests 指令，详细信息请参考下文
        /// </summary>
        [JsonProperty("requests")]
        public int? requests { get; set; } = 1000;
        /// <summary>
        /// 动态设置 keepalive 指令，详细信息请参考下文。
        /// </summary>
        [JsonProperty("size")]
        public int? size { get; set; }
    }
    /// <summary>
    /// 证书
    /// </summary>
    public class tlsInfo
    {
        /// <summary>
        /// 设置跟上游通信时的客户端证书，详细信息请参考下文
        /// 不能和 tls.client_cert_id 一起使用
        /// </summary>
        [JsonProperty("client_cert")]
        public string clientCert { get; set; }
        /// <summary>
        /// 设置跟上游通信时的客户端私钥，详细信息请参考下文。
        /// 不能和 tls.client_cert_id 一起使用
        /// </summary>
        [JsonProperty("client_key")]
        public string clientKey { get; set; }
        /// <summary>
        /// 设置引用的 SSL id，详见 SSL。
        /// 不能和 tls.client_cert、tls.client_key 一起使用
        /// </summary>
        [JsonProperty("client_cert_id")]
        public string clientCertId { get; set; }
    }
    public class checksInfo
    {
        [JsonProperty("active")]
        public checksActiveInfo active { get; set; }
    }
    public class checksActiveInfo
    {
        [JsonProperty("port")]
        public int? port { get; set; }

        [JsonProperty("timeout")]
        public int? timeout { get; set; }

        [JsonProperty("host")]
        public String host { get; set; }

        [JsonProperty("http_path")]
        public String httpPath { get; set; }

        [JsonProperty("type")]
        public String type { get; set; }

        [JsonProperty("concurrency")]
        public int? concurrency { get; set; }

        [JsonProperty("healthy")]
        public checksActiveHealthyInfo healthy { get; set; }

        [JsonProperty("unhealthy")]
        public checksActiveUnhealthyInfo unhealthy { get; set; }
    }

    public class checksActiveHealthyInfo
    {
        [JsonProperty("interval")]
        public int? interval { get; set; } = 1;

        [JsonProperty("successes")]
        public int? successes { get; set; } = 2;

        [JsonProperty("http_statuses")]
        public List<int> httpStatuses { get; set; }// = new List<int>();// { 200, 302 };
    }
    public class checksActiveUnhealthyInfo
    {
        [JsonProperty("interval")]
        public int? interval { get; set; } = 1;

        [JsonProperty("timeouts")]
        public int? timeouts { get; set; } = 3;
        [JsonProperty("http_failures")]
        public int? httpFailures { get; set; } = 5;

        [JsonProperty("tcp_failures")]
        public int? tcpFailures { get; set; } = 3;

        [JsonProperty("http_statuses")]
        public List<int> httpStatuses { get; set; }// = new List<int>();// { 429, 404, 500, 501, 502, 503, 504, 505 };
    }

    public class K8sDeploymentInfo
    {
        [JsonProperty("namespace")]
        public string Namespace { get; set; }


        [JsonProperty("deploy_name")]
        public string deployName { get; set; }

        [JsonProperty("service_name")]
        public string serviceName { get; set; }

        [JsonProperty("port")]
        public int port { get; set; }

        [JsonProperty("backend_type")]
        public string backendType { get; set; }
    }
}
