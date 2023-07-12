using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace ApiSix.CSharp
{
    #region ServiceScopeFactory
    /// <summary>范围服务工厂</summary>
    public interface IServiceScopeFactory
    {
        /// <summary>创建范围服务</summary>
        /// <returns></returns>
        IServiceScope CreateScope();
    }

    class MyServiceScopeFactory : IServiceScopeFactory
    {
        public IServiceProvider ServiceProvider { get; set; }

        public IServiceScope CreateScope() => new MyServiceScope { MyServiceProvider = ServiceProvider };
    }
    /// <summary>范围服务。该范围生命周期内，每个服务类型只有一个实例</summary>
    /// <remarks>
    /// 满足Singleton和Scoped的要求，暂时无法满足Transient的要求（仍然只有一份）。
    /// </remarks>
    public interface IServiceScope : IDisposable
    {
        /// <summary>服务提供者</summary>
        IServiceProvider ServiceProvider { get; }
    }

    class MyServiceScope : IServiceScope, IServiceProvider
    {
        public IServiceProvider MyServiceProvider { get; set; }

        public IServiceProvider ServiceProvider => this;

        private readonly ConcurrentDictionary<Type, Object> _cache = new ConcurrentDictionary<Type, object>();

        public void Dispose()
        {
            // 销毁所有缓存
            foreach (var item in _cache)
            {
                if (item.Value is IDisposable dsp) dsp.Dispose();
            }
        }

        public Object GetService(Type serviceType)
        {
            while (true)
            {
                // 查缓存，如果没有再获取一个并缓存起来
                if (_cache.TryGetValue(serviceType, out var service)) return service;

                service = MyServiceProvider.GetService(serviceType);

                if (_cache.TryAdd(serviceType, service)) return service;
            }
        }
    }
    #endregion

    #region IObjectContainer
    // <summary>轻量级对象容器，支持注入</summary>
    /// <remarks>
    /// </remarks>
    public interface IObjectContainer
    {
        #region 注册
        /// <summary>注册类型和名称</summary>
        /// <param name="serviceType">接口类型</param>
        /// <param name="implementationType">实现类型</param>
        /// <param name="instance">实例</param>
        /// <returns></returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        IObjectContainer Register(Type serviceType, Type implementationType, Object instance);

        /// <summary>添加</summary>
        /// <param name="item"></param>
        void Add(IObject item);

        /// <summary>尝试添加</summary>
        /// <param name="item"></param>
        Boolean TryAdd(IObject item);
        #endregion

        #region 解析
        /// <summary>在指定容器中解析类型的实例</summary>
        /// <param name="serviceType">接口类型</param>
        /// <param name="serviceProvider">容器</param>
        /// <returns></returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        Object Resolve(Type serviceType, IServiceProvider serviceProvider = null);
        #endregion
    }

    /// <summary>生命周期</summary>
    public enum ObjectLifetime
    {
        /// <summary>单实例</summary>
        Singleton,

        /// <summary>容器内单实例</summary>
        Scoped,

        /// <summary>每次一个实例</summary>
        Transient
    }

    /// <summary>对象映射接口</summary>
    public interface IObject
    {
        /// <summary>服务类型</summary>
        Type ServiceType { get; }

        /// <summary>实现类型</summary>
        Type ImplementationType { get; }

        /// <summary>生命周期</summary>
        ObjectLifetime Lifetime { get; }
    }
    #endregion

    #region ObjectContainer
    // <summary>轻量级对象容器，支持注入</summary>
    /// <remarks>
    /// </remarks>
    public class ObjectContainer : IObjectContainer
    {
        #region 静态
        /// <summary>当前容器</summary>
        public static IObjectContainer Current { get; set; } = new ObjectContainer();

        /// <summary>当前容器提供者</summary>
        public static IServiceProvider Provider { get; set; } = new ServiceProvider(Current);
        #endregion

        #region 属性
        private readonly IList<IObject> _list = new List<IObject>();
        /// <summary>服务集合</summary>
        public IList<IObject> Services => _list;

        /// <summary>注册项个数</summary>
        public Int32 Count => _list.Count;
        #endregion

        #region 方法
        /// <summary>添加，允许重复添加同一个服务</summary>
        /// <param name="item"></param>
        public void Add(IObject item)
        {
            lock (_list)
            {
                //for (var i = 0; i < _list.Count; i++)
                //{
                //    // 覆盖重复项
                //    if (_list[i].ServiceType == item.ServiceType)
                //    {
                //        _list[i] = item;
                //        return;
                //    }
                //}

                if (item.ImplementationType == null && item is ServiceDescriptor sd)
                    sd.ImplementationType = sd.Instance?.GetType();

                _list.Add(item);
            }
        }

        /// <summary>尝试添加，不允许重复添加同一个服务</summary>
        /// <param name="item"></param>
        public Boolean TryAdd(IObject item)
        {
            if (_list.Any(e => e.ServiceType == item.ServiceType)) return false;
            lock (_list)
            {
                if (_list.Any(e => e.ServiceType == item.ServiceType)) return false;

                if (item.ImplementationType == null && item is ServiceDescriptor sd)
                    sd.ImplementationType = sd.Instance?.GetType();

                _list.Add(item);

                return true;
            }
        }
        #endregion

        #region 注册
        /// <summary>注册</summary>
        /// <param name="serviceType">接口类型</param>
        /// <param name="implementationType">实现类型</param>
        /// <param name="instance">实例</param>
        /// <returns></returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual IObjectContainer Register(Type serviceType, Type implementationType, Object instance)
        {
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));

            var item = new ServiceDescriptor
            {
                ServiceType = serviceType,
                ImplementationType = implementationType,
                Instance = instance,
                Lifetime = instance == null ? ObjectLifetime.Transient : ObjectLifetime.Singleton,
            };
            Add(item);

            return this;
        }
        #endregion

        #region 解析
        /// <summary>在指定容器中解析类型的实例</summary>
        /// <param name="serviceType">接口类型</param>
        /// <param name="serviceProvider">容器</param>
        /// <returns></returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Object Resolve(Type serviceType, IServiceProvider serviceProvider = null)
        {
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));

            // 优先查找最后一个，避免重复注册
            //var item = _list.FirstOrDefault(e => e.ServiceType == serviceType);
            var item = _list.LastOrDefault(e => e.ServiceType == serviceType);
            if (item == null) return null;

            return Resolve(item, serviceProvider);
        }

        /// <summary>在指定容器中解析类型的实例</summary>
        /// <param name="item"></param>
        /// <param name="serviceProvider"></param>
        /// <returns></returns>
        public virtual Object Resolve(IObject item, IServiceProvider serviceProvider)
        {
            var map = item as ServiceDescriptor;
            var type = item.ImplementationType ?? item.ServiceType;
            switch (item.Lifetime)
            {
                case ObjectLifetime.Singleton:
                    if (map != null)
                    {
                        map.Instance ??= CreateInstance(type, serviceProvider ?? new ServiceProvider(this), map.Factory);

                        return map.Instance;
                    }
                    return CreateInstance(type, serviceProvider ?? new ServiceProvider(this), null);

                case ObjectLifetime.Scoped:
                case ObjectLifetime.Transient:
                default:
                    return CreateInstance(type, serviceProvider ?? new ServiceProvider(this), map?.Factory);
            }
        }

        private static IDictionary<TypeCode, Object> _defs;
        private static Object CreateInstance(Type type, IServiceProvider provider, Func<IServiceProvider, Object> factory)
        {
            if (factory != null) return factory(provider);

            // 初始化
            if (_defs == null)
            {
                var dic = new Dictionary<TypeCode, Object>
            {
                { TypeCode.Empty, null },
                { TypeCode.DBNull, null},
                { TypeCode.Boolean, false },
                { TypeCode.Char, (Char)0 },
                { TypeCode.SByte, (SByte)0 },
                { TypeCode.Byte, (Byte)0 },
                { TypeCode.Int16, (Int16)0 },
                { TypeCode.UInt16, (UInt16)0 },
                { TypeCode.Int32, (Int32)0 },
                { TypeCode.UInt32, (UInt32)0 },
                { TypeCode.Int64, (Int64)0 },
                { TypeCode.UInt64, (UInt64)0 },
                { TypeCode.Single, (Single)0 },
                { TypeCode.Double, (Double)0 },
                { TypeCode.Decimal, (Decimal)0 },
                { TypeCode.DateTime, DateTime.MinValue },
                { TypeCode.String, null }
            };

                _defs = dic;
            }

            ParameterInfo errorParameter = null;
            if (!type.IsAbstract)
            {
                // 选择构造函数，优先选择参数最多的可匹配构造函数
                var constructors = type.GetConstructors();
                foreach (var constructorInfo in constructors.OrderByDescending(e => e.GetParameters().Length))
                {
                    if (constructorInfo.IsStatic) continue;

                    ParameterInfo errorParameter2 = null;
                    var ps = constructorInfo.GetParameters();
                    var pv = new Object[ps.Length];
                    for (var i = 0; i != ps.Length; i++)
                    {
                        if (pv[i] != null) continue;

                        var ptype = ps[i].ParameterType;
                        if (_defs.TryGetValue(Type.GetTypeCode(ptype), out var obj))
                            pv[i] = obj;
                        else
                        {
                            var service = provider.GetService(ps[i].ParameterType);
                            if (service == null)
                            {
                                errorParameter2 = ps[i];

                                break;
                            }
                            else
                            {
                                pv[i] = service;
                            }
                        }
                    }

                    if (errorParameter2 == null) return constructorInfo.Invoke(pv);
                    errorParameter = errorParameter2;
                }
            }

            throw new InvalidOperationException($"未找到适合 '{type}' 的构造函数，请确认该类型构造函数所需参数均已注册。无法解析参数 '{errorParameter}'");
        }
        #endregion

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString() => $"{GetType().Name}[Count={Count}]";
        #endregion
    }
    #endregion

    #region ServiceDescriptor
    /// <summary>对象映射</summary>
    [DebuggerDisplay("Lifetime = {Lifetime}, ServiceType = {ServiceType}, ImplementationType = {ImplementationType}")]
    public class ServiceDescriptor : IObject
    {
        #region 属性
        /// <summary>服务类型</summary>
        public Type ServiceType { get; set; }

        /// <summary>实现类型</summary>
        public Type ImplementationType { get; set; }

        /// <summary>生命周期</summary>
        public ObjectLifetime Lifetime { get; set; }

        /// <summary>实例</summary>
        public Object Instance { get; set; }

        /// <summary>对象工厂</summary>
        public Func<IServiceProvider, Object> Factory { get; set; }
        #endregion

        #region 方法
        /// <summary>显示友好名称</summary>
        /// <returns></returns>
        public override String ToString() => $"[{ServiceType?.Name},{ImplementationType?.Name}]";
        #endregion
    }
    #endregion
    #region ServiceProvider
    internal class ServiceProvider : IServiceProvider
    {
        private readonly IObjectContainer _container;
        /// <summary>容器</summary>
        public IObjectContainer Container => _container;

        public ServiceProvider(IObjectContainer container) => _container = container;

        public Object GetService(Type serviceType)
        {
            if (serviceType == typeof(IObjectContainer)) return _container;
            if (serviceType == typeof(ObjectContainer)) return _container;
            if (serviceType == typeof(IServiceProvider)) return this;

            if (_container is ObjectContainer ioc && !ioc.Services.Any(e => e.ServiceType == typeof(IServiceScopeFactory)))
            {
                //oc.AddSingleton<IServiceScopeFactory>(new MyServiceScopeFactory { ServiceProvider = this });
                ioc.TryAdd(new ServiceDescriptor
                {
                    ServiceType = typeof(IServiceScopeFactory),
                    Instance = new MyServiceScopeFactory { ServiceProvider = this },
                    Lifetime = ObjectLifetime.Singleton,
                });
            }

            return _container.Resolve(serviceType, this);
        }
    }
    #endregion

    /// <summary>对象容器助手。扩展方法专用</summary>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public static class ObjectContainerHelper
    {
        #region 单实例注册
        /// <summary>添加单实例，指定实现类型</summary>
        /// <param name="container"></param>
        /// <param name="serviceType"></param>
        /// <param name="implementationType"></param>
        /// <returns></returns>
        public static IObjectContainer AddSingleton(this IObjectContainer container, Type serviceType, Type implementationType)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
            if (implementationType == null) throw new ArgumentNullException(nameof(implementationType));

            var item = new ServiceDescriptor
            {
                ServiceType = serviceType,
                ImplementationType = implementationType,
                Lifetime = ObjectLifetime.Singleton,
            };
            container.Add(item);

            return container;
        }

        /// <summary>添加单实例，指定实现类型</summary>
        /// <typeparam name="TService"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        /// <param name="container"></param>
        /// <returns></returns>
        public static IObjectContainer AddSingleton<TService, TImplementation>(this IObjectContainer container) where TService : class where TImplementation : class, TService => container.AddSingleton(typeof(TService), typeof(TImplementation));

        /// <summary>添加单实例，指定实例工厂</summary>
        /// <param name="container"></param>
        /// <param name="serviceType"></param>
        /// <param name="factory"></param>
        /// <returns></returns>
        public static IObjectContainer AddSingleton(this IObjectContainer container, Type serviceType, Func<IServiceProvider, Object> factory)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            var item = new ServiceDescriptor
            {
                ServiceType = serviceType,
                Factory = factory,
                Lifetime = ObjectLifetime.Singleton,
            };
            container.Add(item);

            return container;
        }

        /// <summary>添加单实例，指定实例工厂</summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="container"></param>
        /// <param name="factory"></param>
        /// <returns></returns>
        public static IObjectContainer AddSingleton<TService>(this IObjectContainer container, Func<IServiceProvider, TService> factory) where TService : class => container.AddSingleton(typeof(TService), factory);

        /// <summary>添加单实例，指定实例</summary>
        /// <param name="container"></param>
        /// <param name="serviceType"></param>
        /// <param name="instance"></param>
        /// <returns></returns>
        public static IObjectContainer AddSingleton(this IObjectContainer container, Type serviceType, Object instance)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
            //if (instance == null) throw new ArgumentNullException(nameof(instance));

            var item = new ServiceDescriptor
            {
                ServiceType = serviceType,
                Instance = instance,
                Lifetime = ObjectLifetime.Singleton,
            };
            container.Add(item);

            return container;
        }

        /// <summary>添加单实例，指定实例</summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="container"></param>
        /// <param name="instance"></param>
        /// <returns></returns>
        public static IObjectContainer AddSingleton<TService>(this IObjectContainer container, TService instance = null) where TService : class => container.AddSingleton(typeof(TService), instance);

        /// <summary>尝试添加单实例，指定实现类型</summary>
        /// <param name="container"></param>
        /// <param name="serviceType"></param>
        /// <param name="implementationType"></param>
        /// <returns></returns>
        public static IObjectContainer TryAddSingleton(this IObjectContainer container, Type serviceType, Type implementationType)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
            if (implementationType == null) throw new ArgumentNullException(nameof(implementationType));

            var item = new ServiceDescriptor
            {
                ServiceType = serviceType,
                ImplementationType = implementationType,
                Lifetime = ObjectLifetime.Singleton,
            };
            container.TryAdd(item);

            return container;
        }
        #endregion

        #region 范围容器
        /// <summary>添加范围容器实例，指定实现类型</summary>
        /// <param name="container"></param>
        /// <param name="serviceType"></param>
        /// <param name="implementationType"></param>
        /// <returns></returns>
        public static IObjectContainer AddScoped(this IObjectContainer container, Type serviceType, Type implementationType)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
            if (implementationType == null) throw new ArgumentNullException(nameof(implementationType));

            var item = new ServiceDescriptor
            {
                ServiceType = serviceType,
                ImplementationType = implementationType,
                Lifetime = ObjectLifetime.Scoped,
            };
            container.Add(item);

            return container;
        }

        /// <summary>添加范围容器实例，指定实现类型</summary>
        /// <typeparam name="TService"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        /// <param name="container"></param>
        /// <returns></returns>
        public static IObjectContainer AddScoped<TService, TImplementation>(this IObjectContainer container) where TService : class where TImplementation : class, TService => container.AddScoped(typeof(TService), typeof(TImplementation));

        /// <summary>添加范围容器实例，指定实现类型</summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="container"></param>
        /// <returns></returns>
        public static IObjectContainer AddScoped<TService>(this IObjectContainer container) where TService : class => container.AddScoped(typeof(TService), typeof(TService));

        /// <summary>添加范围容器实例，指定实现工厂</summary>
        /// <param name="container"></param>
        /// <param name="serviceType"></param>
        /// <param name="factory"></param>
        /// <returns></returns>
        public static IObjectContainer AddScoped(this IObjectContainer container, Type serviceType, Func<IServiceProvider, Object> factory)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            var item = new ServiceDescriptor
            {
                ServiceType = serviceType,
                Factory = factory,
                Lifetime = ObjectLifetime.Scoped,
            };
            container.Add(item);

            return container;
        }

        /// <summary>添加范围容器实例，指定实现工厂</summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="container"></param>
        /// <param name="factory"></param>
        /// <returns></returns>
        public static IObjectContainer AddScoped<TService>(this IObjectContainer container, Func<IServiceProvider, Object> factory) where TService : class => container.AddScoped(typeof(TService), factory);

        /// <summary>添加范围容器实例，指定实现类型</summary>
        /// <param name="container"></param>
        /// <param name="serviceType"></param>
        /// <param name="implementationType"></param>
        /// <returns></returns>
        public static IObjectContainer TryAddScoped(this IObjectContainer container, Type serviceType, Type implementationType)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
            if (implementationType == null) throw new ArgumentNullException(nameof(implementationType));

            var item = new ServiceDescriptor
            {
                ServiceType = serviceType,
                ImplementationType = implementationType,
                Lifetime = ObjectLifetime.Scoped,
            };
            container.TryAdd(item);

            return container;
        }
        #endregion

        #region 瞬态注册
        /// <summary>添加瞬态实例，指定实现类型</summary>
        /// <param name="container"></param>
        /// <param name="serviceType"></param>
        /// <param name="implementationType"></param>
        /// <returns></returns>
        public static IObjectContainer AddTransient(this IObjectContainer container, Type serviceType, Type implementationType)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
            if (implementationType == null) throw new ArgumentNullException(nameof(implementationType));

            var item = new ServiceDescriptor
            {
                ServiceType = serviceType,
                ImplementationType = implementationType,
                Lifetime = ObjectLifetime.Transient,
            };
            container.Add(item);

            return container;
        }

        /// <summary>添加瞬态实例，指定实现类型</summary>
        /// <typeparam name="TService"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        /// <param name="container"></param>
        /// <returns></returns>
        public static IObjectContainer AddTransient<TService, TImplementation>(this IObjectContainer container) where TService : class where TImplementation : class, TService => container.AddTransient(typeof(TService), typeof(TImplementation));

        /// <summary>添加瞬态实例，指定实现类型</summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="container"></param>
        /// <returns></returns>
        public static IObjectContainer AddTransient<TService>(this IObjectContainer container) where TService : class => container.AddTransient(typeof(TService), typeof(TService));

        /// <summary>添加瞬态实例，指定实现工厂</summary>
        /// <param name="container"></param>
        /// <param name="serviceType"></param>
        /// <param name="factory"></param>
        /// <returns></returns>
        public static IObjectContainer AddTransient(this IObjectContainer container, Type serviceType, Func<IServiceProvider, Object> factory)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            var item = new ServiceDescriptor
            {
                ServiceType = serviceType,
                Factory = factory,
                Lifetime = ObjectLifetime.Transient,
            };
            container.Add(item);

            return container;
        }

        /// <summary>添加瞬态实例，指定实现工厂</summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="container"></param>
        /// <param name="factory"></param>
        /// <returns></returns>
        public static IObjectContainer AddTransient<TService>(this IObjectContainer container, Func<IServiceProvider, Object> factory) where TService : class => container.AddTransient(typeof(TService), factory);

        /// <summary>添加瞬态实例，指定实现类型</summary>
        /// <param name="container"></param>
        /// <param name="serviceType"></param>
        /// <param name="implementationType"></param>
        /// <returns></returns>
        public static IObjectContainer TryAddTransient(this IObjectContainer container, Type serviceType, Type implementationType)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
            if (implementationType == null) throw new ArgumentNullException(nameof(implementationType));

            var item = new ServiceDescriptor
            {
                ServiceType = serviceType,
                ImplementationType = implementationType,
                Lifetime = ObjectLifetime.Transient,
            };
            container.TryAdd(item);

            return container;
        }
        #endregion

        #region 构建
        /// <summary>从对象容器创建服务提供者</summary>
        /// <param name="container"></param>
        /// <returns></returns>
        public static IServiceProvider BuildServiceProvider(this IObjectContainer container)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));

            return new ServiceProvider(container);
        }

        ///// <summary>从对象容器创建应用主机</summary>
        ///// <param name="container"></param>
        ///// <returns></returns>
        //public static IHost BuildHost(this IObjectContainer container)
        //{
        //    // 尝试注册应用主机，如果前面已经注册，则这里无效
        //    container.TryAddTransient(typeof(IHost), typeof(Host));

        //    //return new Host(container.BuildServiceProvider());
        //    return container.BuildServiceProvider().GetService(typeof(IHost)) as IHost;
        //}
        #endregion

        #region 旧版方法
        /// <summary>解析类型的实例</summary>
        /// <typeparam name="TService">接口类型</typeparam>
        /// <param name="container">对象容器</param>
        /// <returns></returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static TService Resolve<TService>(this IObjectContainer container) => (TService)container.Resolve(typeof(TService));
        #endregion
    }

    /// <summary>模型扩展</summary>
    public static class ModelExtension
    {
        /// <summary>获取指定类型的服务对象</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static T GetService<T>(this IServiceProvider provider)
        {
            if (provider == null) return default;

            //// 服务类是否当前类的基类
            //if (provider.GetType().As<T>()) return (T)provider;

            return (T)provider.GetService(typeof(T));
        }

        /// <summary>获取必要的服务，不存在时抛出异常</summary>
        /// <param name="provider"></param>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public static Object GetRequiredService(this IServiceProvider provider, Type serviceType)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            return serviceType == null
                ? throw new ArgumentNullException(nameof(serviceType))
                : provider.GetService(serviceType) ?? throw new InvalidOperationException($"未注册类型{serviceType.FullName}");
        }

        /// <summary>获取必要的服务，不存在时抛出异常</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static T GetRequiredService<T>(this IServiceProvider provider) => provider == null ? throw new ArgumentNullException(nameof(provider)) : (T)provider.GetRequiredService(typeof(T));

        /// <summary>获取一批服务</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static IEnumerable<T> GetServices<T>(this IServiceProvider provider) => provider.GetServices(typeof(T)).Cast<T>();

        /// <summary>获取一批服务</summary>
        /// <param name="provider"></param>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public static IEnumerable<Object> GetServices(this IServiceProvider provider, Type serviceType)
        {
            //var sp = provider as ServiceProvider;
            //if (sp == null && provider is MyServiceScope scope) sp = scope.MyServiceProvider as ServiceProvider;
            //var sp = provider.GetService<ServiceProvider>();
            //if (sp != null && sp.Container is ObjectContainer ioc)
            var ioc = GetService<ObjectContainer>(provider);
            if (ioc != null)
            {
                //var list = new List<Object>();
                //foreach (var item in ioc.Services)
                //{
                //    if (item.ServiceType == serviceType) list.Add(ioc.Resolve(item, provider));
                //}
                for (var i = ioc.Services.Count - 1; i >= 0; i--)
                {
                    var item = ioc.Services[i];
                    if (item.ServiceType == serviceType) yield return ioc.Resolve(item, provider);
                }
                //return list;
            }
            else
            {
                var serviceType2 = typeof(IEnumerable<>)!.MakeGenericType(serviceType);
                var enums = (IEnumerable<Object>)provider.GetRequiredService(serviceType2);
                foreach (var item in enums)
                {
                    yield return item;
                }
            }
        }

        /// <summary>创建范围作用域，该作用域内提供者解析一份数据</summary>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static IServiceScope CreateScope(this IServiceProvider provider) => provider.GetService<IServiceScopeFactory>()?.CreateScope();
    }
}
