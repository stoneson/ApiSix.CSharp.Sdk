using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.IO.Compression;
using System.IO;
using System.Text;

namespace ApiSix.CSharp
{
    /// <summary>协议类型</summary>
    public enum NetType : Byte
    {
        /// <summary>未知协议</summary>
        Unknown = 0,

        /// <summary>传输控制协议</summary>
        Tcp = 6,

        /// <summary>用户数据报协议</summary>
        Udp = 17,

        /// <summary>Http协议</summary>
        Http = 80,

        /// <summary>Https协议</summary>
        Https = 43,

        /// <summary>WebSocket协议</summary>
        WebSocket = 81
    }

    /// <summary>网络工具类</summary>
    public static class NetHelper
    {
        #region 属性
        private static readonly ICache _Cache = MemoryCache.Instance;
        #endregion

        #region 构造
        static NetHelper()
        {
            // 网络有变化时，清空所有缓存
            NetworkChange.NetworkAddressChanged += NetworkChange_NetworkAddressChanged;
            NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;
        }

        private static void NetworkChange_NetworkAvailabilityChanged(Object sender, NetworkAvailabilityEventArgs e) => _Cache.Clear();

        private static void NetworkChange_NetworkAddressChanged(Object sender, EventArgs e) => _Cache.Clear();
        #endregion

        #region 辅助函数
        /// <summary>设置超时检测时间和检测间隔</summary>
        /// <param name="socket">要设置的Socket对象</param>
        /// <param name="iskeepalive">是否启用Keep-Alive</param>
        /// <param name="starttime">多长时间后开始第一次探测（单位：毫秒）</param>
        /// <param name="interval">探测时间间隔（单位：毫秒）</param>
        public static void SetTcpKeepAlive(this Socket socket, Boolean iskeepalive, Int32 starttime = 10000, Int32 interval = 10000)
        {
            if (socket == null || !socket.Connected) return;
            UInt32 dummy = 0;
            var inOptionValues = new Byte[Marshal.SizeOf(dummy) * 3];
            BitConverter.GetBytes((UInt32)(iskeepalive ? 1 : 0)).CopyTo(inOptionValues, 0);
            BitConverter.GetBytes((UInt32)starttime).CopyTo(inOptionValues, Marshal.SizeOf(dummy));
            BitConverter.GetBytes((UInt32)interval).CopyTo(inOptionValues, Marshal.SizeOf(dummy) * 2);

#if !NETFRAMEWORK
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                socket.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);
#endif
        }

        /// <summary>分析地址，根据IP或者域名得到IP地址，缓存60秒，异步更新</summary>
        /// <param name="hostname"></param>
        /// <returns></returns>
        public static IPAddress ParseAddress(this String hostname)
        {
            if (hostname.IsNullOrEmpty()) return null;

            var key = $"NetHelper:ParseAddress:{hostname}";
            if (_Cache.TryGetValue<IPAddress>(key, out var address)) return address;

            address = NetUri.ParseAddress(hostname)?.FirstOrDefault();

            _Cache.Set(key, address, 60);

            return address;
        }

        /// <summary>分析网络终结点</summary>
        /// <param name="address">地址，可以不带端口</param>
        /// <param name="defaultPort">地址不带端口时指定的默认端口</param>
        /// <returns></returns>
        public static IPEndPoint ParseEndPoint(String address, Int32 defaultPort = 0)
        {
            if (String.IsNullOrEmpty(address)) return null;

            var p = address.IndexOf("://");
            if (p >= 0) address = address = address.Substring(p + 3); //address[(p + 3)..];

            p = address.LastIndexOf(':');
            return p > 0
                ? new IPEndPoint(address.Substring(0, p).ParseAddress(), Int32.Parse(address.Substring(p + 1)))//IPEndPoint(address[..p].ParseAddress(), Int32.Parse(address[(p + 1)..]))
                : new IPEndPoint(address.ParseAddress(), defaultPort);
        }

