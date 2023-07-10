using Newtonsoft.Json;
using System;
using System.Diagnostics;

namespace ApiSix.Sharp.model
{
    /// <summary>
    /// skywalking 插件用于与 Apache SkyWalking 集成。
    /// SkyWalking 使用其原生的 NGINX Lua tracer 从服务和 URI 角度提供了分布式追踪、拓扑分析以及 metrics 。
    /// 服务端目前支持 HTTP 和 gRPC 两种协议，在 APISIX 中目前只支持 HTTP 协议。
    /// https://apisix.apache.org/zh/docs/apisix/plugins/skywalking/
    /// </summary>
    public class SkywalkingPlugin : Plugin
    {
        /// <summary>
        /// 采样的比例。设置为 1 时，将对所有请求进行采样
        /// 有效值 [0.00001, 1]
        /// </summary>
        [JsonProperty("sample_ratio")]
        public decimal sampleRatio { get; set; }
    }
    /**
     * 如何设置 Endpoint#
        你可以在配置文件（./conf/config.yaml）中配置以下属性：
         名称	                类型	 默认值	                    描述
        service_name	        string	"APISIX"	                SkyWalking 上报的服务名称。
        service_instance_name	string	"APISIX Instance Name"	    SkyWalking 上报的服务实例名。设置为 $hostname 时，将获取本机主机名。
        endpoint_addr	        string	"http://127.0.0.1:12800"	SkyWalking 的 HTTP endpoint 地址，例如：http://127.0.0.1:12800。
        report_interval     	integer	SkyWalking 客户端内置的值	上报间隔时间，单位为秒。
     * 
     * 
     * 以下是配置示例：./conf/config.yaml
        plugin_attr:
          skywalking:
            service_name: APISIX
            service_instance_name: "APISIX Instance Name"
            endpoint_addr: http://127.0.0.1:12800
     * 
     * 
     * **/
}
