using Newtonsoft.Json;
using System;

namespace ApiSix.CSharp.model
{
    /// <summary>
    /// Zipkin 是一个开源的分布调用链追踪系统。zipkin 插件基于 Zipkin API 规范，支持收集跟踪信息并上报给 Zipkin Collector。
    /// 
    /// 该插件也支持 Apache SkyWalking 和 Jaeger，因为它们都支持了 Zipkin v1 和 v2 API。
    /// 当然 zipkin 插件也可以与其他支持了 Zipkin v1 和 v2 API 格式的调用链追踪系统集成。
    /// https://apisix.apache.org/zh/docs/apisix/plugins/zipkin/
    /// </summary>
    public class ZipkinPlugin : Plugin
    {
        /// <summary>
        /// Zipkin 的 HTTP 节点。例如：http://127.0.0.1:9411/api/v2/spans。
        /// </summary>
        [JsonProperty("endpoint")]
        public String endpoint { get; set; }
        /// <summary>
        /// 对请求进行采样的比例。当设置为 1 时，将对所有请求进行采样。
        /// 有效值 [0.00001, 1]
        /// </summary>
        [JsonProperty("sample_ratio")]
        public decimal sampleRatio { get; set; }
        /// <summary>
        /// 需要在 Zipkin 中显示的服务名称。
        /// </summary>
        [JsonProperty("service_name")]
        public String serviceName { get; set; } = "APISIX";
        /// <summary>
        /// 当前 APISIX 实例的 IPv4 地址。
        /// </summary>
        [JsonProperty("server_addr")]
        public String serverAddr { get; set; } = "$server_addr";
        /// <summary>
        /// span 类型的版本。
        /// 有效值 [1, 2]
        /// </summary>
        [JsonProperty("span_version")]
        public int spanVersion { get; set; } = 2;
    }
}