        /// <summary>针对IPv4和IPv6获取合适的Any地址</summary>
        /// <remarks>除了Any地址以为，其它地址不具备等效性</remarks>
        /// <param name="address"></param>
        /// <param name="family"></param>
        /// <returns></returns>
        public static IPAddress GetRightAny(this IPAddress address, AddressFamily family)
        {
            if (address.AddressFamily == family) return address;

            switch (family)
            {
                case AddressFamily.InterNetwork:
                    if (address == IPAddress.IPv6Any) return IPAddress.Any;
                    break;
                case AddressFamily.InterNetworkV6:
                    if (address == IPAddress.Any) return IPAddress.IPv6Any;
                    break;
                default:
                    break;
            }
            return null;
        }

        /// <summary>是否Any地址，同时处理IPv4和IPv6</summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static Boolean IsAny(this IPAddress address) => IPAddress.Any.Equals(address) || IPAddress.IPv6Any.Equals(address);

        /// <summary>是否Any结点</summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public static Boolean IsAny(this EndPoint endpoint) => (endpoint as IPEndPoint).Address.IsAny() || (endpoint as IPEndPoint).Port == 0;

        /// <summary>是否IPv4地址</summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static Boolean IsIPv4(this IPAddress address) => address.AddressFamily == AddressFamily.InterNetwork;

        /// <summary>是否本地地址</summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static Boolean IsLocal(this IPAddress address) => IPAddress.IsLoopback(address) || GetIPsWithCache().Any(ip => ip.Equals(address));

        /// <summary>获取相对于指定远程地址的本地地址</summary>
        /// <param name="address"></param>
        /// <param name="remote"></param>
        /// <returns></returns>
        public static IPAddress GetRelativeAddress(this IPAddress address, IPAddress remote)
        {
            // 如果不是任意地址，直接返回
            var addr = address;
            if (addr == null || !addr.IsAny()) return addr;

            // 如果是本地环回地址，返回环回地址
            if (IPAddress.IsLoopback(remote)) return addr.IsIPv4() ? IPAddress.Loopback : IPAddress.IPv6Loopback;

            // 否则返回本地第一个IP地址
            foreach (var item in GetIPsWithCache())
                if (item.AddressFamily == addr.AddressFamily) return item;
            return null;
        }

        /// <summary>获取相对于指定远程地址的本地地址</summary>
        /// <param name="local"></param>
        /// <param name="remote"></param>
        /// <returns></returns>
        public static IPEndPoint GetRelativeEndPoint(this IPEndPoint local, IPAddress remote)
        {
            if (local == null || remote == null) return local;

            var addr = local.Address.GetRelativeAddress(remote);
            return addr == null ? local : new IPEndPoint(addr, local.Port);
        }

        /// <summary>指定地址的指定端口是否已被使用，似乎没办法判断IPv6地址</summary>
        /// <param name="protocol"></param>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public static Boolean CheckPort(this IPAddress address, NetType protocol, Int32 port)
        {
            //if (ApiSix.Runtime.Mono) return false;
            if (!Runtime.Windows) return false;

            try
            {
                // 某些情况下检查端口占用会抛出异常，原因未知
                var gp = IPGlobalProperties.GetIPGlobalProperties();

                IPEndPoint[] eps = null;
                switch (protocol)
                {
                    case NetType.Tcp:
                        eps = gp.GetActiveTcpListeners();
                        break;
                    case NetType.Udp:
                        eps = gp.GetActiveUdpListeners();
                        break;
                    default:
                        return false;
                }

                foreach (var item in eps)
                    // 先比较端口，性能更好
                    if (item.Port == port && item.Address.Equals(address)) return true;
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }

            return false;
        }

        /// <summary>检查该协议的地址端口是否已经呗使用</summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static Boolean CheckPort(this NetUri uri) => uri.Address.CheckPort(uri.Type, uri.Port);

        /// <summary>获取所有Tcp连接，带进程Id</summary>
        /// <returns></returns>
        public static TcpConnectionInformation2[] GetAllTcpConnections() => !Runtime.Windows ? new TcpConnectionInformation2[0] : TcpConnectionInformation2.GetAllTcpConnections();
        #endregion

