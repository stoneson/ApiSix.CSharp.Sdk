using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using static System.Net.WebRequestMethods;

namespace ApiSix.CSharp.model
{
    /// <summary>
    /// 使用 grpc-transcode 插件可以在 HTTP 和 gRPC 请求之间进行转换。
    /// APISIX 接收 HTTP 请求后，首先对请求进行转码，并将转码后的请求转发到 gRPC 服务，获取响应并以 HTTP 格式将其返回给客户端。
    /// 
    /// https://apisix.apache.org/zh/docs/apisix/plugins/grpc-transcode/
    /// </summary>
    public class grpcTranscode : Plugin
    {
        [JsonProperty("proto_id")]
        public String protoId;
        [JsonProperty("service")]
        public string service;
        [JsonProperty("method")]
        public string method { get; set; }

        public decimal deadline { get; set; }
        /// <summary>
        /// 类型	        有效值
        /// enum as result: enum_as_name, enum_as_value
        /// int64 as result: int64_as_number, int64_as_string, int64_as_hexstring
        /// default values: auto_default_values, no_default_values, use_default_values, use_default_metatable
        /// hooks:          enable_hooks, disable_hooks
        /// </summary>
        [JsonProperty("pb_option")]
        public System.Collections.Generic.List<string> pbOption { get; set; }
        [JsonProperty("show_status_in_body")]
        public bool showStatusInBody { get; set; }
        [JsonProperty("status_detail_type")]
        public string statusDetailType { get; set; }
    }

}
