using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ApiSix.CSharp.model
{
    /// <summary>
    /// response-rewrite 插件支持修改上游服务或 APISIX 返回的 Body 和 Header 信息。
    /// 
    /// 该插件可以应用在以下场景中：
    /// 通过设置 Access-Control-Allow-* 字段实现 CORS（跨域资源共享）的功能。
    /// 通过设置标头中的 status_code 和 Location 字段实现重定向。
    /// 提示：如果你仅需要重定向功能，建议使用 redirect 插件。
    /// 
    /// https://apisix.apache.org/zh/docs/apisix/plugins/response-rewrite/
    /// </summary>
    public class ResponseRewrite : Plugin
    {
        /// <summary>
        /// 修改上游返回状态码，默认保留原始响应代码。
        /// [200, 598]
        /// </summary>
        [JsonProperty("status_code")]
        public String statusCode { get; set; }
        /// <summary>
        /// 修改上游返回的 body 内容，如果设置了新内容，header 里面的 content-length 字段也会被去掉。
        /// body 和 filters 属性只能配置其中一个。
        /// </summary>
        [JsonProperty("body")]
        public String body { get; set; }
        /// <summary>
        /// 描述 body 字段是否需要 base64 解码之后再返回给客户端，用在某些图片和 Protobuffer 场景。
        /// </summary>
        [JsonProperty("body_base64")]
        public String bodyBase64 { get; set; }
        /// <summary>
        /// 请求头
        /// </summary>
        [JsonProperty("headers")]
        public RewriteHeader headers { get; set; }

        /// <summary>
        /// vars 是一个表达式列表，只有满足条件的请求和响应才会修改 body 和 header 信息，来自 lua-resty-expr。
        /// 如果 vars 字段为空，那么所有的重写动作都会被无条件的执行。
        /// </summary>
        [JsonProperty("vars")]
        public List<String> vars { get; set; }
        /// <summary>
        /// 一组过滤器，采用指定字符串表达式修改响应体
        /// body 和 filters 属性只能配置其中一个。
        /// </summary>
        [JsonProperty("filters")]
        public List<ResponseFilter> filters { get; set; }
    }

    public class ResponseFilter
    {
        /// <summary>
        ///用于匹配响应体正则表达式。
        /// </summary>
        [JsonProperty("regex")]
        public string regex { get; set; }
        /// <summary>
        /// 替换范围，"once" 表达式 filters.regex 仅替换首次匹配上响应体的内容，"global" 则进行全局替换。
        /// 有效值 "once","global"
        /// </summary>
        [JsonProperty("scope")]
        public string scope { get; set; } = "once";
        /// <summary>
        /// 替换后的内容。
        /// </summary>
        [JsonProperty("replace")]
        public string replace { get; set; }
        /// <summary>
        /// 正则匹配有效参数，可选项见 ngx.re.match。
        /// </summary>
        [JsonProperty("options")]
        public string options { get; set; }
    }
}
