using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ApiSix.Sharp.model
{
    /// <summary>
    /// 消费者
    /// Consumer 是某类服务的消费者，需要与用户认证体系配合才能使用。
    /// Consumer 使用 username 作为唯一标识，仅支持使用 HTTP PUT 方法创建 Consumer。
    /// </summary>
    public class Consumer : BaseModel
    {
        public String id { get; set; }
        /// <summary>
        ///  名称。
        /// </summary>
        [JsonProperty("username")]
        public String username { get; set; }
        /// <summary>
        ///  Group 名称。
        /// </summary>
        [JsonProperty("group_id")]
        public String groupId { get; set; }
        /// <summary>
        /// 该 Consumer 对应的插件配置，它的优先级是最高的：Consumer > Route > Plugin Config > Service。
        /// 对于具体插件配置，请参考 Plugins。
        /// </summary>
        [JsonProperty("plugins")]
        public Dictionary<String, Plugin> plugins { get; set; }
        /// <summary>
        /// 描述
        /// </summary>
        [JsonProperty("desc")]
        public String desc { get; set; }
        /// <summary>
        /// 标识附加属性的键值对。	
        /// {"version":"v2","build":"16","env":"production"}
        /// </summary>
        [JsonProperty("labels")]
        public Dictionary<String, String> labels { get; set; }
        /// <summary>
        /// epoch 时间戳，单位为秒。如果不指定则自动创建
        /// </summary>
        [JsonProperty("create_time")]
        public long? createTime { get; set; }
        /// <summary>
        /// epoch 时间戳，单位为秒。如果不指定则自动创建。
        /// </summary>
        [JsonProperty("update_time")]
        public long? updateTime { get; set; }
    }
    /**
     * Consumer 对象 JSON 配置示例：
        {
            "plugins": {},          # 指定 consumer 绑定的插件
            "username": "name",     # 必填
            "desc": "hello world"   # consumer 描述
        }

    同一个 Consumer 可以绑定多个认证插件
    创建 Consumer，并指定认证插件 key-auth，并开启指定插件 limit-count：
        curl http://127.0.0.1:9180/apisix/admin/consumers  \
        -H 'X-API-KEY: edd1c9f034335f136f87ad84b625c8f1' -X PUT -i -d '
        {
            "username": "jack",
            "plugins": {
                "key-auth": {
                    "key": "auth-one"
                },
                "limit-count": {
                    "count": 2,
                    "time_window": 60,
                    "rejected_code": 503,
                    "key": "remote_addr"
                }
            }
        }'
     * **/
}
