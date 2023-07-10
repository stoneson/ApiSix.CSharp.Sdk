using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ApiSix.CSharp.model
{
    /// <summary>
    /// 使用 basic-auth 插件可以将 Basic_access_authentication 添加到 Route 或 Service 中。
    /// 该插件需要与 Consumer 一起使用。API 的消费者可以将它们的密钥添加到请求头中以验证其请求。
    /// https://apisix.apache.org/zh/docs/apisix/plugins/basic-auth/
    /// </summary>
    public class BasicAuth : Plugin
    {
        /// <summary>
        /// Consumer 的用户名并且该用户名是唯一，如果多个 Consumer 使用了相同的 username，将会出现请求匹配异常。
        /// </summary>
        [JsonProperty("username")]
        public String userName { get; set; }
        /// <summary>
        /// 用户的密码。该字段支持使用 APISIX Secret 资源，将值保存在 Secret Manager 中。
        /// </summary>
        [JsonProperty("password")]
        public String password { get; set; }

        [JsonProperty("_meta")]
        public Dictionary<String, object> meta { get; set; }
    }


}
