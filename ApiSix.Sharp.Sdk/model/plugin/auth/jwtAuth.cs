using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ApiSix.Sharp.model
{
    /// <summary>
    /// jwt-auth 插件用于将 JWT 身份验证添加到 Service 或 Route 中。
    /// 通过 Consumer 将其密匙添加到查询字符串参数、请求头或 cookie 中用来验证其请求。
    /// https://apisix.apache.org/zh/docs/apisix/plugins/jwt-auth/
    /// </summary>
    public class jwtAuth : Plugin
    {
        /// <summary>
        /// Consumer 的 access_key 必须是唯一的。如果不同 Consumer 使用了相同的 access_key ，将会出现请求匹配异常
        /// </summary>
        [JsonProperty("key")]
        public String key { get; set; }
        /// <summary>
        /// 加密秘钥。如果未指定，后台将会自动生成。该字段支持使用 APISIX Secret 资源，将值保存在 Secret Manager 中。
        /// </summary>
        public String secret { get; set; }
        /// <summary>
        /// RSA 或 ECDSA 公钥， algorithm 属性选择 RS256 或 ES256 算法时必选。
        /// 该字段支持使用 APISIX Secret 资源，将值保存在 Secret Manager 中。
        /// </summary>
        [JsonProperty("public_key")]
        public String publicKkey { get; set; }
        /// <summary>
        /// RSA 或 ECDSA 私钥， algorithm 属性选择 RS256 或 ES256 算法时必选。
        /// 该字段支持使用 APISIX Secret 资源，将值保存在 Secret Manager 中。
        /// </summary>
        [JsonProperty("private_key")]
        public String privateKey { get; set; }
        /// <summary>
        /// 加密算法。
        /// ["HS256", "HS512", "RS256", "ES256"]
        /// </summary>
        [JsonProperty("algorithm")]
        public String algorithm { get; set; } = "HS256";
        /// <summary>
        /// token 的超时时间。
        /// </summary>
        public int exp { get; set; } = 86400;
        /// <summary>
        /// 当设置为 true 时，密钥为 base64 编码
        /// </summary>
        [JsonProperty("base64_secret")]
        public bool base64Secret { get; set; } = false;
        /// <summary>
        /// 定义生成 JWT 的服务器和验证 JWT 的服务器之间的时钟偏移。该值应该是零（0）或一个正整数。
        /// </summary>
        [JsonProperty("lifetime_grace_period")]
        public int lifetimeGracePeriod { get; set; } = 0;
        public bool disable { get; set; }

        [JsonProperty("_meta")]
        public Dictionary<String, object> meta { get; set; }
    }

}
