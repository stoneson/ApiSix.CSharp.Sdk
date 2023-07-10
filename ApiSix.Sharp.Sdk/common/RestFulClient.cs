
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using static System.Net.WebRequestMethods;

namespace ApiSix.Sharp
{
    //public class HttpClientHelper
    //{
    //    private static readonly object LockObj = new object();
    //    private static HttpClient client = null;
    //    public HttpClientHelper()
    //    {
    //        GetInstance();
    //    }
    //    public static HttpClient GetInstance()
    //    {
    //        if (client == null)
    //        {
    //            lock (LockObj)
    //            {
    //                if (client == null)
    //                {
    //                    //var socketsHttpHandler = new SocketsHttpHandler()
    //                    //{
    //                    //    AllowAutoRedirect = true,// 默认为true,是否允许重定向
    //                    //    MaxAutomaticRedirections = 50,//最多重定向几次,默认50次
    //                    //                                  //MaxConnectionsPerServer = 100,//连接池中统一TcpServer的最大连接数
    //                    //    UseCookies = false,// 是否自动处理cookie

    //                    //    //每个请求连接的最大数量，默认是int.MaxValue,可以认为是不限制
    //                    //    MaxConnectionsPerServer = 100,
    //                    //    //连接池中TCP连接最多可以闲置多久,默认2分钟
    //                    //    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
    //                    //    //连接最长的存活时间,默认是不限制的,一般不用设置
    //                    //    PooledConnectionLifetime = Timeout.InfiniteTimeSpan,

    //                    //    //默认是None，即不压缩
    //                    //    //AutomaticDecompression = DecompressionMethods.GZip,

    //                    //    //建立TCP连接时的超时时间,默认不限制
    //                    //    ConnectTimeout = Timeout.InfiniteTimeSpan,
    //                    //    //等待服务返回statusCode=100的超时时间,默认1秒
    //                    //    Expect100ContinueTimeout = TimeSpan.FromSeconds(1),

    //                    //   // MaxResponseHeadersLength = 64, //单位: KB
    //                    //};
    //                    client = new HttpClient();
    //                }
    //            }
    //        }
    //        return client;
    //    }
    //    public async Task<string> PostAsync(string url, string strJson)//post异步请求方法
    //    {
    //        try
    //        {
    //            HttpContent content = new StringContent(strJson);
    //            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
    //            //由HttpClient发出异步Post请求
    //            HttpResponseMessage res = await client.PostAsync(url, content);
    //            if (res.StatusCode == System.Net.HttpStatusCode.OK)
    //            {
    //                string str = res.Content.ReadAsStringAsync().Result;
    //                return str;
    //            }
    //            else
    //                return null;
    //        }
    //        catch (Exception)
    //        {
    //            return null;
    //        }
    //    }

    //    public string Post(string url, string strJson)//post同步请求方法
    //    {
    //        try
    //        {
    //            HttpContent content = new StringContent(strJson);
    //            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
    //            //client.DefaultRequestHeaders.Connection.Add("keep-alive");
    //            //由HttpClient发出Post请求
    //            Task<HttpResponseMessage> res = client.PostAsync(url, content);
    //            if (res.Result.StatusCode == System.Net.HttpStatusCode.OK)
    //            {
    //                string str = res.Result.Content.ReadAsStringAsync().Result;
    //                return str;
    //            }
    //            else
    //                return null;
    //        }
    //        catch (Exception)
    //        {
    //            return null;
    //        }
    //    }

    //    public string Get(string url)
    //    {
    //        try
    //        {
    //            var responseString = client.GetStringAsync(url);
    //            return responseString.Result;
    //        }
    //        catch (Exception)
    //        {
    //            return null;
    //        }
    //    }

    // }
    /// <summary>
    /// 请求类型
    /// </summary>
    public enum EnumHttpVerb
    {
        GET,
        POST,
        PUT,
        DELETE
    }

    public class RestClient
    {
        #region 属性
        public static string BaseDirectory
        {
            get
            {
                string location = AppDomain.CurrentDomain.BaseDirectory.Substring(0, AppDomain.CurrentDomain.BaseDirectory.LastIndexOf('\\'));
                return location;
            }
        }
        /// <summary>
        /// 端点路径
        /// </summary>
        public string EndPoint { get; set; }

        /// <summary>
        /// 请求方式
        /// </summary>
        public EnumHttpVerb Method { get; set; }

