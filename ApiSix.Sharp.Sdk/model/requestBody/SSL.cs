using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ApiSix.Sharp.model
{
    /// <summary>
    /// 你可以使用该资源创建 SSL 证书。
    /// </summary>
    public class SSL : BaseModel
    {
        public String id { get; set; }
        /// <summary>
        /// HTTP 证书。该字段支持使用 APISIX Secret 资源，将值保存在 Secret Manager 中。
        /// </summary>
        public String cert { get; set; }
        /// <summary>
        /// HTTPS 证书私钥。该字段支持使用 APISIX Secret 资源，将值保存在 Secret Manager 中
        /// </summary>
        public String key { get; set; }
        /// <summary>
        /// 当你想给同一个域名配置多个证书时，除了第一个证书需要通过 cert 传递外，剩下的证书可以通过该参数传递上来。
        /// </summary>
        public List<String> certs { get; set; }
        /// <summary>
        /// certs 对应的证书私钥，需要与 certs 一一对应。
        /// </summary>
        public List<String> keys { get; set; }
        /// <summary>
        /// 非空数组形式，可以匹配多个 SNI。
        /// </summary>
        public List<String> snis { get; set; }

        /// <summary>
        /// 标识附加属性的键值对
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
        /// epoch 时间戳，单位为秒。如果不指定则自动创建
        /// </summary>
        [JsonProperty("update_time")]
        public long? updateTime { get; set; }
        /// <summary>
        /// client 表示证书是客户端证书，APISIX 访问上游时使用；server 表示证书是服务端证书，APISIX 验证客户端请求时使用。
        /// </summary>
        public String type { get; set; } = "server";
        /// <summary>
        /// 1 表示启用，0 表示禁用，默认值为 1。
        /// </summary>
        public int status { get; set; }
    }

    /**
     * 
     * SSL 对象 JSON 配置示例：
        {
            "id": "1",          # id
            "cert": "cert",     # 证书
            "key": "key",       # 私钥
            "snis": ["t.com"]   # HTTPS 握手时客户端发送的 SNI
        }
    **/
}
