using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ApiSix.Sharp.model
{
    /// <summary>
    /// proxy-rewrite 是处理上游代理信息重写的插件，支持对 scheme、uri、host 等信息进行重写。
    /// 
    /// https://apisix.apache.org/zh/docs/apisix/plugins/proxy-rewrite/
    /// </summary>
    public class ProxyRewrite : Plugin
    {
        [JsonProperty("scheme")]
        public String scheme { get; set; }
        /// <summary>
        /// 转发到上游的新 uri 地址。支持 NGINX variables 变量，例如：$arg_name。
        /// </summary>
        [JsonProperty("uri")]
        public String uri { get; set; }
        /// <summary>
        /// 将路由的请求方法代理为该请求方法。
        /// 有效值	["GET", "POST", "PUT", "HEAD", "DELETE", "OPTIONS","MKCOL", "COPY", "MOVE", "PROPFIND", "PROPFIND","LOCK", "UNLOCK", "PATCH", "TRACE"]
        /// </summary>
        [JsonProperty("method")]
        public String method { get; set; }
        /// <summary>
        /// 转发到上游的新 uri 地址。使用正则表达式匹配来自客户端的 uri，
        /// 如果匹配成功，则使用模板替换转发到上游的 uri，
        /// 如果没有匹配成功，则将客户端请求的 uri 转发至上游。
        /// 当同时配置 uri 和 regex_uri 属性时，优先使用 uri。
        /// 例如：["^/iresty/(.)/(.)/(.*)","/$1-$2-$3"] 第一个元素代表匹配来自客户端请求的 uri 正则表达式，
        /// 第二个元素代表匹配成功后转发到上游的 uri 模板。但是目前 APISIX 仅支持一个 regex_uri，所以 regex_uri 数组的长度是 2。
        /// </summary>
        [JsonProperty("regex_uri")]
        public List<String> regexUri { get; set; }
        /// <summary>
        /// 转发到上游的新 host 地址，例如：iresty.com。
        /// </summary>
        [JsonProperty("host")]
        public String host { get; set; }
        /// <summary>
        /// 请求头
        /// </summary>
        [JsonProperty("headers")]
        public RewriteHeader headers { get; set; }

    }
}
