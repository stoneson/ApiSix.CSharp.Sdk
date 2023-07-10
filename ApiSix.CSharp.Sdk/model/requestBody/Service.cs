using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ApiSix.CSharp.model
{
    /// <summary>
    /// Service 是某类 API 的抽象（也可以理解为一组 Route 的抽象）。
    /// 它通常与上游服务抽象是一一对应的，Route 与 Service 之间，通常是 N:1 的关系。
    /// </summary>
    public class Service : BaseModel
    {
        /// <summary>
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
        /// 服务描述
        /// </summary>
        [JsonProperty("desc")]
        public String desc { get; set; }
        /// <summary>
        /// Plugin 配置
        /// </summary>
        [JsonProperty("plugins")]
        public Dictionary<String, Plugin> plugins { get; set; }
        /// <summary>
        /// Upstream 配置
        /// upstream_id 二选一
        /// </summary>
        [JsonProperty("upstream")]
        public Upstream upstream { get; set; }
        /// <summary>
        /// 需要使用的 upstream id
        /// 与 upstream 二选一
        /// </summary>
        [JsonProperty("upstream_id")]
        public String upstreamId { get; set; }
        /// <summary>
        /// 标识附加属性的键值对
        /// {"version":"v2","build":"16","env":"production"}
        /// </summary>
        [JsonProperty("labels")]
        public Dictionary<String, String> labels { get; set; }
        /// <summary>
        /// 当设置为 true 时，启用 websocket(boolean), 默认值为 false。
        /// </summary>
        [JsonProperty("enable_websocket\t")]
        public bool enable_websocket { get; set; }
        /// <summary>
        /// 非空列表形态的 host，表示允许有多个不同 host，匹配其中任意一个即可。
        /// </summary>
        [JsonProperty("hosts")]
        public List<String> hosts { get; set; }
        /// <summary>
        /// epoch 时间戳，单位为秒。如果不指定则自动创建
        /// </summary>
        [JsonProperty("create_time")]
        public long? createTime { get; set; }
        /// <summary>
        /// epoch 时间戳，单位为秒。如果不指定则自动创建
        /// </summary>
        [JsonProperty("update_time")]
        public long? updateTime { get; set; }
    }

    /**
     * Service 对象 JSON 配置示例：
        {
            "id": "1",                # id
            "plugins": {},            # 指定 service 绑定的插件
            "upstream_id": "1",       # upstream 对象在 etcd 中的 id ，建议使用此值
            "upstream": {},           # upstream 信息对象，不建议使用
            "name": "test svc",       # service 名称
            "desc": "hello world",    # service 描述
            "enable_websocket": true, # 启动 websocket 功能
            "hosts": ["foo.com"]
}
     * 
     * 创建一个 Service：
        curl http://127.0.0.1:9180/apisix/admin/services/201  \
        -H 'X-API-KEY: edd1c9f034335f136f87ad84b625c8f1' -X PUT -i -d '
        {
            "plugins": {
                "limit-count": {
                    "count": 2,
                    "time_window": 60,
                    "rejected_code": 503,
                    "key": "remote_addr"
                }
            },
            "enable_websocket": true,
            "upstream": {
                "type": "roundrobin",
                "nodes": {
                    "127.0.0.1:1980": 1
                }
            }
        }'

    在 Service 中添加一个上游节点：
        curl http://127.0.0.1:9180/apisix/admin/services/201 \
        -H 'X-API-KEY: edd1c9f034335f136f87ad84b625c8f1' -X PATCH -i -d '
        {
            "upstream": {
                "nodes": {
                    "127.0.0.1:1981": 1
                }
            }
        }'

    删除 Service 中的一个上游节点：
        curl http://127.0.0.1:9180/apisix/admin/services/201 \
        -H 'X-API-KEY: edd1c9f034335f136f87ad84b625c8f1' -X PATCH -i -d '
        {
            "upstream": {
                "nodes": {
                    "127.0.0.1:1980": null
                }
            }
        }'
     * **/
}
