using ApiSix.Sharp.model;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;

namespace ApiSix.Sharp
{
    /// <summary>
    /// Admin API 是一组用于配置 Apache APISIX 路由、上游、服务、SSL 证书等功能的 RESTful API。
    /// 你可以通过 Admin API 来获取、创建、更新以及删除资源。同时得益于 APISIX 的热加载能力，资源配置完成后 APISIX 将会自动更新配置，无需重启服务。
    /// 如果你想要了解其工作原理，请参考 https://apisix.apache.org/zh/docs/apisix/architecture-design/apisix/。
    /// 
    /// https://apisix.apache.org/zh/docs/apisix/admin-api/
    /// </summary>
    public class AdminClient : BaseClient
    {
        public AdminClient(Profile profile) : base(profile)
        {
        }
        public AdminClient(string endpoint, string apiKey = "edd1c9f034335f136f87ad84b625c8f1", string version = "3.3.0")
            : base(endpoint, version, apiKey)
        {
        }

        #region routes
        /// <summary>
        /// 路由列表
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ApisixSDKExcetion"></exception>
        public Multi<Route> listRoutes()
        {
            return this.getlist<Route>("routes");
        }

        /// <summary>
        /// 按id获取路由
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="ApisixSDKExcetion"></exception>
        public Item<Route>  getRoute(String id)
        {
            return this.getById<Route>(id, "routes");
        }

        /// <summary>
        /// 删除指定id的路由
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool deleteRoute(String id)
        {
            return this.deleteById(id, "routes");
        }

        /// <summary>
        /// 按指定id创建路由
        /// </summary>
        /// <param name="id"></param>
        /// <param name="route"></param>
        /// <returns></returns>
        /// <exception cref="ApisixSDKExcetion"></exception>
        public Item<Route> putRoute(String id, Route route)
        {
            try
            {
                route = resolveUpstream(route);
                return this.putById(id, route, "routes");
            }
            catch (ApisixSDKExcetion e)
            {
                if (e is ApisixSDKExcetion)
                {
                    throw e;
                }
                else
                {
                    throw new ApisixSDKExcetion(e.Message);
                }
            }
        }
        /// <summary>
        /// 按指定id修改已有 route 的部分属性路由
        /// </summary>
        /// <param name="id"></param>
        /// <param name="route"></param>
        /// <returns></returns>
        /// <exception cref="ApisixSDKExcetion"></exception>
        public Item<Route> patchRoute(String id, object route)
        {
            try
            {
                //route = resolveUpstream(route);
                return this.patchById<Route>(id, route, "routes");
            }
            catch (ApisixSDKExcetion e)
            {
                if (e is ApisixSDKExcetion)
                {
                    throw e;
                }
                else
                {
                    throw new ApisixSDKExcetion(e.Message);
                }
            }
        }

        /// <summary>
        /// 自动生成id创建路由
        /// </summary>
        /// <param name="route"></param>
        /// <returns></returns>
        /// <exception cref="ApisixSDKExcetion"></exception>
        public Item<Route> postRoute(Route route)
        {
            try
            {
                route = resolveUpstream(route);
                return this.postSave(route, "routes");
            }
            catch (ApisixSDKExcetion e)
            {
                if (e is ApisixSDKExcetion)
                {
                    throw e;
                }
                else
                {
                    throw new ApisixSDKExcetion(e.Message);
                }
            }
        }

        private Route resolveUpstream(Route route)
        {
            var upstream = route.upstream;
            if (upstream != null)
            {
                var k8sDeploymentInfo = upstream.k8sDeploymentInfo;
                //k8s deployment info is not empty
                if (k8sDeploymentInfo != null)
                {
                    var Namespace = k8sDeploymentInfo.Namespace;
                    var deployName = k8sDeploymentInfo.deployName;
                    var serviceName = k8sDeploymentInfo.serviceName;
                    if (deployName != null && !deployName.Equals("") || (serviceName != null && !serviceName.Equals("")))
                    {
                        var upstreamId = "pod".Equals(k8sDeploymentInfo.backendType) ?
                                Namespace + "-" + deployName + "-" + k8sDeploymentInfo.port :
                                Namespace + "-" + serviceName + "-" + k8sDeploymentInfo.port;
                        var res = putUpstream(upstreamId, upstream);
                        if (k8sDeploymentInfo.ToString().Equals(res.value.k8sDeploymentInfo.ToString()))
                        {
                            //replace to upstream id
                            route.upstream = null;
                            route.upstreamId = upstreamId;
                        }
                    }
                }
            }
            return route;
        }
        #endregion

