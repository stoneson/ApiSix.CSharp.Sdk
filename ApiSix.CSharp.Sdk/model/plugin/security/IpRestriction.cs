using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ApiSix.CSharp.model
{
    /// <summary>
    /// ip-restriction 插件可以通过将 IP 地址列入白名单或黑名单来限制对服务或路由的访问。
    /// 支持对单个 IP 地址、多个 IP 地址和类似 10.10.10.0/24 的 CIDR（无类别域间路由）范围的限制。
    /// 
    /// https://apisix.apache.org/zh/docs/apisix/plugins/ip-restriction/
    /// </summary>
    public class IpRestriction : Plugin
    {
        /// <summary>
        /// 加入白名单的 IP 地址或 CIDR 范围。
        /// </summary>
        [JsonProperty("whitelist")]
        public List<String> whiteList { get; set; }
        /// <summary>
        /// 加入黑名单的 IP 地址或 CIDR 范围
        /// whitelist 和 blacklist 属性无法同时在同一个服务或路由上使用，只能使用其中之一。
        /// </summary>
        [JsonProperty("blacklist")]
        public List<String> blackList { get; set; }
        /// <summary>
        /// 在未允许的 IP 访问的情况下返回的信息。
        /// </summary>
        public string message { get; set; } = "Your IP address is not allowed";
    }
}
