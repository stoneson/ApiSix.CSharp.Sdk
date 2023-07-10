using ApiSix.CSharp.model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ApiSix.CSharp
{
    public abstract class BaseClient
    {
        public const int HTTP_OK = 200;
        public const int HTTP_NOT_OK = 400;
        public const String SDK_VERSION = "3.3.0";

        private Profile profile;
        private Credential credential;
        private String endpoint;
        private String sdkVersion;
        private String apiVersion;
        //public Gson gson;

        public BaseClient(Profile profile)
        {
            this.credential = profile.Credential;
            this.profile = profile;
            this.endpoint = profile.Endpoint;
            this.sdkVersion = BaseClient.SDK_VERSION;
            this.apiVersion = profile.Version;
        }
        public BaseClient(string endpoint, string apiKey = "edd1c9f034335f136f87ad84b625c8f1", string version = "3.3.0")
        {
            this.profile = DefaultProfile.getProfile(endpoint, version, apiKey);
            this.credential = profile.Credential;
            this.endpoint = profile.Endpoint;
            this.apiVersion = profile.Version;
            this.sdkVersion = BaseClient.SDK_VERSION;
        }
        public Profile getProfile()
        {
            return this.profile;
        }

        #region doRequest
        protected String doRequest(String reqMethod, String path)
        {
            var strResp = "";
            try
            {
                var strParam = "";
                strResp = doRequest(reqMethod, path, strParam);
            }
            catch (IOException e)
            {
                throw new ApisixSDKExcetion(e.GetType().FullName + "-" + e.Message);
            }
            return strResp;
        }
        protected String doRequest(BaseModel model, String reqMethod, String path)
        {
            var strResp = "";
            try
            {
                var strParam = model == null ? "" : model.SerializeObjectToJson();
                strResp = doRequest(reqMethod, path, strParam);
            }
            catch (IOException e)
            {
                throw new ApisixSDKExcetion(e.GetType().FullName + "-" + e.Message);
            }
            return strResp;
        }

        private string doRequest(String reqMethod, String path, String param)
        {
            var contentType = "application/json; charset=utf-8";
            var url = this.profile.HttpProfile.Protocol + this.endpoint + path;

            var headers = new Dictionary<string, string>(){
                { "Content-Type", contentType }
               ,{"Host", this.endpoint }
               ,{"X-API-Version", this.apiVersion }
               ,{ "X-SDK-RequestClient", this.sdkVersion } };

            var token = this.credential.Token;
            if (!string.IsNullOrWhiteSpace(token))
            {
                headers["X-API-KEY"] = token;
            }

            if (reqMethod.Equals(HttpProfile.REQ_GET))
            {
                return RestClient.HttpRequestGET(url + "?" + param, contentType, null, headers);
            }
            else if (reqMethod.Equals(HttpProfile.REQ_POST))
            {
                return RestClient.HttpRequestPOST(url, param, contentType, headers);
            }
            else if (reqMethod.Equals(HttpProfile.REQ_DELETE))
            {
                return RestClient.HttpRequestDELETE(url, param, contentType, headers);
            }
            else if (reqMethod.Equals(HttpProfile.REQ_PUT))
            {
                return RestClient.HttpRequestPUT(url, param, contentType, headers);
            }
            else
            {
                throw new ApisixSDKExcetion("Method only support (GET, POST, PUT, DELETE)");
            }
        }
        #endregion

        #region get/delete/put/post Model
        protected List<T> arrangeMulti<T>(List<Item<T>> list)
        {
            Item<T> item;
            T model;
            List<T> result = new List<T>();

            if (list != null)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    item = list[i];
                    model = item.value;
                    result.Add(model);
                }
            }
            return result;
        }
        /// <summary>
        /// 分页获取资源列表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <param name="page">页数，默认展示第一页</param>
        /// <param name="pageSize">每页资源数量。如果不配置该参数，则展示所有查询到的资源</param>
        /// <returns></returns>
        /// <exception cref="ApisixSDKExcetion"></exception>
        protected Multi<T> getPageList<T>(String path, int page = 1, int pageSize = 15) where T : BaseModel
        {
            Multi<T> rsp = null;
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 10) page = 10;
                if (pageSize > 50) page = 50;
                var ress = this.doRequest(HttpProfile.REQ_GET, $"/apisix/admin/{path}?page={page}&page_size={pageSize}");
                rsp = ress.DeserializeObjectByJson<Multi<T>>();
            }
            catch (Exception e)
            {
                if (e is ApisixSDKExcetion ex)
                {
                    if (!ex.getErrorCode().Equals(600))
                        return new Multi<T>() { code = ex.getErrorCode(), msg = ex.Message };
                    throw ex;
                }
                else
                {
                    throw new ApisixSDKExcetion(e.Message);
                }
            }
            return rsp;
        }
        /// <summary>
        /// 获取资源列表
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ApisixSDKExcetion"></exception>
        protected Multi<T> getlist<T>(String path) where T : BaseModel
        {
            Multi<T> rsp = null;
            try
            {
                var ress = this.doRequest(HttpProfile.REQ_GET, $"/apisix/admin/{path}");
                rsp = ress.DeserializeObjectByJson<Multi<T>>();
            }
            catch (Exception e)
            {
                if (e is ApisixSDKExcetion ex)
                {
                    if (!ex.getErrorCode().Equals(600))
                        return new Multi<T>() { code = ex.getErrorCode(), msg = ex.Message };
                    throw ex;
                }
                else
                {
                    throw new ApisixSDKExcetion(e.Message);
                }
            }
            //var result = this.arrangeMulti(rsp.list);
            return rsp;
        }

        /// <summary>
        /// 按id获取资源。
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="ApisixSDKExcetion"></exception>
        protected Item<T> getById<T>(String id, String path) where T : BaseModel
        {
            Item<T> rsp = null;
            try
            {
                var ress = this.doRequest(HttpProfile.REQ_GET, $"/apisix/admin/{path}/" + id);
                rsp = ress.DeserializeObjectByJson<Item<T>>();
            }
            catch (Exception e)
            {
                if (e is ApisixSDKExcetion ex)
                {
                    if (!ex.getErrorCode().Equals(600))
                        return new Item<T>() { code = ex.getErrorCode(), msg = ex.Message };
                    throw ex;
                }
                else
                {
                    throw new ApisixSDKExcetion(e.Message);
                }
            }
            return rsp;
        }

        /// <summary>
        /// 删除指定id的upstream
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool deleteById(String id, String path)
        {
            try
            {
                this.doRequest(HttpProfile.REQ_DELETE, $"/apisix/admin/{path}/" + id);
                return true;
            }
            catch (Exception e)
            {
                if (e is ApisixSDKExcetion ex)
                {
                    if (!ex.getErrorCode().Equals(600))
                        return false;
                    throw ex;
                }
                else
                {
                    throw new ApisixSDKExcetion(e.Message);
                }
            }
        }

        /// <summary>
        /// 创建指定 id 的资源。
        /// </summary>
        /// <param name="id"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <exception cref="ApisixSDKExcetion"></exception>
        public Item<T> putById<T>(String id, T model, String path) where T : BaseModel
        {
            Item<T> rsp = null;
            try
            {
                var ress = this.doRequest(model, HttpProfile.REQ_PUT, $"/apisix/admin/{path}/" + id);
                rsp = ress.DeserializeObjectByJson<Item<T>>();
            }
            catch (Exception e)
            {
                if (e is ApisixSDKExcetion ex)
                {
                    if (!ex.getErrorCode().Equals(600))
                        return new Item<T>() { code = ex.getErrorCode(), msg = ex.Message };
                    throw ex;
                }
                else
                {
                    throw new ApisixSDKExcetion(e.Message);
                }
            }
            return rsp;
        }

        /// <summary>
        /// 创建资源，id 由后台服务自动生成。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <exception cref="ApisixSDKExcetion"></exception>
        public Item<T> postSave<T>(T model, String path) where T : BaseModel
        {
            Item<T> rsp = null;
            try
            {
                var ress = this.doRequest(model, HttpProfile.REQ_POST, $"/apisix/admin/{path}/");
                rsp = ress.DeserializeObjectByJson<Item<T>>();
            }
            catch (Exception e)
            {
                if (e is ApisixSDKExcetion ex)
                {
                    if (!ex.getErrorCode().Equals(600))
                        return new Item<T>() { code = ex.getErrorCode(), msg = ex.Message };
                    throw ex;
                }
                else
                {
                    throw new ApisixSDKExcetion(e.Message);
                }
            }
            return rsp;
        }
        /// <summary>
        /// 标准 PATCH，修改已有 对象 的部分属性，其他不涉及的属性会原样保留；
        /// 如果需要删除某个属性，可将该属性的值设置为 null；
        /// 注意：当需要修改属性的值为数组时，该属性将全量更新。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <param name="model"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="ApisixSDKExcetion"></exception>
        public Item<T> patchById<T>(String id, object model, String path) where T : BaseModel
        {
            Item<T> rsp = null;
            try
            {
                var strParam = model == null ? "" : model.SerializeObjectToJson();
                var strResp = doRequest(HttpProfile.REQ_PATCH, $"/apisix/admin/{path}/" + id, strParam);
                rsp = strResp.DeserializeObjectByJson<Item<T>>();
                //var ress = this.doRequest(model, HttpProfile.REQ_PATCH, $"/apisix/admin/{path}/" + id);
                //rsp = ress.DeserializeObjectByJson<Item<T>>();
            }
            catch (Exception e)
            {
                if (e is ApisixSDKExcetion ex)
                {
                    if (!ex.getErrorCode().Equals(600))
                        return new Item<T>() { code = ex.getErrorCode(), msg = ex.Message };
                    throw ex;
                }
                else
                {
                    throw new ApisixSDKExcetion(e.Message);
                }
            }
            return rsp;
        }
        #endregion
    }
}
