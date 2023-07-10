using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ApiSix.Sharp.model
{
    /// <summary>
    /// api-breaker 插件实现了 API 熔断功能，从而帮助我们保护上游业务服务。
    /// 注意：
    /// 关于熔断超时逻辑，由代码逻辑自动按触发不健康状态的次数递增运算：
    /// 当上游服务返回 unhealthy.http_statuses 配置中的状态码（默认为 500），并达到 unhealthy.failures 预设次数时（默认为 3 次），则认为上游服务处于不健康状态。
    /// 第一次触发不健康状态时，熔断 2 秒。超过熔断时间后，将重新开始转发请求到上游服务，如果继续返回 unhealthy.http_statuses 状态码，记数再次达到 unhealthy.failures 预设次数时，熔断 4 秒。依次类推（2，4，8，16，……），直到达到预设的 max_breaker_sec值。
    /// 当上游服务处于不健康状态时，如果转发请求到上游服务并返回 healthy.http_statuses 配置中的状态码（默认为 200），并达到 healthy.successes 次时，则认为上游服务恢复至健康状态。
    /// 
    /// https://apisix.apache.org/zh/docs/apisix/plugins/api-breaker/
    /// </summary>
    public class apiBreaker : Plugin
    {
        /// <summary>
        /// 当上游服务处于不健康状态时返回的 HTTP 错误码。
        /// 有效值	[200, ..., 599]
        /// </summary>
        [JsonProperty("break_response_code")]
        public int breakResponseCode { get; set; }
        /// <summary>
        /// 当上游服务处于不健康状态时返回的 HTTP 响应体信息。
        /// </summary>
        [JsonProperty("break_response_body")]
        public String breakResponseBody { get; set; }
        /// <summary>
        /// 当上游服务处于不健康状态时返回的 HTTP 响应头信息。
        /// 该字段仅在配置了 break_response_body 属性时生效，并能够以 $var 的格式包含 APISIX 变量，
        /// 比如 {"key":"X-Client-Addr","value":"$remote_addr:$remote_port"}。
        /// 
        ///有效值 [{"key":"header_name","value":"can contain Nginx $var"}]
        /// </summary>
        [JsonProperty("break_response_headers")]
        public List<Dictionary<String, String>> breakResponseHeaders { get; set; }

        /// <summary>
        /// 请求速率超过（rate + burst）的请求会被直接拒绝。
        /// >=3
        /// </summary>
        [JsonProperty("max_breaker_sec")]
        public int maxBreakerSec { get; set; } = 300;

        [JsonProperty("unhealthy")]
        public apiBreakerUnhealthyInfo unhealthy { get; set; }
        [JsonProperty("healthy")]
        public apiBreakerHealthyInfo healthy { get; set; }
    }

    public class apiBreakerHealthyInfo
    {
        //[JsonProperty("interval")]
        // public int? interval { get; set; } = 1;
        /// <summary>
        /// 上游服务触发健康状态的连续正常请求次数。
        /// >=1
        /// </summary>
        [JsonProperty("successes")]
        public int? successes { get; set; } = 3;
        /// <summary>
        /// 上游服务处于健康状态时的 HTTP 状态码。
        /// [200, ..., 499]
        /// </summary>
        [JsonProperty("http_statuses")]
        public List<int> httpStatuses { get; set; } = new List<int> { 200 };
    }
    public class apiBreakerUnhealthyInfo
    {
        //[JsonProperty("interval")]
        //public int? interval { get; set; } = 1;

        //[JsonProperty("timeouts")]
        //public int? timeouts { get; set; } = 3;
        //[JsonProperty("http_failures")]
        //public int? httpFailures { get; set; } = 5;
        /// <summary>
        /// 上游服务在一定时间内触发不健康状态的异常请求次数
        /// >=1
        /// </summary>
        [JsonProperty("failures")]
        public int? failures { get; set; } = 3;
        /// <summary>
        /// 上游服务处于不健康状态时的 HTTP 状态码。
        /// [500, ..., 599]
        /// </summary>
        [JsonProperty("http_statuses")]
        public List<int> httpStatuses { get; set; } = new List<int>() { 500 };//429, 404, 500, 501, 502, 503, 504, 505 };
    }
}
