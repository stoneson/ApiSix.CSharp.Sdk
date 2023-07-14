using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ApiSix.CSharp.model
{
    public class StreamRoute : BaseBody
    {
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("sni")]
        public String SNI { get; set; }
        /// <summary>
        /// 标识描述、使用场景等。
        /// </summary>
        [JsonProperty("desc")]
        public String desc { get; set; }
        /// <summary>
        /// 客户端请求的 IP 地址。支持 IPv4 地址，如：192.168.1.101 以及 CIDR 格式的支持 192.168.1.0/24；
        /// 支持 IPv6 地址匹配，如 ::1，fe80::1，fe80::1/64 等。
        /// 
        /// remote_addrs 二选一
        /// </summary>
        [JsonProperty("remote_addr")]
        public String remoteAddr { get; set; }
        [JsonProperty("server_addr")]
        public String serverAddr { get; set; }
        [JsonProperty("server_port")]
        public int? serverPort { get; set; }
        /// <summary>
        /// Upstream 配置
        /// 与 upstream_id 二选一
        /// </summary>
        [JsonProperty("upstream")]
        public Upstream upstream { get; set; }
        /// <summary>
        /// 需要使用的 upstream id
        /// 与 upstream 二选一
        /// </summary>
        [JsonProperty("upstream_id")]
        public String upstreamId { get; set; }
        /// <summary>
        /// Plugin 配置
        /// </summary>
        [JsonProperty("plugins")]
        public Dictionary<String, Plugin> plugins { get; set; }
    }
}
