using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;

namespace ApiSix.CSharp.model
{
    /// <summary>
    /// Route 也称之为路由，可以通过定义一些规则来匹配客户端的请求，然后根据匹配结果加载并执行相应的插件，
    /// 并把请求转发给到指定 Upstream（上游）。
    /// 
    /// 注意:
    /// 对于同一类参数比如 uri与 uris，upstream 与 upstream_id，host 与 hosts，remote_addr 与 remote_addrs 等，是不能同时存在，二者只能选择其一。如果同时启用，则会出现异常。
    /// 在 vars 中，当获取 Cookie 的值时，Cookie name 是区分大小写字母的。例如：var = cookie_x_foo 与 var = cookie_X_Foo 表示不同的 cookie。
    /// </summary>
    public class Route : BaseBody
    {
        /// <summary>
        /// 路由名称最大长度应仅为 100
        /// </summary>
        [JsonProperty("name")]
        public String name { get; set; }
        /// <summary>
        /// HTTP 请求路径，如 /foo/index.html，支持请求路径前缀 /foo/*。/* 代表所有路径
        /// 与 uris 二选一
        /// </summary>
        [JsonProperty("uri")]
        public String uri { get; set; }
        /// <summary>
        /// 非空数组形式，可以匹配多个 uri。示例值 ["/hello", "/world"]
        /// 与 uri 二选一
        /// </summary>
        [JsonProperty("uris")]
        public List<String> uris { get; set; }
        /// <summary>
        /// Plugin 配置
        /// </summary>
        [JsonProperty("plugins")]
        public Dictionary<String, Plugin> plugins { get; set; }
        /// <summary>
        /// Upstream 配置
        /// </summary>
        [JsonProperty("upstream")]
        public Upstream upstream { get; set; }
        /// <summary>
        /// 需要使用的 Upstream id
        /// </summary>
        [JsonProperty("upstream_id")]
        public String upstreamId { get; set; }
        /// <summary>
        /// 需要绑定的 Service id
        /// </summary>
        [JsonProperty("service_id")]
        public String ServiceId { get; set; }

        //[JsonProperty("service_protocol")]
        //public String serviceProtocol { get; set; }
        /// <summary>
        /// 路由描述信息
        /// </summary>
        [JsonProperty("desc")]
        public String desc { get; set; }
        /// <summary>
        /// 当前请求域名，比如 foo.com；也支持泛域名，比如 *.foo.com。
        /// 与 hosts 二选一
        /// </summary>
        [JsonProperty("host")]
        public String host { get; set; }
        /// <summary>
        /// 非空列表形态的 host，表示允许有多个不同 host，匹配其中任意一个即可
        /// 与 host 二选一
        /// </summary>
        [JsonProperty("hosts")]
        public List<String> hosts { get; set; }
        /// <summary>
        /// 客户端请求的 IP 地址。支持 IPv4 地址，如：192.168.1.101 以及 CIDR 格式的支持 192.168.1.0/24；
        /// 支持 IPv6 地址匹配，如 ::1，fe80::1，fe80::1/64 等。
        /// 
        /// remote_addrs 二选一
        /// </summary>
        [JsonProperty("remote_addr")]
        public String remoteAddr { get; set; }
        /// <summary>
        /// 非空列表形态的 remote_addr，表示允许有多个不同 IP 地址，符合其中任意一个即可。["127.0.0.1", "192.0.0.0/8", "::1"]
        /// 
        /// remote_addr 二选一
        /// </summary>
        [JsonProperty("remote_addrs")]
        public List<String> remoteAddrs { get; set; }
        /// <summary>
        /// HTTP 方法
        /// 如果为空或没有该选项，则表示没有任何 method 限制。
        /// 你也可以配置一个或多个的组合：GET，POST，PUT，DELETE，PATCH，HEAD，OPTIONS，CONNECT，TRACE，PURGE。
        /// </summary>
        [JsonProperty("methods")]
        public List<String> methods { get; set; }
        /// <summary>
        /// 优先级
        /// 如果不同路由包含相同的 uri，则根据属性 priority 确定哪个 route 被优先匹配，值越大优先级越高，默认值为 0。
        /// </summary>
        [JsonProperty("priority")]
        public int priority { get; set; } = 0;
        /// <summary>
        /// 当设置为 1 时，启用该路由，默认值为 1。
        /// </summary>
        [JsonProperty("status")]
        public int status { get; set; }
        /// <summary>
        /// 由一个或多个[var, operator, val]元素组成的列表，类似 [[var, operator, val], [var, operator, val], ...]]。
        /// 例如：["arg_name", "==", "json"] 则表示当前请求参数 name 是 json。
        /// 此处 var 与 NGINX 内部自身变量命名是保持一致的，所以也可以使用 request_uri、host 等。
        /// 更多细节请参考 lua-resty-expr。
        /// 
        /// [["arg_name", "==", "json"], ["arg_age", ">", 18]]
        /// </summary>
        [JsonProperty("vars")]
        public List<Tuple<string,string,string>> vars { get; set; }
        /// <summary>
        /// 用户自定义的过滤函数。可以使用它来实现特殊场景的匹配要求实现。
        /// 该函数默认接受一个名为 vars 的输入参数，可以用它来获取 NGINX 变量。
        /// function(vars) return vars["arg_name"] == "json" end
        /// </summary>
        [JsonProperty("filter_func")]
        public String filterFunc { get; set; }
        /// <summary>
        /// 标识附加属性的键值对
        /// {"version":"v2","build":"16","env":"production"}
        /// </summary>
        [JsonProperty("labels")]
        public Dictionary<String, String> labels { get; set; }
        /// <summary>
        /// 为 Route 设置 Upstream 连接、发送消息和接收消息的超时时间（单位为秒）。该配置将会覆盖在 Upstream 中配置的 timeout 选项。
        /// {"connect": 3, "send": 3, "read": 3}
        /// </summary>
        [JsonProperty("timeout")]
        public timeoutInfo timeout { get; set; }
        /// <summary>
        /// 当设置为 true 时，启用 websocket(boolean), 默认值为 false。
        /// </summary>
        [JsonProperty("enable_websocket\t")]
        public bool enableWebsocket { get; set; }