        #region Services
        private Service resolveUpstream(Service service)
        {
            var upstream = service.upstream;
            if (upstream != null)
            {
                var k8sDeploymentInfo = upstream.k8sDeploymentInfo;
                //k8s deployment info is not empty
                if (k8sDeploymentInfo != null)
                {
                    var Namespace = k8sDeploymentInfo.Namespace;
                    var deployName = k8sDeploymentInfo.deployName;
                    var serviceName = k8sDeploymentInfo.serviceName;
                    if (deployName != null && !deployName.Equals("") || (serviceName != null && !serviceName.Equals("")))
                    {
                        var upstreamId = "pod".Equals(k8sDeploymentInfo.backendType) ?
                                Namespace + "-" + deployName + "-" + k8sDeploymentInfo.port :
                                Namespace + "-" + serviceName + "-" + k8sDeploymentInfo.port;
                        var res = putUpstream(upstreamId, upstream);
                        if (k8sDeploymentInfo.ToString().Equals(res.value.k8sDeploymentInfo.ToString()))
                        {
                            //replace to upstream id
                            service.upstream = null;
                            service.upstreamId = upstreamId;
                        }
                    }
                }
            }
            return service;
        }
        /// <summary>
        /// service列表
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ApisixSDKExcetion"></exception>
        public Multi<Service> listServices()
        {
            return this.getlist<Service>("services");
        }

        /// <summary>
        /// 按id获取service
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="ApisixSDKExcetion"></exception>
        public Item<Service> getService(String id)
        {
            return this.getById<Service>(id, "services");
        }

        /// <summary>
        /// 删除指定id的service
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool deleteService(String id)
        {
            return this.deleteById(id, "services");
        }

        /// <summary>
        /// 按指定id创建service
        /// </summary>
        /// <param name="id"></param>
        /// <param name="service"></param>
        /// <returns></returns>
        /// <exception cref="ApisixSDKExcetion"></exception>
        public Item<Service> putService(String id, Service service)
        {
            try
            {
                service = resolveUpstream(service);
                return this.putById(id, service, "services");
            }
            catch (ApisixSDKExcetion e)
            {
                if (e is ApisixSDKExcetion)
                {
                    throw e;
                }
                else
                {
                    throw new ApisixSDKExcetion(e.Message);
                }
            }
        }

        /// <summary>
        /// 自动生成id创建service
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        /// <exception cref="ApisixSDKExcetion"></exception>
        public Item<Service> postService(Service service)
        {
            try
            {
                service = resolveUpstream(service);
                return this.postSave(service, "services");
            }
            catch (ApisixSDKExcetion e)
            {
                if (e is ApisixSDKExcetion)
                {
                    throw e;
                }
                else
                {
                    throw new ApisixSDKExcetion(e.Message);
                }
            }
        }
        /// <summary>
        /// 按指定id修改已有 Service 的部分属性路由
        /// </summary>
        /// <param name="id"></param>
        /// <param name="service"></param>
        /// <returns></returns>
        /// <exception cref="ApisixSDKExcetion"></exception>
        public Item<Service> patchService(String id, object service)
        {
            try
            {
                //service = resolveUpstream(service);
                return this.patchById<Service>(id, service, "services");
            }
            catch (ApisixSDKExcetion e)
            {
                if (e is ApisixSDKExcetion)
                {
                    throw e;
                }
                else
                {
                    throw new ApisixSDKExcetion(e.Message);
                }
            }
        }
        #endregion

        #region upstreams
        /// <summary>
        /// upstream列表
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ApisixSDKExcetion"></exception>
        public Multi<Upstream> listUpstreams()
        {
            return this.getlist<Upstream>("upstreams");
        }

        /// <summary>
        /// 按id获取upstream
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="ApisixSDKExcetion"></exception>
        public Item<Upstream> getUpstream(String id)
        {
            return this.getById<Upstream>(id, "upstreams");
        }

        /// <summary>
        /// 删除指定id的upstream
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool deleteUpstream(String id)
        {
            return this.deleteById(id, "upstreams");
        }

        /// <summary>
        /// 按指定id创建upstream
        /// </summary>
        /// <param name="id"></param>
        /// <param name="upstream"></param>
        /// <returns></returns>
        /// <exception cref="ApisixSDKExcetion"></exception>
        public Item<Upstream> putUpstream(String id, Upstream upstream)
        {
            return this.putById(id, upstream, "upstreams");
        }

