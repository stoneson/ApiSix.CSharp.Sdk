using Newtonsoft.Json;
using System;

namespace ApiSix.Sharp.model
{
    /// <summary>
    /// cors 插件可以让你轻松地为服务端启用 CORS（Cross-Origin Resource Sharing，跨域资源共享）的返回头。
    /// </summary>
    public class httpCors : Plugin
    {
        /// <summary>
        /// 允许跨域访问的 Origin，格式为 scheme://host:port，示例如 https://somedomain.com:8081。
        /// 如果你有多个 Origin，请使用 , 分隔。
        /// 当 allow_credential 为 false 时，可以使用 * 来表示允许所有 Origin 通过。
        /// 你也可以在启用了 allow_credential 后使用 ** 强制允许所有 Origin 均通过，但请注意这样存在安全隐患。
        /// 
        /// https://apisix.apache.org/zh/docs/apisix/plugins/cors/
        /// </summary>
        [JsonProperty("allow_origins")]
        public String allowOrigins { get; set; } = "*";
        /// <summary>
        /// 允许跨域访问的 Method，比如：GET，POST 等。如果你有多个 Method，请使用 , 分割。
        /// 当 allow_credential 为 false 时，可以使用 * 来表示允许所有 Method 通过。
        /// 你也可以在启用了 allow_credential 后使用 ** 强制允许所有 Method 都通过，但请注意这样存在安全隐患
        /// </summary>
        [JsonProperty("allow_methods")]
        public String allowMethods { get; set; } = "*";
        /// <summary>
        /// 允许跨域访问时请求方携带哪些非 CORS 规范 以外的 Header。如果你有多个 Header，请使用 , 分割。
        /// 当 allow_credential 为 false 时，可以使用 * 来表示允许所有 Header 通过。
        /// 你也可以在启用了 allow_credential 后使用 ** 强制允许所有 Header 都通过，但请注意这样存在安全隐患。
        /// </summary>
        [JsonProperty("allow_headers")]
        public String allowHeaders { get; set; } = "*";
        /// <summary>
        /// 允许跨域访问时响应方携带哪些非 CORS 规范 以外的 Header。如果你有多个 Header，请使用 , 分割。
        /// 当 allow_credential 为 false 时，可以使用 * 来表示允许任意 Header 。
        /// 你也可以在启用了 allow_credential 后使用 ** 强制允许任意 Header，但请注意这样存在安全隐患。
        /// </summary>
        [JsonProperty("expose_headers")]
        public String exposeHeaders { get; set; } = "*";
        /// <summary>
        /// 浏览器缓存 CORS 结果的最大时间，单位为秒。在这个时间范围内，浏览器会复用上一次的检查结果，-1 表示不缓存。
        /// 请注意各个浏览器允许的最大时间不同，详情请参考 Access-Control-Max-Age - MDN。
        /// </summary>
        [JsonProperty("max_age")]
        public int maxAge { get; set; } = 5;
        /// <summary>
        /// 是否允许跨域访问的请求方携带凭据（如 Cookie 等）。根据 CORS 规范，
        /// 如果设置该选项为 true，那么将不能在其他属性中使用 *。
        /// </summary>
        [JsonProperty("allow_credential")]
        public bool allowCredential { get; set; }
        /// <summary>
        /// 通过引用插件元数据的 allow_origins 配置允许跨域访问的 Origin。
        /// 比如当插件元数据为 "allow_origins": {"EXAMPLE": "https://example.com"} 时，配置 ["EXAMPLE"] 将允许 Origin https://example.com 的访问。
        /// </summary>
        [JsonProperty("allow_origins_by_metadata")]
        public System.Collections.Generic.List<String> allowOriginsByMetadata { get; set; }
        /// <summary>
        /// 使用正则表达式数组来匹配允许跨域访问的 Origin，如 [".*\.test.com"] 可以匹配任何 test.com 的子域名 *。
        /// 如果 allow_origins_by_regex 属性已经指定，则会忽略 allow_origins 属性。
        /// </summary>
        [JsonProperty("allow_origins_by_regex")]
        public System.Collections.Generic.List<String> allowOriginsByRegex { get; set; }
    }

}
