using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiSix.CSharp.Sdk.Test
{
    public class ObjectContainerTests
    {
        [Test]
        public void Current()
        {
            var ioc = ObjectContainer.Current;
            var provider = ObjectContainer.Provider;

            var ioc2 = provider.GetValue("Container") as IObjectContainer;
            Assert.NotNull(ioc2);

            Assert.AreEqual(ioc, ioc2);
        }

        [Test]
        public void Add()
        {
            var ioc = new ObjectContainer();
            ioc.Add(new ServiceDescriptor { ServiceType = typeof(MemoryCache) });
            ioc.TryAdd(new ServiceDescriptor { ServiceType = typeof(MemoryCache) });

            Assert.AreEqual(1, ioc.Count);

            var services = ioc.GetValue("_list") as IList<IObject>;
            Assert.AreEqual(1, services.Count);
            Assert.AreEqual(typeof(MemoryCache), services[0].ServiceType);
            Assert.Null(services[0].ImplementationType);
            Assert.AreEqual(ObjectLifetime.Singleton, services[0].Lifetime);
        }

        [Test]
        public void Register()
        {
            var ioc = new ObjectContainer();
            ioc.Register(typeof(MemoryCache), null, null);
            ioc.Register(typeof(ICache), typeof(MemoryCache), null);

            Assert.AreEqual(2, ioc.Count);
        }

        [Test]
        public void Resolve()
        {
            var ioc = new ObjectContainer();
            ioc.Register(typeof(MemoryCache), null, null);
            ioc.Register(typeof(ICache), typeof(MemoryCache), null);

            var mc = ioc.Resolve(typeof(MemoryCache));
            Assert.NotNull(mc);

            var mci = ioc.Resolve(typeof(ICache));
            Assert.NotNull(mci);

            Assert.AreNotEqual(mc, mci);

            //var rds = ioc.Resolve(typeof(Redis));
            //Assert.NotNull(rds);
            //var rds2 = ioc.Resolve(typeof(Redis));
            //Assert.AreNotEqual(rds, rds2);

            var cache = ioc.Resolve(typeof(ICache));
            Assert.NotNull(cache);
        }

        [Test]
        public void AddSingleton()
        {
            var ioc = new ObjectContainer();
            var services = ioc.Services;

            ioc.AddSingleton<ICache, MemoryCache>();
            Assert.AreEqual(1, ioc.Count);
            Assert.True(ioc.Resolve<ICache>() is MemoryCache);

            ioc.AddSingleton<ICache>(p => new MemoryCache());
            Assert.True(ioc.Resolve<ICache>() is MemoryCache);

            Assert.AreEqual(2, services.Count);
            Assert.AreEqual(ObjectLifetime.Singleton, services[0].Lifetime);

            var serviceProvider = ioc.BuildServiceProvider();
            var obj = serviceProvider.GetService<ICache>();
            Assert.True(obj is MemoryCache);

            var obj2 = serviceProvider.GetService<ICache>();
            Assert.AreEqual(obj, obj2);

            var objs = serviceProvider.GetServices<ICache>().ToList();
            Assert.AreEqual(2, objs.Count);
            Assert.AreEqual(obj, objs[0]);
            Assert.AreNotEqual(obj, objs[1]);
            Assert.AreNotEqual(objs[0], objs[1]);
        }

        [Test]
        public void AddScoped()
        {
            var ioc = new ObjectContainer();
            var services = ioc.Services;

            ioc.AddScoped<ICache, MemoryCache>();
            Assert.AreEqual(1, ioc.Count);
            Assert.True(ioc.Resolve<ICache>() is MemoryCache);

            ioc.AddScoped<ICache>(p => new MemoryCache());
            Assert.True(ioc.Resolve<ICache>() is MemoryCache);

            Assert.AreEqual(2, services.Count);
            Assert.AreEqual(ObjectLifetime.Scoped, services[0].Lifetime);

            var root = ioc.BuildServiceProvider();
            {
                var serviceProvider = root;
                var obj = serviceProvider.GetService<ICache>();
                Assert.True(obj is MemoryCache);

                var obj2 = serviceProvider.GetService<ICache>();
                Assert.AreNotEqual(obj, obj2);

                var objs = serviceProvider.GetServices<ICache>().ToList();
                Assert.AreEqual(2, objs.Count);
                Assert.AreNotEqual(obj, objs[0]);
                Assert.AreNotEqual(obj, objs[1]);
                Assert.AreNotEqual(objs[0], objs[1]);
            }

            {
                using var scope = root.CreateScope();
                var serviceProvider = scope.ServiceProvider;
                var obj = serviceProvider.GetService<ICache>();
                Assert.True(obj is MemoryCache);

                var instance = root.GetService<ICache>();
                Assert.AreNotEqual(obj, instance);

                var obj2 = serviceProvider.GetService<ICache>();
                Assert.AreEqual(obj, obj2);
                Assert.AreNotEqual(obj2, instance);

                var objs = serviceProvider.GetServices<ICache>().ToList();
                Assert.AreEqual(2, objs.Count);
                Assert.AreNotEqual(obj, objs[0]);
                Assert.AreNotEqual(obj, objs[1]);
                Assert.AreNotEqual(objs[0], objs[1]);
            }

            {
                var serviceProvider = root;
                var obj = serviceProvider.GetService<ICache>();
                Assert.True(obj is MemoryCache);

                var obj2 = serviceProvider.GetService<ICache>();
                Assert.AreNotEqual(obj, obj2);

                var objs = serviceProvider.GetServices<ICache>().ToList();
                Assert.AreEqual(2, objs.Count);
                Assert.AreNotEqual(obj, objs[0]);
                Assert.AreNotEqual(obj, objs[1]);
                Assert.AreNotEqual(objs[0], objs[1]);
            }
        }

        [Test]
        public void AddTransient()
        {
            var ioc = new ObjectContainer();
            var services = ioc.Services;

            ioc.AddTransient<ICache, MemoryCache>();
            Assert.AreEqual(1, ioc.Count);
            Assert.True(ioc.Resolve<ICache>() is MemoryCache);

            ioc.AddTransient<ICache>(p => new MemoryCache());
            Assert.True(ioc.Resolve<ICache>() is MemoryCache);

            Assert.AreEqual(2, services.Count);
            Assert.AreEqual(ObjectLifetime.Transient, services[0].Lifetime);

            var serviceProvider = ioc.BuildServiceProvider();
            var obj = serviceProvider.GetService<ICache>();
            Assert.True(obj is MemoryCache);

            var obj2 = serviceProvider.GetService<ICache>();
            Assert.AreNotEqual(obj, obj2);

            var objs = serviceProvider.GetServices<ICache>().ToList();
            Assert.AreEqual(2, objs.Count);
            Assert.AreNotEqual(obj, objs[0]);
            Assert.AreNotEqual(obj, objs[1]);
            Assert.AreNotEqual(objs[0], objs[1]);
        }

        [Test]
        public void BuildServiceProvider()
        {
            var ioc = new ObjectContainer();

            ioc.AddTransient<ICache, MemoryCache>();

            var provider = ioc.BuildServiceProvider();

            var cache = provider.GetService(typeof(ICache));
            var cache2 = provider.GetService(typeof(ICache));
            Assert.NotNull(cache);
            Assert.NotNull(cache2);
            Assert.AreNotEqual(cache, cache2);
        }

        [Test]
        public void TestMutilConstructor()
        {
            {
                var ioc = new ObjectContainer();
                ioc.AddSingleton<ICache, MemoryCache>();
                ioc.AddTransient<MyService>();

                var svc = ioc.Resolve<MyService>();
                Assert.AreEqual(1, svc.Kind);
            }

            {
                var ioc = new ObjectContainer();
                ioc.AddSingleton<MemoryCache>();
                ioc.AddTransient<MyService>();

                var svc = ioc.Resolve<MyService>();
                Assert.AreEqual(2, svc.Kind);
            }

            {
                var ioc = new ObjectContainer();
                ioc.AddSingleton<ICache, MemoryCache>();
                ioc.AddTransient<MyService>();

                var svc = ioc.Resolve<MyService>();
                Assert.AreEqual(1, svc.Kind);
            }

            {
                var ioc = new ObjectContainer();
                ioc.AddSingleton<ICache, MemoryCache>();
                ioc.AddTransient<MyService>();

                var svc = ioc.Resolve<MyService>();
                Assert.AreEqual(1, svc.Kind);
            }

            {
                var ioc = new ObjectContainer();
                ioc.AddSingleton<ICache, MemoryCache>();
                ioc.AddSingleton<ILog>(XTrace.Log);
                ioc.AddTransient<MyService>();

                var svc = ioc.Resolve<MyService>();
                Assert.AreEqual(3, svc.Kind);
            }
        }
    }
    public class MyService
    {
        public Int32 Kind { get; set; }

        public MyService() => Kind = 1;

        public MyService(MemoryCache redis) => Kind = 2;

        public MyService(ICache cache, ILog log) => Kind = 3;
    }
}