        /// <summary>
        /// 自动生成id创建upstream
        /// </summary>
        /// <param name="upstream"></param>
        /// <returns></returns>
        /// <exception cref="ApisixSDKExcetion"></exception>
        public Item<Upstream> postUpstream(Upstream upstream)
        {
            return this.postSave(upstream, "upstreams");
        }
        /// <summary>
        /// 按指定id修改已有 Upstream 的部分属性路由
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ApisixSDKExcetion"></exception>
        public Item<Upstream> patchUpstream(String id, object upstream)
        {
            return this.patchById<Upstream>(id, upstream, "upstreams");
        }
        #endregion

        #region consumers
        /// <summary>
        /// consumer列表
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ApisixSDKExcetion"></exception>
        public Multi<Consumer> listConsumers()
        {
            return this.getlist<Consumer>("consumers");
        }

        /// <summary>
        /// 按id获取consumer
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public Item<Consumer> getConsumer(String username)
        {
            return this.getById<Consumer>(username, "consumers");
        }

        /// <summary>
        /// 删除指定id的consumer
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public bool deleteConsumer(String username)
        {
            return this.deleteById(username, "consumers");
        }

        /// <summary>
        /// 按指定id创建consumer
        /// </summary>
        /// <param name="username"></param>
        /// <param name="consumer"></param>
        /// <returns></returns>
        public Item<Consumer> putConsumer(String username, Consumer consumer)
        {
            return this.putById(username, consumer, "consumers");
        }

        ///// <summary>
        ///// 自动生成id创建consumer
        ///// </summary>
        ///// <param name="consumer"></param>
        ///// <returns></returns>
        //public Item<Consumer> postConsumer(Consumer consumer)
        //{
        //    return this.postSave(consumer, "consumers");
        //}
        #endregion

        #region ssls
        /// <summary>
        /// ssl列表
        /// </summary>
        /// <returns></returns>
        public Multi<SSL> listSSLs()
        {
            return this.getlist<SSL>("ssls");
        }

        /// <summary>
        /// 按id获取ssl
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Item<SSL> getSSL(String id)
        {
            return this.getById<SSL>(id, "ssls");
        }

        /// <summary>
        /// 删除指定id的ssl
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool deleteSSL(String id)
        {
            return this.deleteById(id, "ssls");
        }

        /// <summary>
        /// 按指定id创建ssl
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ssl"></param>
        /// <returns></returns>
        /// <exception cref="ApisixSDKExcetion"></exception>
        public Item<SSL> putSSL(String id, SSL ssl)
        {
            return this.putById(id, ssl, "ssls");
        }

        /// <summary>
        /// 自动生成id创建ssl
        /// </summary>
        /// <param name="ssl"></param>
        /// <returns></returns>
        /// <exception cref="ApisixSDKExcetion"></exception>
        public Item<SSL> postSSL(SSL ssl)
        {
            return this.postSave(ssl, "ssls");
        }
        #endregion

        #region protos
        /// <summary>
        /// proto列表
        /// </summary>
        /// <returns></returns>
        public Multi<Proto> listProto()
        {
            return this.getlist<Proto>("protos");
        }

        /// <summary>
        /// 按id获取Proto
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Item<Proto> getProtoL(String id)
        {
            return this.getById<Proto>(id, "protos");
        }

        /// <summary>
        /// 删除指定id的Proto
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool deleteProto(String id)
        {
            return this.deleteById(id, "protos");
        }

        /// <summary>
        /// 按指定id创建Proto
        /// </summary>
        /// <param name="id"></param>
        /// <param name="proto"></param>
        /// <returns></returns>
        /// <exception cref="ApisixSDKExcetion"></exception>
        public Item<Proto> putProto(String id, Proto proto)
        {
            return this.putById(id, proto, "protos");
        }

        /// <summary>
        /// 自动生成id创建Proto
        /// </summary>
        /// <param name="proto"></param>
        /// <returns></returns>
        /// <exception cref="ApisixSDKExcetion"></exception>
        public Item<Proto> postProto(Proto proto)
        {
            return this.postSave(proto, "protos");
        }
        #endregion

        #region global_rules
        /// <summary>
        /// Global Rule列表
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ApisixSDKExcetion"></exception>
        public Multi<GlobalRule> listGlobalRule()
        {
            return this.getlist<GlobalRule>("global_rules");
        }

        /// <summary>
        /// 按id获取Global Rule
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="ApisixSDKExcetion"></exception>
        public Item<GlobalRule> getGlobalRule(String id)
        {
            return this.getById<GlobalRule>(id, "global_rules");
        }

        /// <summary>
        /// 删除指定id的Global Rule
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool deleteGlobalRule(String id)
        {
            return this.deleteById(id, "global_rules");
        }

