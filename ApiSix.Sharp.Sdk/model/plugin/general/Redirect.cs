using Newtonsoft.Json;
using System;

namespace ApiSix.Sharp.model
{
    /// <summary>
    /// redirect 插件可用于配置 URI 重定向。
    /// </summary>
    public class Redirect : Plugin
    {
        /// <summary>
        /// 当设置为 true 并且请求是 HTTP 时，它将被重定向具有相同 URI 和 301 状态码的 HTTPS，原 URI 的查询字符串也将包含在 Location 头中。
        /// https://apisix.apache.org/zh/docs/apisix/plugins/redirect/
        /// 
        /// NOTE
        /// -http_to_https、uri 和 regex_uri 只能配置其中一个属性。
        /// -http_to_https、和 append_query_string 只能配置其中一个属性。
        /// -当开启 http_to_https 时，重定向 URL 中的端口将按如下顺序选取一个值（按优先级从高到低排列）
        ///   --从配置文件（conf/config.yaml）中读取 plugin_attr.redirect.https_port。
        ///   --如果 apisix.ssl 处于开启状态，读取 apisix.ssl.listen 并从中随机选一个 port。
        ///   --使用 443 作为默认 https port。
        /// </summary>
        [JsonProperty("http_to_https")]
        public bool httpToHttps { get; set; }
        /// <summary>
        /// 要重定向到的 URI，可以包含 NGINX 变量。
        /// 例如：/test/index.htm，$uri/index.html，${uri}/index.html，https://example.com/foo/bar。
        /// 如果你引入了一个不存在的变量，它不会报错，而是将其视为一个空变量。
        /// </summary>
        [JsonProperty("uri")]
        public String uri { get; set; }
        /// <summary>
        /// 将来自客户端的 URL 与正则表达式匹配并重定向。
        /// 当匹配成功后使用模板替换发送重定向到客户端，如果未匹配成功会将客户端请求的 URI 转发至上游。 
        /// 和 regex_uri 不可以同时存在。例如：["^/iresty/(.)/(.)/(.*)","/$1-$2-$3"] 
        /// 第一个元素代表匹配来自客户端请求的 URI 正则表达式，
        /// 第二个元素代表匹配成功后发送重定向到客户端的 URI 模板。
        /// </summary>
        [JsonProperty("regex_uri")]
        public System.Collections.Generic.List<String> regexUri { get; set; }

        /// <summary>
        /// HTTP 响应码
        /// 有效值	[200, ...]
        /// </summary>
        [JsonProperty("ret_code")]
        public int retCode { get; set; } = 302;
        /// <summary>
        /// 当设置为 true 时，对返回的 Location Header 按照 RFC3986 的编码格式进行编码。
        /// </summary>
        [JsonProperty("encode_uri")]
        public bool encodeUri { get; set; }
        /// <summary>
        /// 当设置为 true 时，将原始请求中的查询字符串添加到 Location Header。
        /// 如果已配置 uri 或 regex_uri 已经包含查询字符串，则请求中的查询字符串将附加一个&。
        /// 如果你已经处理过查询字符串（例如，使用 NGINX 变量 $request_uri），请不要再使用该参数以避免重复。
        /// </summary>
        [JsonProperty("append_query_string")]
        public bool appendQueryString { get; set; }

    }


}