        #region 本机信息
        /// <summary>获取活动的接口信息</summary>
        /// <returns></returns>
        public static IEnumerable<IPInterfaceProperties> GetActiveInterfaces()
        {
            foreach (var item in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (item.OperationalStatus != OperationalStatus.Up) continue;
                if (item.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

                var ip = item.GetIPProperties();
                if (ip != null) yield return ip;
            }
        }

        /// <summary>获取可用的DHCP地址</summary>
        /// <returns></returns>
        public static IEnumerable<IPAddress> GetDhcps()
        {
            var list = new List<IPAddress>();
            foreach (var item in GetActiveInterfaces())
            {
#if NET5_0_OR_GREATER
            if (item != null && !OperatingSystem.IsMacOS() && item.DhcpServerAddresses.Count > 0)
            {
                foreach (var elm in item.DhcpServerAddresses)
                {
                    if (list.Contains(elm)) continue;
                    list.Add(elm);

                    yield return elm;
                }
            }
#else
                if (item != null && item.DhcpServerAddresses.Count > 0)
                {
                    foreach (var elm in item.DhcpServerAddresses)
                    {
                        if (list.Contains(elm)) continue;
                        list.Add(elm);

                        yield return elm;
                    }
                }
#endif
            }
        }

        /// <summary>获取可用的DNS地址</summary>
        /// <returns></returns>
        public static IEnumerable<IPAddress> GetDns()
        {
            var list = new List<IPAddress>();
            foreach (var item in GetActiveInterfaces())
                if (item != null && item.DnsAddresses.Count > 0)
                    foreach (var elm in item.DnsAddresses)
                    {
                        if (list.Contains(elm)) continue;
                        list.Add(elm);

                        yield return elm;
                    }
        }

        /// <summary>获取可用的网关地址</summary>
        /// <returns></returns>
        public static IEnumerable<IPAddress> GetGateways()
        {
            var list = new List<IPAddress>();
            foreach (var item in GetActiveInterfaces())
                if (item != null && item.GatewayAddresses.Count > 0)
                    foreach (var elm in item.GatewayAddresses)
                    {
                        if (list.Contains(elm.Address)) continue;
                        list.Add(elm.Address);

                        yield return elm.Address;
                    }
        }

        /// <summary>获取可用的IP地址</summary>
        /// <returns></returns>
        public static IEnumerable<IPAddress> GetIPs()
        {
            var dic = new Dictionary<UnicastIPAddressInformation, Int32>();
            foreach (var item in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (item.OperationalStatus != OperationalStatus.Up)
                    continue;
                if (item.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;

                var ipp = item.GetIPProperties();
                if (ipp != null && ipp.UnicastAddresses.Count > 0)
                {
                    var gw = 0;

#if NET5_0_OR_GREATER
                if (!OperatingSystem.IsAndroid())
                {
                    gw = ipp.GatewayAddresses.Count;
                }
#else
                    gw = ipp.GatewayAddresses.Count;
#endif

                    foreach (var elm in ipp.UnicastAddresses)
                    {
#if NET5_0_OR_GREATER
                    try
                    {
                        if (OperatingSystem.IsWindows() &&
                            elm.DuplicateAddressDetectionState != DuplicateAddressDetectionState.Preferred)
                            continue;
                    }
                    catch { }
#endif

                        dic.Add(elm, gw);
                    }
                }
            }

            // 带网关的接口地址很重要，优先返回
            // Linux下不支持PrefixOrigin
            var ips = dic.OrderByDescending(e => e.Value)
                //.ThenByDescending(e => e.Key.PrefixOrigin == PrefixOrigin.Dhcp || e.Key.PrefixOrigin == PrefixOrigin.Manual)
                .Select(e => e.Key.Address).ToList();

            return ips;
        }

        /// <summary>获取本机可用IP地址，缓存60秒，异步更新</summary>
        /// <returns></returns>
        public static IPAddress[] GetIPsWithCache()
        {
            var key = $"NetHelper:GetIPsWithCache";
            if (_Cache.TryGetValue<IPAddress[]>(key, out var addrs)) return addrs;

            addrs = GetIPs().ToArray();

            _Cache.Set(key, addrs, 60);

            return addrs;
        }

        /// <summary>获取可用的多播地址</summary>
        /// <returns></returns>
        public static IEnumerable<IPAddress> GetMulticasts()
        {
            var list = new List<IPAddress>();
            foreach (var item in GetActiveInterfaces())
                if (item != null && item.MulticastAddresses.Count > 0)
                    foreach (var elm in item.MulticastAddresses)
                    {
                        if (list.Contains(elm.Address)) continue;
                        list.Add(elm.Address);

                        yield return elm.Address;
                    }
        }

        private static readonly String[] _Excludes = new[] { "Loopback", "VMware", "VBox", "Virtual", "Teredo", "Microsoft", "VPN", "VNIC", "IEEE" };
        /// <summary>获取所有物理网卡MAC地址</summary>
        /// <returns></returns>
        public static IEnumerable<Byte[]> GetMacs()
        {
            foreach (var item in NetworkInterface.GetAllNetworkInterfaces())
            {
                // 只要物理网卡
                if (item.NetworkInterfaceType is NetworkInterfaceType.Loopback 
                    || item.NetworkInterfaceType is NetworkInterfaceType.Tunnel 
                    || item.NetworkInterfaceType is NetworkInterfaceType.Unknown)
                    continue;
                if (_Excludes.Any(e => item.Description.Contains(e))) continue;
                if (Runtime.Windows && item.Speed < 1_000_000) continue;

                // 物理网卡在禁用时没有IP，如果有IP，则不能是环回
                var ips = item.GetIPProperties();
                var addrs = ips.UnicastAddresses
                    .Where(e => e.Address.AddressFamily == AddressFamily.InterNetwork)
                    .Select(e => e.Address)
                    .ToArray();
                if (addrs.Length > 0 && addrs.All(e => IPAddress.IsLoopback(e))) continue;

                var mac = item.GetPhysicalAddress()?.GetAddressBytes();
                if (mac != null && mac.Length == 6) yield return mac;
            }
        }

        /// <summary>获取网卡MAC地址（网关所在网卡）</summary>
        /// <returns></returns>
        public static Byte[] GetMac()
        {
            foreach (var item in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (_Excludes.Any(e => item.Description.Contains(e))) continue;
                if (Runtime.Windows && item.Speed < 1_000_000) continue;

                var ips = item.GetIPProperties();
                var addrs = ips.UnicastAddresses
                    .Where(e => e.Address.AddressFamily == AddressFamily.InterNetwork)
                    .Select(e => e.Address)
                    .ToArray();
                if (addrs.All(e => IPAddress.IsLoopback(e))) continue;

                // 网关
                addrs = ips.GatewayAddresses
                    .Where(e => e.Address.AddressFamily == AddressFamily.InterNetwork)
                    .Select(e => e.Address)
                    .ToArray();
                if (addrs.Length == 0) continue;

                var mac = item.GetPhysicalAddress()?.GetAddressBytes();
                if (mac != null && mac.Length == 6) return mac;
            }

            return null;
        }

        /// <summary>获取本地第一个IPv4地址</summary>
        /// <returns></returns>
        public static IPAddress MyIP() => GetIPsWithCache().FirstOrDefault(ip => ip.IsIPv4() && !IPAddress.IsLoopback(ip) && ip.GetAddressBytes()[0] != 169);

        /// <summary>获取本地第一个IPv6地址</summary>
        /// <returns></returns>
        public static IPAddress MyIPv6() => GetIPsWithCache().FirstOrDefault(ip => !ip.IsIPv4() && !IPAddress.IsLoopback(ip));
        #endregion

        #region 远程开机
        /// <summary>唤醒指定MAC地址的计算机</summary>
        /// <param name="macs"></param>
        public static void Wake(params String[] macs)
        {
            if (macs == null || macs.Length <= 0) return;

            foreach (var item in macs)
                Wake(item);
        }

        private static void Wake(String mac)
        {
            mac = mac.Replace("-", null).Replace(":", null);
            var buffer = new Byte[mac.Length / 2];
            for (var i = 0; i < buffer.Length; i++)
                buffer[i] = Byte.Parse(mac.Substring(i * 2, 2), NumberStyles.HexNumber);

            var bts = new Byte[6 + 16 * buffer.Length];
            for (var i = 0; i < 6; i++)
                bts[i] = 0xFF;
            for (Int32 i = 6, k = 0; i < bts.Length; i++, k++)
            {
                if (k >= buffer.Length) k = 0;

                bts[i] = buffer[k];
            }

            var client = new UdpClient
            {
                EnableBroadcast = true
            };
            client.Send(bts, bts.Length, new IPEndPoint(IPAddress.Broadcast, 7));
            client.Close();
            //client.SendAsync(bts, bts.Length, new IPEndPoint(IPAddress.Broadcast, 7));
        }
        #endregion

        #region MAC获取/ARP协议
        [DllImport("Iphlpapi.dll")]
        private static extern Int32 SendARP(UInt32 destip, UInt32 srcip, Byte[] mac, ref Int32 length);

        /// <summary>根据IP地址获取MAC地址</summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public static Byte[] GetMac(this IPAddress ip)
        {
            // 考虑到IPv6是16字节，不确定SendARP是否支持IPv6
            var len = 16;
            var buf = new Byte[16];
            var rs = SendARP(ip.GetAddressBytes().ToUInt32(), 0, buf, ref len);
            if (rs != 0 || len <= 0) return null;

            if (len != buf.Length) buf = buf.ReadBytes(0, len);
            return buf;
        }
        #endregion

        #region IP地理位置
        ///// <summary>IP地址提供者</summary>
        //public static IIPResolver IpResolver { get; set; }

        ///// <summary>获取IP地址的物理地址位置</summary>
        ///// <param name="addr"></param>
        ///// <returns></returns>
        //public static String GetAddress(this IPAddress addr)
        //{
        //    if (addr.IsAny()) return "任意地址";
        //    if (IPAddress.IsLoopback(addr)) return "本地环回";
        //    if (addr.IsLocal()) return "本机地址";

        //    //if (IpProvider == null) IpProvider = new MyIpProvider();

        //    return IpResolver?.GetAddress(addr);
        //}

        ///// <summary>根据字符串形式IP地址转为物理地址</summary>
        ///// <param name="addr"></param>
        ///// <returns></returns>
        //public static String IPToAddress(this String addr)
        //{
        //    if (addr.IsNullOrEmpty()) return String.Empty;

        //    // 有可能是NetUri
        //    var p = addr.IndexOf("://");
        //    if (p >= 0) addr = addr.Substring(p + 3); //addr[(p + 3)..];

        //    // 有可能是多个IP地址
        //    p = addr.IndexOf(',');
        //    if (p >= 0) addr = addr.Split(',').FirstOrDefault();

        //    // 过滤IPv4/IPv6端口
        //    if (addr.Replace("::", "").Contains(':')) addr = addr.Substring(0, addr.LastIndexOf(":")); //addr[..addr.LastIndexOf(':')];

        //    return !IPAddress.TryParse(addr, out var ip) ? String.Empty : ip.GetAddress();
        //}
        #endregion
    }

    // <summary>网络资源标识，指定协议、地址、端口、地址族（IPv4/IPv6）</summary>
    /// <remarks>
    /// 仅序列化<see cref="Type"/>和<see cref="EndPoint"/>，其它均是配角！
    /// 有可能<see cref="Host"/>代表主机域名，而<see cref="Address"/>指定主机IP地址。
    /// </remarks>
    public class NetUri
    {
        #region 属性
        /// <summary>协议类型</summary>
        public NetType Type { get; set; }

        /// <summary>主机</summary>
        public String Host { get; set; }

        /// <summary>地址</summary>
        [XmlIgnore, IgnoreDataMember]
        public IPAddress Address { get { return EndPoint.Address; } set { EndPoint.Address = value; } }

        /// <summary>端口</summary>
        public Int32 Port { get { return EndPoint.Port; } set { EndPoint.Port = value; } }

        [NonSerialized]
        private IPEndPoint _EndPoint;
        /// <summary>终结点</summary>
        [XmlIgnore, IgnoreDataMember]
        public IPEndPoint EndPoint
        {
            get
            {
                var ep = _EndPoint;
                ep ??= _EndPoint = new IPEndPoint(IPAddress.Any, 0);
                if ((ep.Address == null || ep.Address.IsAny()) && !Host.IsNullOrEmpty()) ep.Address = NetHelper.ParseAddress(Host) ?? IPAddress.Any;

                return ep;
            }
            set
            {
                var ep = _EndPoint = value;
                Host = ep?.Address?.ToString();
            }
        }
        #endregion

        #region 扩展属性
        /// <summary>是否Tcp协议</summary>
        [XmlIgnore, IgnoreDataMember]
        public Boolean IsTcp => Type == NetType.Tcp;

        /// <summary>是否Udp协议</summary>
        [XmlIgnore, IgnoreDataMember]
        public Boolean IsUdp => Type == NetType.Udp;
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public NetUri() { }

        /// <summary>实例化</summary>
        /// <param name="uri"></param>
        public NetUri(String uri) => Parse(uri);

        /// <summary>实例化</summary>
        /// <param name="protocol"></param>
        /// <param name="endpoint"></param>
        public NetUri(NetType protocol, IPEndPoint endpoint)
        {
            Type = protocol;
            _EndPoint = endpoint;
        }

        /// <summary>实例化</summary>
        /// <param name="protocol"></param>
        /// <param name="address"></param>
        /// <param name="port"></param>
        public NetUri(NetType protocol, IPAddress address, Int32 port)
        {
            Type = protocol;
            Address = address;
            Port = port;
        }

        /// <summary>实例化</summary>
        /// <param name="protocol"></param>
        /// <param name="host"></param>
        /// <param name="port"></param>
        public NetUri(NetType protocol, String host, Int32 port)
        {
            Type = protocol;
            Host = host;
            Port = port;
        }
        #endregion

        #region 方法
        static readonly String Sep = "://";

        /// <summary>分析</summary>
        /// <param name="uri"></param>
        public NetUri Parse(String uri)
        {
            if (uri.IsNullOrWhiteSpace()) return this;

            // 分析协议
            var protocol = "";
            var array = uri.Split(Sep);
            if (array.Length >= 2)
            {
                protocol = array[0]?.Trim();
                Type = ParseType(protocol);
                uri = array[1]?.Trim();
            }

            Host = null;
            _EndPoint = null;

            // 特殊协议端口
            switch (protocol.ToLower())
            {
                case "http":
                case "ws":
                    Port = 80;
                    break;
                case "https":
                case "wss":
                    Port = 443;
                    break;
            }

            // 这个可能是一个Uri，去掉尾部
            var p = uri.IndexOf('/');
            if (p < 0) p = uri.IndexOf('\\');
            if (p < 0) p = uri.IndexOf('?');
            if (p >= 0) uri = uri.Substring(0, p)?.Trim(); //uri[..p]?.Trim();

            // 分析端口，冒号前一个不能是冒号
            p = uri.LastIndexOf(':');
            if (p >= 0 && (p < 1 || uri[p - 1] != ':'))
            {
                var pt = uri.Substring(p + 1); //uri[(p + 1)..];
                if (Int32.TryParse(pt, out var port))
                {
                    Port = port;
                    uri = uri.Substring(0, p)?.Trim(); //uri[..p]?.Trim();
                }
            }

            if (IPAddress.TryParse(uri, out var address))
                Address = address;
            else
                Host = uri;

            return this;
        }

        private static NetType ParseType(String value)
        {
            if (value.IsNullOrEmpty()) return NetType.Unknown;

            try
            {
                if (value.EqualIgnoreCase("Http", "Https")) return NetType.Http;
                if (value.EqualIgnoreCase("ws", "wss")) return NetType.WebSocket;

                return (NetType)(Int32)Enum.Parse(typeof(ProtocolType), value, true);
            }
            catch { return NetType.Unknown; }
        }

        /// <summary>获取该域名下所有IP地址</summary>
        /// <returns></returns>
        public IPAddress[] GetAddresses() => ParseAddress(Host) ?? new[] { Address };

        /// <summary>获取该域名下所有IP节点（含端口）</summary>
        /// <returns></returns>
        public IPEndPoint[] GetEndPoints() => GetAddresses().Select(e => new IPEndPoint(e, Port)).ToArray();
        #endregion

        #region 辅助
        /// <summary>分析地址</summary>
        /// <param name="hostname">主机地址</param>
        /// <returns></returns>
        public static IPAddress[] ParseAddress(String hostname)
        {
            if (hostname.IsNullOrEmpty()) return null;
            if (hostname == "*") return null;

            try
            {
                if (IPAddress.TryParse(hostname, out var addr)) return new[] { addr };

                var hostAddresses = Dns.GetHostAddresses(hostname);
                if (hostAddresses == null || hostAddresses.Length <= 0) return null;

                return hostAddresses.Where(d => d.AddressFamily is AddressFamily.InterNetwork || d.AddressFamily is AddressFamily.InterNetworkV6).ToArray();
            }
            catch (SocketException ex)
            {
                throw new XException("解析主机" + hostname + "的地址失败！" + ex.Message, ex);
            }
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString()
        {
            var protocol = Type.ToString().ToLower();
            switch (Type)
            {
                case NetType.Unknown:
                    protocol = "";
                    break;
                case NetType.WebSocket:
                    protocol = Port == 443 ? "wss" : "ws";
                    break;
            }
            var host = Host;
            if (host.IsNullOrEmpty())
            {
                if (Address.AddressFamily == AddressFamily.InterNetworkV6 && Port > 0)
                    host = $"[{Address}]";
                else
                    host = Address + "";
            }

            if (Port > 0)
                return $"{protocol}://{host}:{Port}";
            else
                return $"{protocol}://{host}";
        }
        #endregion

        #region 重载运算符
        /// <summary>重载类型转换，字符串直接转为NetUri对象</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static implicit operator NetUri(String value) => new NetUri(value);
        #endregion
    }

    /// <summary>Tcp连接信息</summary>
    public class TcpConnectionInformation2 : TcpConnectionInformation
    {
        /// <summary>本地结点</summary>
        public override IPEndPoint LocalEndPoint { get; }

        /// <summary>远程结点</summary>
        public override IPEndPoint RemoteEndPoint { get; }

        /// <summary>Tcp状态</summary>
        public override TcpState State { get; }

        /// <summary>进程标识</summary>
        public Int32 ProcessId { get; }

        /// <summary>实例化Tcp连接信息</summary>
        /// <param name="local"></param>
        /// <param name="remote"></param>
        /// <param name="state"></param>
        /// <param name="processId"></param>
        public TcpConnectionInformation2(IPEndPoint local, IPEndPoint remote, TcpState state, Int32 processId)
        {
            LocalEndPoint = local;
            RemoteEndPoint = remote;
            State = state;
            ProcessId = processId;
        }

        private TcpConnectionInformation2(MIB_TCPROW_OWNER_PID row)
        {
            State = (TcpState)row.state;
            var port = (row.localPort1 << 8) | row.localPort2;
            var port2 = (State != TcpState.Listen) ? ((row.remotePort1 << 8) | row.remotePort2) : 0;
            LocalEndPoint = new IPEndPoint(row.localAddr, port);
            RemoteEndPoint = new IPEndPoint(row.remoteAddr, port2);
            ProcessId = row.owningPid;
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString() => $"{LocalEndPoint}<=>{RemoteEndPoint} {State} {ProcessId}";

        private enum TCP_TABLE_CLASS : Int32
        {
            TCP_TABLE_BASIC_LISTENER,
            TCP_TABLE_BASIC_CONNECTIONS,
            TCP_TABLE_BASIC_ALL,
            TCP_TABLE_OWNER_PID_LISTENER,
            TCP_TABLE_OWNER_PID_CONNECTIONS,
            TCP_TABLE_OWNER_PID_ALL,
            TCP_TABLE_OWNER_MODULE_LISTENER,
            TCP_TABLE_OWNER_MODULE_CONNECTIONS,
            TCP_TABLE_OWNER_MODULE_ALL
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_TCPROW_OWNER_PID
        {
            public UInt32 state;
            public UInt32 localAddr;
            public Byte localPort1;
            public Byte localPort2;
            public Byte localPort3;
            public Byte localPort4;
            public UInt32 remoteAddr;
            public Byte remotePort1;
            public Byte remotePort2;
            public Byte remotePort3;
            public Byte remotePort4;
            public Int32 owningPid;

            public UInt16 LocalPort => BitConverter.ToUInt16(new Byte[2] { localPort2, localPort1 }, 0);

            public UInt16 RemotePort => BitConverter.ToUInt16(new Byte[2] { remotePort2, remotePort1 }, 0);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_TCPTABLE_OWNER_PID
        {
            public UInt32 dwNumEntries;
            private MIB_TCPROW_OWNER_PID table;
        }

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern UInt32 GetExtendedTcpTable(IntPtr pTcpTable,
            ref Int32 dwOutBufLen,
            Boolean sort,
            Int32 ipVersion,
            TCP_TABLE_CLASS tblClass,
            Int32 reserved);

        /// <summary>获取所有Tcp连接</summary>
        /// <returns></returns>
        public static TcpConnectionInformation2[] GetAllTcpConnections()
        {
            //MIB_TCPROW_OWNER_PID[] tTable;
            var AF_INET = 2;    // IP_v4
            var buffSize = 0;

            // how much memory do we need?
            var ret = GetExtendedTcpTable(IntPtr.Zero,
                ref buffSize,
                true,
                AF_INET,
                TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL,
                0);
            if (ret != 0 && ret != 122) // 122 insufficient buffer size
                throw new Exception("bad ret on check " + ret);
            var buffTable = Marshal.AllocHGlobal(buffSize);

            var list = new List<TcpConnectionInformation2>();
            try
            {
                ret = GetExtendedTcpTable(buffTable,
                    ref buffSize,
                    true,
                    AF_INET,
                    TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL,
                    0);
                if (ret != 0)
                    throw new Exception("bad ret " + ret);

                // get the number of entries in the table
                var tab =
                    (MIB_TCPTABLE_OWNER_PID)Marshal.PtrToStructure(
                        buffTable,
                        typeof(MIB_TCPTABLE_OWNER_PID));
                var rowPtr = (IntPtr)((Int64)buffTable +
                    Marshal.SizeOf(tab.dwNumEntries));
                //tTable = new MIB_TCPROW_OWNER_PID[tab.dwNumEntries];

                for (var i = 0; i < tab.dwNumEntries; i++)
                {
                    var tcpRow = (MIB_TCPROW_OWNER_PID)Marshal
                        .PtrToStructure(rowPtr, typeof(MIB_TCPROW_OWNER_PID));
                    //tTable[i] = tcpRow;
                    list.Add(new TcpConnectionInformation2(tcpRow));

                    // next entry
                    rowPtr = (IntPtr)((Int64)rowPtr + Marshal.SizeOf(tcpRow));
                }
            }
            finally
            {
                // Free the Memory
                Marshal.FreeHGlobal(buffTable);
            }
            //return tTable;
            return list.ToArray();
        }
    }
}