        /// <summary>
        /// 按指定id创建Global Rule
        /// </summary>
        /// <param name="id"></param>
        /// <param name="globalRule"></param>
        /// <returns></returns>
        /// <exception cref="ApisixSDKExcetion"></exception>
        public Item<GlobalRule> putGlobalRule(String id, GlobalRule globalRule)
        {
            return this.putById(id, globalRule, "global_rules");
        }

        /// <summary>
        /// 按指定id修改已有 Global Rule 的部分属性路由
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ApisixSDKExcetion"></exception>
        public Item<GlobalRule> patchGlobalRule(String id, object globalRule)
        {
            return this.patchById<GlobalRule>(id, globalRule, "global_rules");
        }
        #endregion
        #region consumer_groups
        /// <summary>
        /// Consumer Group列表
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ApisixSDKExcetion"></exception>
        public Multi<ConsumerGroup> listConsumerGroup()
        {
            return this.getlist<ConsumerGroup>("consumer_groups");
        }

        /// <summary>
        /// 按id获取Consumer Group
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="ApisixSDKExcetion"></exception>
        public Item<ConsumerGroup> getConsumerGroup(String id)
        {
            return this.getById<ConsumerGroup>(id, "consumer_groups");
        }

        /// <summary>
        /// 删除指定id的Consumer Group
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool deleteConsumerGroup(String id)
        {
            return this.deleteById(id, "consumer_groups");
        }

        /// <summary>
        /// 按指定id创建Consumer Group
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ApisixSDKExcetion"></exception>
        public Item<ConsumerGroup> putConsumerGroup(String id, ConsumerGroup consumerGroup)
        {
            return this.putById(id, consumerGroup, "consumer_groups");
        }
        /// <summary>
        /// 按指定id修改已有 Consumer Group 的部分属性路由
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ApisixSDKExcetion"></exception>
        public Item<ConsumerGroup> patchConsumerGroup(String id, object consumerGroup)
        {
            return this.patchById<ConsumerGroup>(id, consumerGroup, "consumer_groups");
        }
        #endregion
        #region plugin_configs
        /// <summary>
        /// Plugin Config列表
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ApisixSDKExcetion"></exception>
        public Multi<PluginConfig> listPluginConfig()
        {
            return this.getlist<PluginConfig>("plugin_configs");
        }

        /// <summary>
        /// 按id获取Plugin Config
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="ApisixSDKExcetion"></exception>
        public Item<PluginConfig> getPluginConfig(String id)
        {
            return this.getById<PluginConfig>(id, "plugin_configs");
        }

        /// <summary>
        /// 删除指定id的Plugin Config
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool deletePluginConfig(String id)
        {
            return this.deleteById(id, "plugin_configs");
        }

        /// <summary>
        /// 按指定id创建Plugin Config
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ApisixSDKExcetion"></exception>
        public Item<PluginConfig> putPluginConfig(String id, PluginConfig pluginConfig)
        {
            return this.putById(id, pluginConfig, "plugin_configs");
        }
        /// <summary>
        /// 按指定id修改已有 Plugin Config 的部分属性路由
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ApisixSDKExcetion"></exception>
        public Item<PluginConfig> patchPluginConfig(String id, object pluginConfig)
        {
            return this.patchById<PluginConfig>(id, pluginConfig, "plugin_configs");
        }
        #endregion
        #region plugins
        /// <summary>
        /// 获取所有插件列表。
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ApisixSDKExcetion"></exception>
        public List<string> lisPlugins()
        {
            List<string> rsp = new List<string>();
            try
            {
                var ress = this.doRequest(HttpProfile.REQ_GET, $"/apisix/admin/plugins/list");
                rsp = ress.DeserializeObjectByJson<List<string>>();
            }
            catch (Exception e)
            {
                if (e is ApisixSDKExcetion ex)
                {
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
        /// 获取指定插件的属性
        /// </summary>
        /// <param name="plugin_name"></param>
        /// <returns></returns>
        /// <exception cref="ApisixSDKExcetion"></exception>
        public string getPluginProperties(String plugin_name)
        {
            List<string> rsp = new List<string>();
            try
            {
                var ress = this.doRequest(HttpProfile.REQ_GET, $"/apisix/admin/plugins/{plugin_name}");
                return ress;
            }
            catch (Exception e)
            {
                if (e is ApisixSDKExcetion ex)
                {
                    throw ex;
                }
                else
                {
                    throw new ApisixSDKExcetion(e.Message);
                }
            }
        }
        #endregion
    }
}