        /// <summary>
        /// 文本类型（1、application/json 2、txt/html）
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// 请求的数据(一般为JSon格式)
        /// </summary>
        public string PostData { get; set; }

        ///// <summary>
        ///// 是否证书验证
        ///// </summary>
        //public static bool IsSsl { get; set; } = false;
        ///// <summary>
        ///// 验证证书文件
        ///// </summary>
        //public static string PfxFile { get; set; }
        ///// <summary>
        ///// 验证证书密码
        ///// </summary>
        //public static string Pfxkey { get; set; }
        #endregion

        #region 初始化
        public RestClient()
        {
            EndPoint = "";
            Method = EnumHttpVerb.GET;
            ContentType = "application/json";
            PostData = "";
        }

        public RestClient(string endpoint)
        {
            EndPoint = endpoint;
            Method = EnumHttpVerb.GET;
            ContentType = "application/json";
            PostData = "";
        }

        public RestClient(string endpoint, EnumHttpVerb method)
        {
            EndPoint = endpoint;
            Method = method;
            ContentType = "application/json";
            PostData = "";
        }

        public RestClient(string endpoint, EnumHttpVerb method, string postData)
        {
            EndPoint = endpoint;
            Method = method;
            ContentType = "application/json";
            PostData = postData;
        }
        #endregion

        #region 方法
        /// <summary>
        /// http请求(不带参数请求)
        /// </summary>
        /// <returns></returns>
        public string HttpRequest()
        {
            return HttpRequest("");
        }

        /// <summary>
        /// http请求(带参数)
        /// </summary>
        /// <param name="parameters">parameters例如：?name=LiLei</param>
        /// <returns></returns>
        public string HttpRequest(string parameters)
        {
            return HttpRequest(Method, EndPoint + parameters, PostData, ContentType);

            //var request = (HttpWebRequest)WebRequest.Create(EndPoint + parameters);

            //request.Method = Method.ToString();
            //request.ContentLength = 0;
            //request.ContentType = ContentType;

            //if (!string.IsNullOrWhiteSpace(PostData) && Method != EnumHttpVerb.POST)
            //{
            //    try
            //    {
            //        var bytes = Encoding.UTF8.GetBytes(PostData);
            //        request.ContentLength = bytes.Length;

            //        //创建输入流
            //        using (var writeStream = request.GetRequestStream())
            //        {
            //            writeStream.Write(bytes, 0, bytes.Length);
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        return ex.ToString();//连接服务器失败
            //    }
            //}
            ////------------------------------读取返回消息-----------------------------------------------------------------
            //return GetResponseAsString(request, Encoding.UTF8);
        }
        #endregion

