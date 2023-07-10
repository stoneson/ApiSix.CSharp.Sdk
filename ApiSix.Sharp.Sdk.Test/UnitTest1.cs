using ApiSix.Sharp.model;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;

namespace ApiSix.Sharp.Sdk.Test
{
    public class Tests
    {
        private String endpoint = "192.168.3.128:9180";
        private String version = "3.3.0";
        private String apiKey = "edd1c9f034335f136f87ad84b625c8f1";

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            Assert.Pass();
        }
        [Test]
        public void testUpstream()
        {
            Credential credential = new DefaultCredential(apiKey);
            Profile profile = DefaultProfile.getProfile(endpoint, version, credential);
            AdminClient adminClient = new AdminClient(profile);

            var id = "2";
            var upstream = new Upstream();
            upstream.id = id;
            upstream.name = "name" + id;
            var nodes = new Dictionary<string, int>();

            nodes.Add("127.0.0.1:8080", 1);

            upstream.type = "roundrobin";
            upstream.desc = "upstream created by c# sdk.";
            upstream.nodes = nodes;

            var up = adminClient.getUpstream("467815367964623559");
            var ups = adminClient.listUpstreams();

            try
            {
                adminClient.deleteUpstream(id);
            }
            catch (ApisixSDKExcetion e)
            {
                Assert.AreEqual(e.getErrorCode().ToString().ToString(), "404");
            }
            adminClient.putUpstream(id, upstream);
            //adminClient.postUpstream(upstream);
            upstream.desc = "upstream created by c# sdk.2";
            try
            {
                adminClient.putUpstream(id, upstream);
                //adminClient.postUpstream(upstream);
            }
            catch (ApisixSDKExcetion e)
            {
                Assert.AreEqual(e.getErrorCode().ToString(), "400");
            }
            up = adminClient.getUpstream(id);

            Assert.That(up.value.desc, Is.EqualTo(upstream.desc));
            Assert.That(up.value.type, Is.EqualTo("roundrobin"));


            var list = adminClient.listUpstreams();
            int size1 = list.total;

            //must has 1 or more routes
            Assert.True(size1 > 0);

            //delete not exist route
            try
            {
                adminClient.deleteUpstream("id-not-exists");
            }
            catch (ApisixSDKExcetion e)
            {
                Assert.AreEqual(e.getErrorCode().ToString(), "404");
            }

            Assert.True(adminClient.deleteUpstream(id));

            try
            {
                adminClient.getUpstream(id);
            }
            catch (ApisixSDKExcetion e)
            {
                Assert.AreEqual(e.getErrorCode().ToString(), "404");
            }

        }
        [Test]
        public void testService()
        {
            Credential credential = new DefaultCredential(apiKey);
            Profile profile = DefaultProfile.getProfile(endpoint, version, credential);
            AdminClient adminClient = new AdminClient(profile);

            var id = "2";
            Service service = new Service();
            service.name = "servicename" + id;
            Upstream upstream = new Upstream();
            upstream.id = id;
            upstream.name = "name" + id;
            Dictionary<String, int> nodes = new Dictionary<String, int>();
            List<String> methods = new List<String>();

            nodes.Add("127.0.0.1:8080", 1);
            methods.Add("GET");
            upstream.type = ("roundrobin");
            upstream.nodes = (nodes);


            LimitCount lmt = new LimitCount();
            lmt.count = (2);
            lmt.key = ("remote_addr");
            lmt.rejectedCode = (503);
            lmt.timeWindow = (60);

            var plugins = new Dictionary<string, Plugin>();
            plugins.Add("limit-count", lmt);

            service.upstream = (upstream);
            service.desc = ("service created by c# sdk.s");
            service.plugins = (plugins);

            var svcs = adminClient.listServices();

            adminClient.putService(id, service);

            var svc = adminClient.getService(id);

            Assert.AreEqual(service.desc, svc.value.desc);


            var list = adminClient.listServices();
            int size1 = list.total;

            //must has 1 or more services
            Assert.True(size1 > 0);

            //delete not exist service
            try
            {
                adminClient.deleteService("id-not-exists");
            }
            catch (ApisixSDKExcetion e)
            {
                Assert.AreEqual(e.getErrorCode().ToString(), "404");
            }

            var delRes = adminClient.deleteService(id);
            Assert.True(delRes);

            try
            {
                svc = adminClient.getService(id);
            }
            catch (ApisixSDKExcetion e)
            {
                Assert.AreEqual(e.getErrorCode().ToString(), "404");
            }

        }
        [Test]
        public void testRoute()
        {
            Credential credential = new DefaultCredential(apiKey);
            Profile profile = DefaultProfile.getProfile(endpoint, version, credential);
            AdminClient adminClient = new AdminClient(profile);

            var list1 = adminClient.listRoutes();

            //创建upstream
            var upstreamId = "3";
            Upstream upstream = new Upstream();
            Dictionary<String, int> nodes = new Dictionary<String, int>();
            nodes.Add("127.0.0.1:8080", 1);
            nodes.Add("127.0.0.1:8180", 1);
            upstream.name = "upstreamname" + upstreamId;
            upstream.type = ("roundrobin");
            upstream.desc = ("upstream created by c# sdk.");
            upstream.nodes = (nodes);

            adminClient.putUpstream(upstreamId, upstream);

            //创建service

            String serviceId = "3";
            Service service = new Service();

            LimitCount lmt = new LimitCount();
            lmt.count = (2);
            lmt.key = ("remote_addr");
            lmt.rejectedCode = (503);
            lmt.timeWindow = (60);

            var plugins = new Dictionary<string, Plugin>();
            plugins.Add("limit-count", lmt);

            service.name= "servicename" + serviceId;
            service.upstreamId = (upstreamId);
            service.desc = ("service created by c# sdk.");
            service.plugins = (plugins);

            adminClient.putService(serviceId, service);

            var routeId = "3";
            Route route = new Route();

            List<String> methods = new List<String>();

            methods.Add("GET");
            upstream.type = ("roundrobin");
            upstream.nodes = (nodes);

            route.uri = ("/helloworld");
            route.desc = ("route created by c# sdk");
            route.methods = (methods);
            route.ServiceId = (serviceId);
            route.name = routeId;
            route.status = 1;

            route.labels = new Dictionary<string, string>();
            route.labels["test"] = "test";
            route.labels["API_VERSION"] = "v2";

            adminClient.putRoute(routeId, route);

            var routeEntity = adminClient.getRoute(routeId);

            Assert.AreEqual("/helloworld", routeEntity.value.uri);
            Assert.AreEqual(serviceId, routeEntity.value.ServiceId);


            var list = adminClient.listRoutes();
            int size1 = list.total;

            //must has 1 or more routes
            Assert.True(size1 > 0);

            //delete not exist route
            try
            {
                adminClient.deleteRoute("id-not-exists");
            }
            catch (ApisixSDKExcetion e)
            {
                Assert.AreEqual(e.getErrorCode().ToString(), "404");
            }

            Assert.True(adminClient.deleteRoute(routeId));
            Assert.True(adminClient.deleteService(serviceId));
            Assert.True(adminClient.deleteUpstream(upstreamId));

            list = adminClient.listRoutes();
            int size2 = list.total;

            //size minus
            Assert.AreEqual(size1, size2 + 1);

            try
            {
                adminClient.getRoute(routeId);
            }
            catch (ApisixSDKExcetion e)
            {
                Assert.AreEqual(e.getErrorCode().ToString(), "404");
            }
        }