        [JsonProperty("script_id")]
        public String scriptID { get; set; }
        [JsonProperty("script")]
        public String script { get; set; }
        [JsonProperty("plugin_config_id")]
        public String pluginConfigID { get; set; }
    }

    /**
     * Route 对象 JSON 配置示例：
        {
            "id": "1",                            # id，非必填
            "uris": ["/a","/b"],                  # 一组 URL 路径
            "methods": ["GET","POST"],            # 可以填多个方法
            "hosts": ["a.com","b.com"],           # 一组 host 域名
            "plugins": {},                        # 指定 route 绑定的插件
            "priority": 0,                        # apisix 支持多种匹配方式，可能会在一次匹配中同时匹配到多条路由，此时优先级高的优先匹配中
            "name": "路由 xxx",
            "desc": "hello world",
            "remote_addrs": ["127.0.0.1"],        # 一组客户端请求 IP 地址
            "vars": [["http_user", "==", "ios"]], # 由一个或多个 [var, operator, val] 元素组成的列表
            "upstream_id": "1",                   # upstream 对象在 etcd 中的 id ，建议使用此值
            "upstream": {},                       # upstream 信息对象，建议尽量不要使用
            "timeout": {                          # 为 route 设置 upstream 的连接、发送消息、接收消息的超时时间。
                "connect": 3,
                "send": 3,
                "read": 3
            },
            "filter_func": ""                     # 用户自定义的过滤函数，非必填
        }

    0. 创建一个路由：
        curl http://127.0.0.1:9180/apisix/admin/routes/1 \
        -H 'X-API-KEY: edd1c9f034335f136f87ad84b625c8f1' -X PUT -i -d '
        {
            "uri": "/index.html",
            "hosts": ["foo.com", "*.bar.com"],
            "remote_addrs": ["127.0.0.0/8"],
            "methods": ["PUT", "GET"],
            "enable_websocket": true,
            "upstream": {
                "type": "roundrobin",
                "nodes": {
                    "127.0.0.1:1980": 1
                }
            }
        }'

    1. 创建一个有效期为 60 秒的路由，过期后自动删除：
        curl 'http://127.0.0.1:9180/apisix/admin/routes/2?ttl=60' \
        -H 'X-API-KEY: edd1c9f034335f136f87ad84b625c8f1' -X PUT -i -d '
        {
            "uri": "/aa/index.html",
            "upstream": {
                "type": "roundrobin",
                "nodes": {
                    "127.0.0.1:1980": 1
                }
            }
        }'

    2. 在路由中新增一个上游节点：
        curl http://127.0.0.1:9180/apisix/admin/routes/1 \
        -H 'X-API-KEY: edd1c9f034335f136f87ad84b625c8f1' -X PATCH -i -d '
        {
            "upstream": {
                "nodes": {
                    "127.0.0.1:1981": 1
                }
            }
        }'
    执行成功后，上游节点将更新为：
        {
            "127.0.0.1:1980": 1,
            "127.0.0.1:1981": 1
        }

    3. 更新路由中上游节点的权重：
        curl http://127.0.0.1:9180/apisix/admin/routes/1 \
        -H 'X-API-KEY: edd1c9f034335f136f87ad84b625c8f1' -X PATCH -i -d '
        {
            "upstream": {
                "nodes": {
                    "127.0.0.1:1981": 10
                }
            }
        }'
    执行成功后，上游节点将更新为：
        {
            "127.0.0.1:1980": 1,
            "127.0.0.1:1981": 10
        }


    4. 从路由中删除一个上游节点：
        curl http://127.0.0.1:9180/apisix/admin/routes/1 \
        -H 'X-API-KEY: edd1c9f034335f136f87ad84b625c8f1' -X PATCH -i -d '
        {
            "upstream": {
                "nodes": {
                    "127.0.0.1:1980": null
                }
            }
        }'
    执行成功后，Upstream nodes 将更新为：
        {
            "127.0.0.1:1981": 10
        }

    5. 使用 sub path 更新路由中的 methods：
        curl http://127.0.0.1:9180/apisix/admin/routes/1/methods \
        -H 'X-API-KEY: edd1c9f034335f136f87ad84b625c8f1' -X PATCH -i -d '["POST", "DELETE", "PATCH"]'
    执行成功后，methods 将不保留原来的数据，更新为：
        ["POST", "DELETE", "PATCH"]

     **/
}