        #region HttpRequest GET/POST/PUT/DELETE
        /// <summary>
        /// Http (GET)
        /// </summary>
        /// <param name="url">请求URL</param>
        /// <param name="contentType">HTTP 标头的值</param>
        /// <param name="parameters">请求参数</param>
        /// <param name="headers">HTTP 标头的名称/值对的集合</param>
        /// <returns>响应内容</returns>
        public static string HttpRequestGET(string url, string contentType = "application/json"
            , IDictionary<string, string> parameters = null, IDictionary<string, string> headers = null, string webProxyAddress = "")
        {
            return HttpRequest(EnumHttpVerb.GET, url, "", contentType, parameters, headers, webProxyAddress);
        }
        /// <summary>
        /// Http (POST)
        /// </summary>
        /// <param name="url">请求URL</param>
        /// <param name="postData">Post 数据</param>
        /// <param name="contentType">HTTP 标头的值</param>
        /// <param name="headers">HTTP 标头的名称/值对的集合</param>
        /// <returns>响应内容</returns>
        public static string HttpRequestPOST(string url, string postData, string contentType = "application/json"
            , IDictionary<string, string> headers = null, string webProxyAddress = "")
        {
            return HttpRequest(EnumHttpVerb.POST, url, postData, contentType, null, headers, webProxyAddress);
        }
        /// <summary>
        /// Http (PUT)
        /// </summary>
        /// <param name="url">请求URL</param>
        /// <param name="postData">Post 数据</param>
        /// <param name="contentType">TTP 标头的值</param>
        /// <param name="headers">HTTP 标头的名称/值对的集合</param>
        /// <returns>响应内容</returns>
        public static string HttpRequestPUT(string url, string postData, string contentType = "application/json"
            , IDictionary<string, string> headers = null, string webProxyAddress = "")
        {
            return HttpRequest(EnumHttpVerb.PUT, url, postData, contentType, null, headers, webProxyAddress);
        }
        /// <summary>
        ///  Http (DELETE)
        /// </summary>
        /// <param name="url">请求URL</param>
        /// <param name="postData">Post 数据</param>
        /// <param name="contentType">HTTP 标头的值</param>
        /// <param name="headers">HTTP 标头的名称/值对的集合</param>
        /// <returns>响应内容</returns>
        public static string HttpRequestDELETE(string url, string postData, string contentType = "application/json"
            , IDictionary<string, string> headers = null, string webProxyAddress = "")
        {
            return HttpRequest(EnumHttpVerb.DELETE, url, postData, contentType, null, headers, webProxyAddress);
        }
        /// <summary>
        /// Http (GET/POST/PUT/DELETE)
        /// </summary>
        /// <param name="method">请求方法</param>
        /// <param name="url">请求URL</param>
        /// <param name="postData">Post 数据</param>
        /// <param name="contentType">HTTP 标头的值</param>
        /// <param name="parameters">请求参数</param>
        /// <param name="headers">HTTP 标头的名称/值对的集合</param>
        /// <returns>响应内容</returns>
        public static string HttpRequest(EnumHttpVerb method, string url, string postData, string contentType = "application/json"
            , IDictionary<string, string> parameters = null, IDictionary<string, string> headers = null, string webProxyAddress = "")
        {
            //设置请求参数
            if (parameters != null && parameters.Count > 0)
            {
                url += (url.Contains("?") ? "&" : "?") + BuildQuery(parameters, Encoding.UTF8);
            }
            var request = (HttpWebRequest)WebRequest.Create(url);
            //设置HTTP 标头的名称/值对的集合
            if (headers != null && headers.Count > 0)
            {
                BuildHeader(request, headers);
            }
            //设置代理
            if (!string.IsNullOrWhiteSpace(webProxyAddress))
            {
                var proxy = new WebProxy(webProxyAddress);//IP地址 port为端口号 代理类
                request.Proxy = proxy;
            }
            request.ProtocolVersion = HttpVersion.Version11;
            request.Method = method.ToString();
            request.ContentLength = 0;
            request.ContentType = contentType;
            //--------------------------------SSL---------------------------------------------
            //if (IsSsl && !string.IsNullOrWhiteSpace(PfxFile))
            //{
            //    var cerCaiShang = new X509Certificate(PfxFile, Pfxkey);
            //    request.ClientCertificates.Add(cerCaiShang);
            //}
            //-----------------------------------------------------------------------------
            if (!string.IsNullOrWhiteSpace(postData) && method != EnumHttpVerb.GET)
            {
                //try
                {
                    var bytes = Encoding.UTF8.GetBytes(postData);
                    request.ContentLength = bytes.Length;

                    //创建输入流
                    using (var writeStream = request.GetRequestStream())
                    {
                        writeStream.Write(bytes, 0, bytes.Length);
                    }
                }
                // catch (Exception ex)
                // {
                //     return ex.ToString();//连接服务器失败
                // }
            }
            //-------------------------读取返回消息----------------------------------------------------------------------
            return GetResponseAsString(request, Encoding.UTF8);
        }
        static void BuildHeader(HttpWebRequest request, IDictionary<string, string> headers)
        {
            if (request == null) return;
            IEnumerator<KeyValuePair<string, string>> dem = headers.GetEnumerator();
            while (dem.MoveNext())
            {
                string name = dem.Current.Key;
                string value = dem.Current.Value;
                if (name.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                {
                    request.ContentType = value;
                    continue;
                }
                else if (name.Equals("Host", StringComparison.OrdinalIgnoreCase))
                {
                    request.Host = value;
                    continue;
                }
                // 忽略参数名或参数值为空的参数
                if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(value))
                {
                    request.Headers[name] = value;
                    //if (request.Headers.AllKeys.Contains(name))
                    //    request.Headers.Set(name, value);
                    //else
                    //    request.Headers.Add(name, value);
                }
            }
        }
        /// <summary>
        /// 组装普通文本请求参数。
        /// </summary>
        /// <param name="parameters">Key-Value形式请求参数字典</param>
        /// <returns>URL编码后的请求数据</returns>
        static string BuildQuery(IDictionary<string, string> parameters, Encoding encode = null)
        {
            StringBuilder postData = new StringBuilder();
            bool hasParam = false;
            IEnumerator<KeyValuePair<string, string>> dem = parameters.GetEnumerator();
            while (dem.MoveNext())
            {
                string name = dem.Current.Key;
                string value = dem.Current.Value;
                // 忽略参数名或参数值为空的参数
                if (!string.IsNullOrWhiteSpace(name))//&& !string.IsNullOrWhiteSpace(value)
                {
                    if (hasParam)
                    {
                        postData.Append("&");
                    }
                    postData.Append(name);
                    postData.Append("=");
                    if (encode == null)
                    {
                        postData.Append(HttpUtility.UrlEncode(value, encode));
                    }
                    else
                    {
                        postData.Append(value);
                    }
                    hasParam = true;
                }
            }
            return postData.ToString();
        }