        [Test]
        public void testK8sInfo()
        {
            Credential credential = new DefaultCredential(apiKey);
            Profile profile = DefaultProfile.getProfile(endpoint, version, credential);
            AdminClient adminClient = new AdminClient(profile);

            //创建upstream
            Upstream upstream = new Upstream();
            Dictionary<String, int> nodes = new Dictionary<String, int>();
            K8sDeploymentInfo k8sInfo = new K8sDeploymentInfo();

            k8sInfo.Namespace = ("test-namespace");
            k8sInfo.deployName = ("test-deploy");
            k8sInfo.serviceName = ("test-service");
            k8sInfo.port = (8080);
            k8sInfo.backendType = ("pod");

            String routeId = "3";
            Route route = new Route();

            List<String> methods = new List<String>();

            nodes.Add("127.0.0.1:8080", 1);
            methods.Add("GET");
            upstream.type = ("roundrobin");
            upstream.nodes = (nodes);
            upstream.k8sDeploymentInfo = (k8sInfo);

            route.uri = ("/helloworld");
            route.desc = ("route created by c# sdk");
            route.methods = (methods);
            route.upstream = (upstream);

            adminClient.putRoute(routeId, route);

            var routeEntity = adminClient.getRoute(routeId);

            Assert.AreEqual("/helloworld", routeEntity.value.uri);


            var list = adminClient.listRoutes();
            int size1 = list.total;

            //must has 1 or more routes
            Assert.True(size1 > 0);

            //delete not exist route
            try
            {
                adminClient.deleteRoute("id-not-exists");
            }
            catch (ApisixSDKExcetion e)
            {
                Assert.AreEqual(e.getErrorCode().ToString(), "404");
            }

            var Namespace = k8sInfo.Namespace;
            String deployName = k8sInfo.deployName;
            String serviceName = k8sInfo.serviceName;
            String upstreamId = Namespace + "-" + deployName + "-" + serviceName;
            try
            {
                Assert.True(adminClient.deleteRoute(routeId));
                Assert.True(adminClient.deleteUpstream(upstreamId));
            }
            catch (ApisixSDKExcetion e)
            {
                Assert.AreEqual(e.getErrorCode().ToString(), "404");
            }
            list = adminClient.listRoutes();
            int size2 = list.total;

            //size minus
            Assert.AreEqual(size1, size2 + 1);

            try
            {
                adminClient.getRoute(routeId);
            }
            catch (ApisixSDKExcetion e)
            {
                Assert.AreEqual(e.getErrorCode().ToString(), "404");
            }
        }