        /// <summary>
        /// 把响应流转换为文本。
        /// </summary>
        /// <param name="rsp">响应流对象</param>
        /// <param name="encoding">编码方式</param>
        /// <returns>响应文本</returns>
        static string GetResponseAsString(HttpWebRequest request, Encoding encoding)
        {
            HttpWebResponse response;
            try
            {
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException ex)
            {
                //return ex.Message;
                if (ex.Response == null) { throw new ApisixSDKExcetion(ex); }
                response = (HttpWebResponse)ex.Response;
            }
            catch (Exception ex)
            {
                throw new ApisixSDKExcetion(ex);
                //return ex.ToString();
                //连接服务器失败
            }
            //读取返回消息
            string res = string.Empty;
            // try
            //{
            //判断HTTP响应状态 
            if (response == null || response.StatusCode.GetHashCode() >= 400)// response.StatusCode != HttpStatusCode.OK
            {
                var code = response.StatusCode;
                res = " 访问失败:Response.StatusCode=" + code;//连接服务器失败
                response.Close();

                throw new ApisixSDKExcetion(res, code.GetHashCode());
            }
            else
            {
                StreamReader reader = new StreamReader(response.GetResponseStream(), encoding);
                res = reader.ReadToEnd();
                reader.Close();
            }
            //}
            //catch (Exception ex)
            //{
            //    res = ex.ToString();
            //连接服务器失败
            //}
            return res;
        }
        #endregion

        #region SetCertificatePolicy
        static RestClient()
        {
            SetCertificatePolicy();
        }
        /// <summary>
        /// Sets the cert policy.
        /// </summary>
        public static void SetCertificatePolicy()
        {
            //添加验证证书的回调方法
            ServicePointManager.ServerCertificateValidationCallback = ValidateServerCertificate;
            // 这里设置了协议类型。
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)192 | (SecurityProtocolType)768 | (SecurityProtocolType)3072; //(SecurityProtocolType)3072 | SecurityProtocolType.Tls | SecurityProtocolType.Ssl3; //SecurityProtocolType.Tls;// (SecurityProtocolType)3072; // 
                                                                                                                                       //ServicePointManager.CheckCertificateRevocationList = true;
                                                                                                                                       //ServicePointManager.DefaultConnectionLimit = 100;
                                                                                                                                       //ServicePointManager.Expect100Continue = false;
        }

        /// <summary>
        /// Remotes the certificate validate.
        /// </summary>
        private static bool ValidateServerCertificate(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors error)
        {
            return true;
        }
        #endregion

        #region 读/写文件
        /// <summary>
        /// 读取文件
        /// </summary>
        /// <param name="path">E:\\test.txt</param>
        /// <returns></returns>
        public static string Read(string path)
        {
            var rtStr = "";
            try
            {
                var pat = System.IO.Path.GetDirectoryName(path);
                if (!System.IO.Directory.Exists(pat)) System.IO.Directory.CreateDirectory(pat);
                StreamReader sr = new StreamReader(path, Encoding.Default);
                System.String line;
                while ((line = sr.ReadLine()) != null)
                {
                    rtStr += line.ToString();
                }
            }
            catch (IOException e)
            {
                rtStr = e.ToString();
            }
            return rtStr;
        }

        /// <summary>
        /// 写入文件
        /// </summary>
        /// <param name="path"></param>
        /// <param name="content"></param>
        public static void Write(string path, string content)
        {
            var pat = System.IO.Path.GetDirectoryName(path);
            if (!System.IO.Directory.Exists(pat)) System.IO.Directory.CreateDirectory(pat);
            FileStream fs = new FileStream(path, FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);
            //开始写入
            sw.Write(content);
            //清空缓冲区
            sw.Flush();
            //关闭流
            sw.Close();
            fs.Close();
        }
        #endregion
    }
}