        [Test]
        public void testConsumer()
        {
            Credential credential = new DefaultCredential(apiKey);
            Profile profile = DefaultProfile.getProfile(endpoint, version, credential);
            AdminClient adminClient = new AdminClient(profile);

            var csms = adminClient.listConsumers();

            var username = "test";
            Consumer consumer = new Consumer();

            var keyAuth = new KeyAuth();
            keyAuth.key = ("testkey");

            var plugins = new Dictionary<string, Plugin>();
            plugins.Add("key-auth", keyAuth);

            consumer.username = ("test");
            consumer.desc = ("consumer created by c# sdk.");
            consumer.plugins = (plugins);

            adminClient.putConsumer(username, consumer);

            var csm = adminClient.getConsumer(username);

            Assert.AreEqual("consumer created by c# sdk.", csm.value.desc);
            Assert.AreEqual(username, csm.value.username);


            //delete not exist consumer
            try
            {
                adminClient.deleteConsumer("id-not-exists");
            }
            catch (ApisixSDKExcetion e)
            {
                Assert.AreEqual(e.getErrorCode().ToString(), "404");
            }

            Assert.True(adminClient.deleteConsumer(username));

            try
            {
                adminClient.getConsumer(username);
            }
            catch (ApisixSDKExcetion e)
            {
                Assert.AreEqual(e.getErrorCode().ToString(), "404");
            }

        }

        [Test]
        public void testSSL()
        {
            Credential credential = new DefaultCredential(apiKey);
            Profile profile = DefaultProfile.getProfile(endpoint, version, credential);
            AdminClient adminClient = new AdminClient(profile);

            String id = "2";
            SSL ssl = new SSL();

            ssl.cert = ("fake-cert-contentfake-cert-contentfake-cert-contentfake-cert-contentfake-cert-contentfake-cert-contentfake-cert-contentfake-cert-contentfake-cert-contentfake-cert-contentfake-cert-contentfake-cert-content");
            ssl.key = ("fake-key-contentfake-key-contentfake-key-contentfake-key-contentfake-key-contentfake-key-contentfake-key-contentfake-key-contentfake-key-contentfake-key-contentfake-key-contentfake-key-contentfake-key-content");
            ssl.snis = new List<string> { "sni.cn" };

            adminClient.putSSL(id, ssl);

            var s = adminClient.getSSL(id);

            //Assert.AreEqual("sni.cn", s.value.sni);

            //delete not exist ssl
            try
            {
                adminClient.deleteSSL("id-not-exists");
            }
            catch (ApisixSDKExcetion e)
            {
                Assert.AreEqual(e.getErrorCode(),404);
            }

            Assert.True(adminClient.deleteSSL(id));

            try
            {
                adminClient.getSSL(id);
            }
            catch (ApisixSDKExcetion e)
            {
                Assert.AreEqual(e.getErrorCode().ToString(), "404");
            }
        }
    }
}