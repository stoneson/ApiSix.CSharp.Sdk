using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Xml.Linq;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Linq;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Text;

namespace ApiSix.CSharp
{
    #region ICache
    /// <summary>轻量级生产者消费者接口</summary>
    /// <remarks>
    /// 不一定支持Ack机制；也不支持消息体与消息键分离
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    public interface IProducerConsumer<T>
    {
        /// <summary>元素个数</summary>
        Int32 Count { get; }

        /// <summary>集合是否为空</summary>
        Boolean IsEmpty { get; }

        /// <summary>生产添加</summary>
        /// <param name="values"></param>
        /// <returns></returns>
        Int32 Add(params T[] values);

        /// <summary>消费获取一批</summary>
        /// <param name="count"></param>
        /// <returns></returns>
        IEnumerable<T> Take(Int32 count = 1);

        /// <summary>消费获取一个</summary>
        /// <param name="timeout">超时。默认0秒，永久等待</param>
        /// <returns></returns>
        T TakeOne(Int32 timeout = 0);

        /// <summary>异步消费获取一个</summary>
        /// <param name="timeout">超时。单位秒，0秒表示永久等待</param>
        /// <returns></returns>
        Task<T> TakeOneAsync(Int32 timeout = 0);

        /// <summary>异步消费获取一个</summary>
        /// <param name="timeout">超时。单位秒，0秒表示永久等待</param>
        /// <param name="cancellationToken">取消通知</param>
        /// <returns></returns>
        Task<T> TakeOneAsync(Int32 timeout, CancellationToken cancellationToken);

        /// <summary>确认消费</summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        Int32 Acknowledge(params String[] keys);
    }

    /// <summary>缓存接口</summary>
    public interface ICache
    {
        #region 属性
        /// <summary>名称</summary>
        String Name { get; }

        /// <summary>默认缓存时间。默认0秒表示不过期</summary>
        Int32 Expire { get; set; }

        /// <summary>获取和设置缓存，永不过期</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Object this[String key] { get; set; }

        /// <summary>缓存个数</summary>
        Int32 Count { get; }

        /// <summary>所有键</summary>
        ICollection<String> Keys { get; }
        #endregion

        #region 基础操作
        /// <summary>是否包含缓存项</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Boolean ContainsKey(String key);

        /// <summary>设置缓存项</summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="expire">过期时间，秒。小于0时采用默认缓存时间<seealso cref="Expire"/></param>
        /// <returns></returns>
        Boolean Set<T>(String key, T value, Int32 expire = -1);

        /// <summary>设置缓存项</summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="expire">过期时间</param>
        /// <returns></returns>
        Boolean Set<T>(String key, T value, TimeSpan expire);

        /// <summary>获取缓存项</summary>
        /// <param name="key">键</param>
        /// <returns></returns>
        T Get<T>(String key);

        /// <summary>批量移除缓存项</summary>
        /// <param name="keys">键集合</param>
        /// <returns></returns>
        Int32 Remove(params String[] keys);

        /// <summary>清空所有缓存项</summary>
        void Clear();

        /// <summary>设置缓存项有效期</summary>
        /// <param name="key">键</param>
        /// <param name="expire">过期时间</param>
        Boolean SetExpire(String key, TimeSpan expire);

        /// <summary>获取缓存项有效期</summary>
        /// <param name="key">键</param>
        /// <returns></returns>
        TimeSpan GetExpire(String key);
        #endregion

        #region 集合操作
        /// <summary>批量获取缓存项</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="keys"></param>
        /// <returns></returns>
        IDictionary<String, T> GetAll<T>(IEnumerable<String> keys);

        /// <summary>批量设置缓存项</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <param name="expire">过期时间，秒。小于0时采用默认缓存时间<seealso cref="Expire"/></param>
        void SetAll<T>(IDictionary<String, T> values, Int32 expire = -1);

        /// <summary>获取列表</summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="key">键</param>
        /// <returns></returns>
        IList<T> GetList<T>(String key);

        /// <summary>获取哈希</summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="key">键</param>
        /// <returns></returns>
        IDictionary<String, T> GetDictionary<T>(String key);

        /// <summary>获取队列</summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="key">键</param>
        /// <returns></returns>
        IProducerConsumer<T> GetQueue<T>(String key);

        /// <summary>获取栈</summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="key">键</param>
        /// <returns></returns>
        IProducerConsumer<T> GetStack<T>(String key);

        /// <summary>获取Set</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        ICollection<T> GetSet<T>(String key);
        #endregion

        #region 高级操作
        /// <summary>添加，已存在时不更新</summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="expire">过期时间，秒。小于0时采用默认缓存时间<seealso cref="Cache.Expire"/></param>
        /// <returns></returns>
        Boolean Add<T>(String key, T value, Int32 expire = -1);

        /// <summary>设置新值并获取旧值，原子操作</summary>
        /// <remarks>
        /// 常常配合Increment使用，用于累加到一定数后重置归零，又避免多线程冲突。
        /// </remarks>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        T Replace<T>(String key, T value);

        /// <summary>尝试获取指定键，返回是否包含值。有可能缓存项刚好是默认值，或者只是反序列化失败，解决缓存穿透问题</summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="value">值。即使有值也不一定能够返回，可能缓存项刚好是默认值，或者只是反序列化失败</param>
        /// <returns>返回是否包含值，即使反序列化失败</returns>
        Boolean TryGetValue<T>(String key, out T value);

        /// <summary>获取 或 添加 缓存数据，在数据不存在时执行委托请求数据</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="callback"></param>
        /// <param name="expire">过期时间，秒。小于0时采用默认缓存时间<seealso cref="Cache.Expire"/></param>
        /// <returns></returns>
        T GetOrAdd<T>(String key, Func<String, T> callback, Int32 expire = -1);

        /// <summary>累加，原子操作</summary>
        /// <param name="key">键</param>
        /// <param name="value">变化量</param>
        /// <returns></returns>
        Int64 Increment(String key, Int64 value);

        /// <summary>累加，原子操作</summary>
        /// <param name="key">键</param>
        /// <param name="value">变化量</param>
        /// <returns></returns>
        Double Increment(String key, Double value);

        /// <summary>递减，原子操作</summary>
        /// <param name="key">键</param>
        /// <param name="value">变化量</param>
        /// <returns></returns>
        Int64 Decrement(String key, Int64 value);

        /// <summary>递减，原子操作</summary>
        /// <param name="key">键</param>
        /// <param name="value">变化量</param>
        /// <returns></returns>
        Double Decrement(String key, Double value);
        #endregion

        #region 事务
        /// <summary>提交变更。部分提供者需要刷盘</summary>
        /// <returns></returns>
        Int32 Commit();

        /// <summary>申请分布式锁</summary>
        /// <param name="key">要锁定的key</param>
        /// <param name="msTimeout">锁等待时间，单位毫秒</param>
        /// <returns></returns>
        IDisposable AcquireLock(String key, Int32 msTimeout);

        /// <summary>申请分布式锁</summary>
        /// <param name="key">要锁定的key</param>
        /// <param name="msTimeout">锁等待时间，申请加锁时如果遇到冲突则等待的最大时间，单位毫秒</param>
        /// <param name="msExpire">锁过期时间，超过该时间如果没有主动释放则自动释放锁，必须整数秒，单位毫秒</param>
        /// <param name="throwOnFailure">失败时是否抛出异常，如果不抛出异常，可通过返回null得知申请锁失败</param>
        /// <returns></returns>
        IDisposable AcquireLock(String key, Int32 msTimeout, Int32 msExpire, Boolean throwOnFailure);
        #endregion

        #region 性能测试
        /// <summary>多线程性能测试</summary>
        /// <param name="rand">随机读写。顺序，每个线程多次操作一个key；随机，每个线程每次操作不同key</param>
        /// <param name="batch">批量操作。默认0不分批，分批仅针对随机读写，对顺序读写的单key操作没有意义</param>
        Int64 Bench(Boolean rand = false, Int32 batch = 0);
        #endregion
    }

    /// <summary>分布式锁</summary>
    public class CacheLock : DisposeBase
    {
        private ICache Client { get; set; }

        /// <summary>
        /// 是否持有锁
        /// </summary>
        private Boolean _hasLock = false;

        /// <summary>键</summary>
        public String Key { get; set; }

        /// <summary>实例化</summary>
        /// <param name="client"></param>
        /// <param name="key"></param>
        public CacheLock(ICache client, String key)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (key.IsNullOrEmpty()) throw new ArgumentNullException(nameof(key));

            Client = client;
            Key = key;
        }

        /// <summary>申请锁</summary>
        /// <param name="msTimeout">锁等待时间，申请加锁时如果遇到冲突则等待的最大时间，单位毫秒</param>
        /// <param name="msExpire">锁过期时间，超过该时间如果没有主动释放则自动释放锁，必须整数秒，单位毫秒</param>
        /// <returns></returns>
        public Boolean Acquire(Int32 msTimeout, Int32 msExpire)
        {
            var ch = Client;
            var now = Runtime.TickCount64;

            // 循环等待
            var end = now + msTimeout;
            while (now < end)
            {
                // 申请加锁。没有冲突时可以直接返回
                var rs = ch.Add(Key, now + msExpire, msExpire / 1000);
                if (rs) return _hasLock = true;

                // 死锁超期检测
                var dt = ch.Get<Int64>(Key);
                if (dt <= now)
                {
                    // 开抢死锁。所有竞争者都会修改该锁的时间戳，但是只有一个能拿到旧的超时的值
                    var old = ch.Replace(Key, now + msExpire);
                    // 如果拿到超时值，说明抢到了锁。其它线程会抢到一个为超时的值
                    if (old <= dt)
                    {
                        ch.SetExpire(Key, TimeSpan.FromMilliseconds(msExpire));
                        return _hasLock = true;
                    }
                }

                // 没抢到，继续
                Thread.Sleep(200);

                now = Runtime.TickCount64;
            }

            return false;
        }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void Dispose(Boolean disposing)
        {
            base.Dispose(disposing);

            // 如果客户端已释放，则不删除
            if (Client is DisposeBase db && db.Disposed)
            {
            }
            else
            {
                if (_hasLock)
                {
                    Client.Remove(Key);
                }
            }
        }
    }

    /// <summary>缓存</summary>
    public abstract class Cache : DisposeBase, ICache
    {
        #region 静态默认实现
        /// <summary>默认缓存</summary>
        public static ICache Default { get; set; } = new MemoryCache();
        #endregion

        #region 属性
        /// <summary>名称</summary>
        public String Name { get; set; }

        /// <summary>默认过期时间。避免Set操作时没有设置过期时间，默认0秒表示不过期</summary>
        public Int32 Expire { get; set; }

        /// <summary>获取和设置缓存，使用默认过期时间</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public virtual Object this[String key] { get => Get<Object>(key); set => Set(key, value); }

        /// <summary>缓存个数</summary>
        public abstract Int32 Count { get; }

        /// <summary>所有键</summary>
        public abstract ICollection<String> Keys { get; }
        #endregion

        #region 构造
        /// <summary>构造函数</summary>
        protected Cache() => Name = GetType().Name.TrimEnd("Cache");
        #endregion

        #region 基础操作
        /// <summary>使用连接字符串初始化配置</summary>
        /// <param name="config"></param>
        public virtual void Init(String config) { }

        /// <summary>是否包含缓存项</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract Boolean ContainsKey(String key);

        /// <summary>设置缓存项</summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="expire">过期时间，秒</param>
        /// <returns></returns>
        public abstract Boolean Set<T>(String key, T value, Int32 expire = -1);

        /// <summary>设置缓存项</summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="expire">过期时间</param>
        /// <returns></returns>
        public virtual Boolean Set<T>(String key, T value, TimeSpan expire) => Set(key, value, (Int32)expire.TotalSeconds);

        /// <summary>获取缓存项</summary>
        /// <param name="key">键</param>
        /// <returns></returns>
        public abstract T Get<T>(String key);

        /// <summary>批量移除缓存项</summary>
        /// <param name="keys">键集合</param>
        /// <returns></returns>
        public abstract Int32 Remove(params String[] keys);

        /// <summary>清空所有缓存项</summary>
        public virtual void Clear() => throw new NotSupportedException();

        /// <summary>设置缓存项有效期</summary>
        /// <param name="key">键</param>
        /// <param name="expire">过期时间，秒</param>
        public abstract Boolean SetExpire(String key, TimeSpan expire);

        /// <summary>获取缓存项有效期</summary>
        /// <param name="key">键</param>
        /// <returns></returns>
        public abstract TimeSpan GetExpire(String key);
        #endregion

        #region 集合操作
        /// <summary>批量获取缓存项</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="keys"></param>
        /// <returns></returns>
        public virtual IDictionary<String, T> GetAll<T>(IEnumerable<String> keys)
        {
            var dic = new Dictionary<String, T>();
            foreach (var key in keys)
            {
                dic[key] = Get<T>(key);
            }

            return dic;
        }

        /// <summary>批量设置缓存项</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <param name="expire">过期时间，秒</param>
        public virtual void SetAll<T>(IDictionary<String, T> values, Int32 expire = -1)
        {
            foreach (var item in values)
            {
                Set(item.Key, item.Value, expire);
            }
        }

        /// <summary>获取列表</summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="key">键</param>
        /// <returns></returns>
        public virtual IList<T> GetList<T>(String key) => throw new NotSupportedException();

        /// <summary>获取哈希</summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="key">键</param>
        /// <returns></returns>
        public virtual IDictionary<String, T> GetDictionary<T>(String key) => throw new NotSupportedException();

        /// <summary>获取队列</summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="key">键</param>
        /// <returns></returns>
        public virtual IProducerConsumer<T> GetQueue<T>(String key) => throw new NotSupportedException();

        /// <summary>获取栈</summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="key">键</param>
        /// <returns></returns>
        public virtual IProducerConsumer<T> GetStack<T>(String key) => throw new NotSupportedException();

        /// <summary>获取Set</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public virtual ICollection<T> GetSet<T>(String key) => throw new NotSupportedException();
        #endregion

        #region 高级操作
        /// <summary>添加，已存在时不更新</summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="expire">过期时间，秒</param>
        /// <returns></returns>
        public virtual Boolean Add<T>(String key, T value, Int32 expire = -1)
        {
            if (ContainsKey(key)) return false;

            return Set(key, value, expire);
        }

        /// <summary>设置新值并获取旧值，原子操作</summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        public virtual T Replace<T>(String key, T value)
        {
            var rs = Get<T>(key);
            Set(key, value);
            return rs;
        }

        /// <summary>尝试获取指定键，返回是否包含值。有可能缓存项刚好是默认值，或者只是反序列化失败</summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="value">值。即使有值也不一定能够返回，可能缓存项刚好是默认值，或者只是反序列化失败</param>
        /// <returns>返回是否包含值，即使反序列化失败</returns>
        public virtual Boolean TryGetValue<T>(String key, out T value)
        {
            value = Get<T>(key);
            if (!Equals(value, default)) return true;

            return ContainsKey(key);
        }

        /// <summary>获取 或 添加 缓存数据，在数据不存在时执行委托请求数据</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="callback"></param>
        /// <param name="expire">过期时间，秒。小于0时采用默认缓存时间<seealso cref="Cache.Expire"/></param>
        /// <returns></returns>
        public virtual T GetOrAdd<T>(String key, Func<String, T> callback, Int32 expire = -1)
        {
            var value = Get<T>(key);
            if (!Equals(value, default)) return value;

            if (ContainsKey(key)) return value;

            value = callback(key);

            if (expire < 0) expire = Expire;
            if (Add(key, value, expire)) return value;

            return Get<T>(key);
        }

        /// <summary>累加，原子操作</summary>
        /// <param name="key">键</param>
        /// <param name="value">变化量</param>
        /// <returns></returns>
        public virtual Int64 Increment(String key, Int64 value)
        {
            lock (this)
            {
                var v = Get<Int64>(key);
                v += value;
                Set(key, v);

                return v;
            }
        }

        /// <summary>累加，原子操作</summary>
        /// <param name="key">键</param>
        /// <param name="value">变化量</param>
        /// <returns></returns>
        public virtual Double Increment(String key, Double value)
        {
            lock (this)
            {
                var v = Get<Double>(key);
                v += value;
                Set(key, v);

                return v;
            }
        }

        /// <summary>递减，原子操作</summary>
        /// <param name="key">键</param>
        /// <param name="value">变化量</param>
        /// <returns></returns>
        public virtual Int64 Decrement(String key, Int64 value)
        {
            lock (this)
            {
                var v = Get<Int64>(key);
                v -= value;
                Set(key, v);

                return v;
            }
        }

        /// <summary>递减，原子操作</summary>
        /// <param name="key">键</param>
        /// <param name="value">变化量</param>
        /// <returns></returns>
        public virtual Double Decrement(String key, Double value)
        {
            lock (this)
            {
                var v = Get<Double>(key);
                v -= value;
                Set(key, v);

                return v;
            }
        }
        #endregion

        #region 事务
        /// <summary>提交变更。部分提供者需要刷盘</summary>
        /// <returns></returns>
        public virtual Int32 Commit() => 0;

        /// <summary>申请分布式锁</summary>
        /// <param name="key">要锁定的key</param>
        /// <param name="msTimeout">锁等待时间，单位毫秒</param>
        /// <returns></returns>
        public IDisposable AcquireLock(String key, Int32 msTimeout)
        {
            var rlock = new CacheLock(this, key);
            if (!rlock.Acquire(msTimeout, msTimeout)) throw new InvalidOperationException($"锁定[{key}]失败！msTimeout={msTimeout}");

            return rlock;
        }

        /// <summary>申请分布式锁</summary>
        /// <param name="key">要锁定的key</param>
        /// <param name="msTimeout">锁等待时间，申请加锁时如果遇到冲突则等待的最大时间，单位毫秒</param>
        /// <param name="msExpire">锁过期时间，超过该时间如果没有主动释放则自动释放锁，必须整数秒，单位毫秒</param>
        /// <param name="throwOnFailure">失败时是否抛出异常，如果不抛出异常，可通过返回null得知申请锁失败</param>
        /// <returns></returns>
        public IDisposable AcquireLock(String key, Int32 msTimeout, Int32 msExpire, Boolean throwOnFailure)
        {
            var rlock = new CacheLock(this, key);
            if (!rlock.Acquire(msTimeout, msExpire))
            {
                if (throwOnFailure) throw new InvalidOperationException($"锁定[{key}]失败！msTimeout={msTimeout}");

                return null;
            }

            return rlock;
        }
        #endregion

        #region 性能测试
        /// <summary>多线程性能测试</summary>
        /// <param name="rand">随机读写。顺序，每个线程多次操作一个key；随机，每个线程每次操作不同key</param>
        /// <param name="batch">批量操作。默认0不分批，分批仅针对随机读写，对顺序读写的单key操作没有意义</param>
        /// <remarks>
        /// Memory性能测试[顺序]，逻辑处理器 32 个 2,000MHz Intel(R) Xeon(R) CPU E5-2640 v2 @ 2.00GHz
        /// 
        /// 测试 10,000,000 项，  1 线程
        /// 赋值 10,000,000 项，  1 线程，耗时   3,764ms 速度 2,656,748 ops
        /// 读取 10,000,000 项，  1 线程，耗时   1,296ms 速度 7,716,049 ops
        /// 删除 10,000,000 项，  1 线程，耗时   1,230ms 速度 8,130,081 ops
        /// 
        /// 测试 20,000,000 项，  2 线程
        /// 赋值 20,000,000 项，  2 线程，耗时   3,088ms 速度 6,476,683 ops
        /// 读取 20,000,000 项，  2 线程，耗时   1,051ms 速度 19,029,495 ops
        /// 删除 20,000,000 项，  2 线程，耗时   1,011ms 速度 19,782,393 ops
        /// 
        /// 测试 40,000,000 项，  4 线程
        /// 赋值 40,000,000 项，  4 线程，耗时   3,060ms 速度 13,071,895 ops
        /// 读取 40,000,000 项，  4 线程，耗时   1,023ms 速度 39,100,684 ops
        /// 删除 40,000,000 项，  4 线程，耗时     994ms 速度 40,241,448 ops
        /// 
        /// 测试 80,000,000 项，  8 线程
        /// 赋值 80,000,000 项，  8 线程，耗时   3,124ms 速度 25,608,194 ops
        /// 读取 80,000,000 项，  8 线程，耗时   1,171ms 速度 68,317,677 ops
        /// 删除 80,000,000 项，  8 线程，耗时   1,199ms 速度 66,722,268 ops
        /// 
        /// 测试 320,000,000 项， 32 线程
        /// 赋值 320,000,000 项， 32 线程，耗时  13,857ms 速度 23,093,021 ops
        /// 读取 320,000,000 项， 32 线程，耗时   1,950ms 速度 164,102,564 ops
        /// 删除 320,000,000 项， 32 线程，耗时   3,359ms 速度 95,266,448 ops
        /// 
        /// 测试 320,000,000 项， 64 线程
        /// 赋值 320,000,000 项， 64 线程，耗时   9,648ms 速度 33,167,495 ops
        /// 读取 320,000,000 项， 64 线程，耗时   1,974ms 速度 162,107,396 ops
        /// 删除 320,000,000 项， 64 线程，耗时   1,907ms 速度 167,802,831 ops
        /// 
        /// 测试 320,000,000 项，256 线程
        /// 赋值 320,000,000 项，256 线程，耗时  12,429ms 速度 25,746,238 ops
        /// 读取 320,000,000 项，256 线程，耗时   1,907ms 速度 167,802,831 ops
        /// 删除 320,000,000 项，256 线程，耗时   2,350ms 速度 136,170,212 ops
        /// </remarks>
        public virtual Int64 Bench(Boolean rand = false, Int32 batch = 0)
        {
            var cpu = Environment.ProcessorCount;
            XTrace.WriteLine($"{Name}性能测试[{(rand ? "随机" : "顺序")}]，批大小[{batch}]，逻辑处理器 {cpu:n0} 个");

            var rs = 0L;
            var times = 10_000;

            // 单线程
            rs += BenchOne(times, 1, rand, batch);

            // 多线程
            if (cpu != 2) rs += BenchOne(times * 2, 2, rand, batch);
            if (cpu != 4) rs += BenchOne(times * 4, 4, rand, batch);
            if (cpu != 8) rs += BenchOne(times * 8, 8, rand, batch);

            // CPU个数
            rs += BenchOne(times * cpu, cpu, rand, batch);

            //// 1.5倍
            //var cpu2 = cpu * 3 / 2;
            //if (!(new[] { 2, 4, 8, 64, 256 }).Contains(cpu2)) BenchOne(times * cpu2, cpu2, rand);

            // 最大
            if (cpu < 64) rs += BenchOne(times * cpu, 64, rand, batch);
            //if (cpu * 8 >= 256) BenchOne(times * cpu, cpu * 8, rand);

            return rs;
        }

        /// <summary>使用指定线程测试指定次数</summary>
        /// <param name="times">次数</param>
        /// <param name="threads">线程</param>
        /// <param name="rand">随机读写</param>
        /// <param name="batch">批量操作</param>
        public virtual Int64 BenchOne(Int64 times, Int32 threads, Boolean rand, Int32 batch)
        {
            if (threads <= 0) threads = Environment.ProcessorCount;
            if (times <= 0) times = threads * 1_000;

            //XTrace.WriteLine("");
            XTrace.WriteLine($"测试 {times:n0} 项，{threads,3:n0} 线程");

            var rs = 3L;

            //提前执行一次网络操作，预热链路
            var key = "bstr_";
            Set(key, Rand.NextString(32));
            _ = Get<String>(key);
            Remove(key);

            // 赋值测试
            rs += BenchSet(key, times, threads, rand, batch);

            // 读取测试
            rs += BenchGet(key, times, threads, rand, batch);

            // 删除测试
            rs += BenchRemove(key, times, threads, rand, batch);

            // 累加测试
            key = "bint_";
            rs += BenchInc(key, times, threads, rand, batch);

            return rs;
        }

        /// <summary>读取测试</summary>
        /// <param name="key">键</param>
        /// <param name="times">次数</param>
        /// <param name="threads">线程</param>
        /// <param name="rand">随机读写</param>
        /// <param name="batch">批量操作</param>
        protected virtual Int64 BenchGet(String key, Int64 times, Int32 threads, Boolean rand, Int32 batch)
        {
            //提前执行一次网络操作，预热链路
            var v = Get<String>(key);

            var sw = Stopwatch.StartNew();
            if (rand)
            {
                // 随机操作，每个线程每次操作不同key，跳跃式
                Parallel.For(0, threads, k =>
                {
                    if (batch == 0)
                    {
                        for (var i = k; i < times; i += threads)
                        {
                            var val = Get<String>(key + i);
                        }
                    }
                    else
                    {
                        var n = 0;
                        var keys = new String[batch];
                        for (var i = k; i < times; i += threads)
                        {
                            keys[n++] = key + i;

                            if (n >= batch)
                            {
                                var vals = GetAll<String>(keys);
                                n = 0;
                            }
                        }
                        if (n > 0)
                        {
                            var vals = GetAll<String>(keys.Take(n));
                        }
                    }
                });
            }
            else
            {
                // 顺序操作，每个线程多次操作同一个key
                Parallel.For(0, threads, k =>
                {
                    var mykey = key + k;
                    var count = times / threads;
                    for (var i = 0; i < count; i++)
                    {
                        var val = Get<String>(mykey);
                    }
                });
            }
            sw.Stop();

            var speed = times * 1000 / sw.ElapsedMilliseconds;
            XTrace.WriteLine($"读取 耗时 {sw.ElapsedMilliseconds,7:n0}ms 速度 {speed,9:n0} ops");

            return times + 1;
        }

        /// <summary>赋值测试</summary>
        /// <param name="key">键</param>
        /// <param name="times">次数</param>
        /// <param name="threads">线程</param>
        /// <param name="rand">随机读写</param>
        /// <param name="batch">批量操作</param>
        protected virtual Int64 BenchSet(String key, Int64 times, Int32 threads, Boolean rand, Int32 batch)
        {
            Set(key, Rand.NextString(32));

            var sw = Stopwatch.StartNew();
            if (rand)
            {
                // 随机操作，每个线程每次操作不同key，跳跃式
                Parallel.For(0, threads, k =>
                {
                    var val = Rand.NextString(8);
                    if (batch == 0)
                    {
                        for (var i = k; i < times; i += threads)
                        {
                            Set(key + i, val);
                        }
                    }
                    else
                    {
                        var n = 0;
                        var dic = new Dictionary<String, String>();
                        for (var i = k; i < times; i += threads)
                        {
                            dic[key + i] = val;
                            n++;

                            if (n >= batch)
                            {
                                SetAll(dic);
                                dic.Clear();
                                n = 0;
                            }
                        }
                        if (n > 0)
                        {
                            SetAll(dic);
                        }
                    }

                    // 提交变更
                    Commit();
                });
            }
            else
            {
                // 顺序操作，每个线程多次操作同一个key
                Parallel.For(0, threads, k =>
                {
                    var mykey = key + k;
                    var val = Rand.NextString(8);
                    var count = times / threads;
                    for (var i = 0; i < count; i++)
                    {
                        Set(mykey, val);
                    }

                    // 提交变更
                    Commit();
                });
            }
            sw.Stop();

            var speed = times * 1000 / sw.ElapsedMilliseconds;
            XTrace.WriteLine($"赋值 耗时 {sw.ElapsedMilliseconds,7:n0}ms 速度 {speed,9:n0} ops");

            return times + 1;
        }

        /// <summary>删除测试</summary>
        /// <param name="key">键</param>
        /// <param name="times">次数</param>
        /// <param name="threads">线程</param>
        /// <param name="rand">随机读写</param>
        /// <param name="batch">批量操作</param>
        protected virtual Int64 BenchRemove(String key, Int64 times, Int32 threads, Boolean rand, Int32 batch)
        {
            //提前执行一次网络操作，预热链路
            Remove(key);

            var sw = Stopwatch.StartNew();
            if (rand)
            {
                // 随机操作，每个线程每次操作不同key，跳跃式
                Parallel.For(0, threads, k =>
                {
                    if (batch == 0)
                    {
                        for (var i = k; i < times; i += threads)
                        {
                            Remove(key + i);
                        }
                    }
                    else
                    {
                        var n = 0;
                        var keys = new String[batch];
                        for (var i = k; i < times; i += threads)
                        {
                            keys[n++] = key + i;

                            if (n >= batch)
                            {
                                Remove(keys);
                                n = 0;
                            }
                        }
                        if (n > 0)
                        {
                            Remove(keys.Take(n).ToArray());
                        }
                    }

                    // 提交变更
                    Commit();
                });
            }
            else
            {
                // 顺序操作，每个线程多次操作同一个key
                Parallel.For(0, threads, k =>
                {
                    var mykey = key + k;
                    var count = times / threads;
                    for (var i = 0; i < count; i++)
                    {
                        Remove(mykey);
                    }

                    // 提交变更
                    Commit();
                });
            }
            sw.Stop();

            var speed = times * 1000 / sw.ElapsedMilliseconds;
            XTrace.WriteLine($"删除 耗时 {sw.ElapsedMilliseconds,7:n0}ms 速度 {speed,9:n0} ops");

            return times + 1;
        }

        /// <summary>累加测试</summary>
        /// <param name="key">键</param>
        /// <param name="times">次数</param>
        /// <param name="threads">线程</param>
        /// <param name="rand">随机读写</param>
        /// <param name="batch">批量操作</param>
        protected virtual Int64 BenchInc(String key, Int64 times, Int32 threads, Boolean rand, Int32 batch)
        {
            //提前执行一次网络操作，预热链路
            Increment(key, 1);

            var sw = Stopwatch.StartNew();
            if (rand)
            {
                // 随机操作，每个线程每次操作不同key，跳跃式
                Parallel.For(0, threads, k =>
                {
                    var val = Rand.Next(100);
                    for (var i = k; i < times; i += threads)
                    {
                        Increment(key + i, val);
                    }

                    // 提交变更
                    Commit();
                });
            }
            else
            {
                // 顺序操作，每个线程多次操作同一个key
                Parallel.For(0, threads, k =>
                {
                    var mykey = key + k;
                    var val = Rand.Next(100);
                    var count = times / threads;
                    for (var i = 0; i < count; i++)
                    {
                        Increment(mykey, val);
                    }

                    // 提交变更
                    Commit();
                });
            }
            sw.Stop();

            var speed = times * 1000 / sw.ElapsedMilliseconds;
            XTrace.WriteLine($"累加 耗时 {sw.ElapsedMilliseconds,7:n0}ms 速度 {speed,9:n0} ops");

            return times + 1;
        }
        #endregion

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString() => Name;
        #endregion
    }
    #endregion

    #region MemoryCache
    /// <summary>默认字典缓存</summary>
    public class MemoryCache : Cache
    {
        #region 属性
        /// <summary>缓存核心</summary>
        protected ConcurrentDictionary<String, CacheItem> _cache;

        /// <summary>容量。容量超标时，采用LRU机制删除，默认100_000</summary>
        public Int32 Capacity { get; set; } = 100_000;

        /// <summary>定时清理时间，默认60秒</summary>
        public Int32 Period { get; set; } = 60;

        /// <summary>缓存键过期</summary>
        public event EventHandler<EventArgs<String>> KeyExpired;
        #endregion

        #region 静态默认实现
        /// <summary>默认缓存</summary>
        public static ICache Instance { get; set; } = new MemoryCache();
        #endregion

        #region 构造
        /// <summary>实例化一个内存字典缓存</summary>
        public MemoryCache()
        {
            _cache = new ConcurrentDictionary<String, CacheItem>();
            Name = "Memory";

            Init(null);
        }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void Dispose(Boolean disposing)
        {
            base.Dispose(disposing);

            clearTimer.TryDispose();
            clearTimer = null;
        }
        #endregion

        #region 缓存属性
        private Int32 _count;
        /// <summary>缓存项。原子计数</summary>
        public override Int32 Count => _count;

        /// <summary>所有键。实际返回只读列表新实例，数据量较大时注意性能</summary>
        public override ICollection<String> Keys => _cache.Keys;
        #endregion

        #region 方法
        /// <summary>初始化配置</summary>
        /// <param name="config"></param>
        public override void Init(String config)
        {
            if (clearTimer == null)
            {
                var period = Period;
                clearTimer = new TimerX(RemoveNotAlive, null, 10 * 1000, period * 1000)
                {
                    Async = true,
                    CanExecute = () => _cache.Any(),
                };
            }
        }

        /// <summary>获取或添加缓存项</summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="expire">过期时间，秒</param>
        /// <returns></returns>
        public virtual T GetOrAdd<T>(String key, T value, Int32 expire = -1)
        {
            if (expire < 0) expire = Expire;

            CacheItem ci = null;
            do
            {
                if (_cache.TryGetValue(key, out var item)) return (T)item.Visit();

                if (ci == null) ci = new CacheItem(value, expire);
            } while (!_cache.TryAdd(key, ci));

            Interlocked.Increment(ref _count);

            return (T)ci.Visit();
        }
        #endregion

        #region 基本操作
        /// <summary>是否包含缓存项</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public override Boolean ContainsKey(String key) => _cache.TryGetValue(key, out var item) && item != null && !item.Expired;

        /// <summary>添加缓存项，已存在时更新</summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="expire">过期时间，秒</param>
        /// <returns></returns>
        public override Boolean Set<T>(String key, T value, Int32 expire = -1)
        {
            if (expire < 0) expire = Expire;

            //_cache.AddOrUpdate(key,
            //    k => new CacheItem(value, expire),
            //    (k, item) =>
            //    {
            //        item.Value = value;
            //        item.ExpiredTime = DateTime.Now.AddSeconds(expire);

            //        return item;
            //    });

            // 不用AddOrUpdate，避免匿名委托带来的GC损耗
            CacheItem ci = null;
            do
            {
                if (_cache.TryGetValue(key, out var item))
                {
                    item.Set(value, expire);
                    return true;
                }

                if (ci == null) ci = new CacheItem(value, expire);
            } while (!_cache.TryAdd(key, ci));

            Interlocked.Increment(ref _count);

            return true;
        }

        /// <summary>获取缓存项，不存在时返回默认值</summary>
        /// <param name="key">键</param>
        /// <returns></returns>
        public override T Get<T>(String key)
        {
            if (!_cache.TryGetValue(key, out var item) || item == null || item.Expired) return default;

            return item.Visit().ChangeType<T>();
        }

        /// <summary>批量移除缓存项</summary>
        /// <param name="keys">键集合</param>
        /// <returns>实际移除个数</returns>
        public override Int32 Remove(params String[] keys)
        {
            var count = 0;
            foreach (var k in keys)
            {
                if (_cache.TryRemove(k, out _))
                {
                    count++;

                    Interlocked.Decrement(ref _count);
                }
            }
            return count;
        }

        /// <summary>清空所有缓存项</summary>
        public override void Clear()
        {
            _cache.Clear();
            _count = 0;
        }

        /// <summary>设置缓存项有效期</summary>
        /// <param name="key">键</param>
        /// <param name="expire">过期时间</param>
        /// <returns>设置是否成功</returns>
        public override Boolean SetExpire(String key, TimeSpan expire)
        {
            if (!_cache.TryGetValue(key, out var item) || item == null) return false;

            item.ExpiredTime = Runtime.TickCount64 + (Int64)expire.TotalMilliseconds;

            return true;
        }

        /// <summary>获取缓存项有效期，不存在时返回Zero</summary>
        /// <param name="key">键</param>
        /// <returns></returns>
        public override TimeSpan GetExpire(String key)
        {
            if (!_cache.TryGetValue(key, out var item) || item == null) return TimeSpan.Zero;

            return TimeSpan.FromMilliseconds(item.ExpiredTime - Runtime.TickCount64);
        }
        #endregion

        #region 高级操作
        /// <summary>添加，已存在时不更新，常用于锁争夺</summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="expire">过期时间，秒</param>
        /// <returns></returns>
        public override Boolean Add<T>(String key, T value, Int32 expire = -1)
        {
            if (expire < 0) expire = Expire;

            CacheItem ci = null;
            do
            {
                if (_cache.TryGetValue(key, out _)) return false;

                if (ci == null) ci = new CacheItem(value, expire);
            } while (!_cache.TryAdd(key, ci));

            Interlocked.Increment(ref _count);

            return true;
        }

        /// <summary>设置新值并获取旧值，原子操作</summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        public override T Replace<T>(String key, T value)
        {
            var expire = Expire;

            CacheItem ci = null;
            do
            {
                if (_cache.TryGetValue(key, out var item))
                {
                    var rs = item.Value;
                    // 如果已经过期，不要返回旧值
                    if (item.Expired) rs = default(T);
                    item.Set(value, expire);
                    return (T)rs;
                }

                if (ci == null) ci = new CacheItem(value, expire);
            } while (!_cache.TryAdd(key, ci));

            Interlocked.Increment(ref _count);

            return default;
        }

        /// <summary>尝试获取指定键，返回是否包含值。有可能缓存项刚好是默认值，或者只是反序列化失败</summary>
        /// <remarks>
        /// 在 MemoryCache 中，如果某个key过期，在清理之前仍然可以通过TryGet访问，并且更新访问时间，避免被清理。
        /// </remarks>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="value">值。即使有值也不一定能够返回，可能缓存项刚好是默认值，或者只是反序列化失败</param>
        /// <returns>返回是否包含值，即使反序列化失败</returns>
        public override Boolean TryGetValue<T>(String key, out T value)
        {
            value = default;

            // 没有值，直接结束
            if (!_cache.TryGetValue(key, out var item) || item == null) return false;

            // 得到已有值
            value = item.Visit().ChangeType<T>();

            // 是否未过期的有效值
            return !item.Expired;
        }

        /// <summary>获取 或 添加 缓存数据，在数据不存在时执行委托请求数据</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="callback"></param>
        /// <param name="expire">过期时间，秒。小于0时采用默认缓存时间<seealso cref="Cache.Expire"/></param>
        /// <returns></returns>
        public override T GetOrAdd<T>(String key, Func<String, T> callback, Int32 expire = -1)
        {
            if (expire < 0) expire = Expire;

            CacheItem ci = null;
            do
            {
                if (_cache.TryGetValue(key, out var item)) return (T)item.Visit();

                if (ci == null) ci = new CacheItem(callback(key), expire);
            } while (!_cache.TryAdd(key, ci));

            Interlocked.Increment(ref _count);

            return (T)ci.Visit();
        }

        /// <summary>累加，原子操作</summary>
        /// <param name="key">键</param>
        /// <param name="value">变化量</param>
        /// <returns></returns>
        public override Int64 Increment(String key, Int64 value)
        {
            var item = GetOrAddItem(key, k => 0L);
            return item.Inc(value);
        }

        /// <summary>累加，原子操作</summary>
        /// <param name="key">键</param>
        /// <param name="value">变化量</param>
        /// <returns></returns>
        public override Double Increment(String key, Double value)
        {
            var item = GetOrAddItem(key, k => 0d);
            return item.Inc(value);
        }

        /// <summary>递减，原子操作</summary>
        /// <param name="key">键</param>
        /// <param name="value">变化量</param>
        /// <returns></returns>
        public override Int64 Decrement(String key, Int64 value)
        {
            var item = GetOrAddItem(key, k => 0L);
            return item.Dec(value);
        }

        /// <summary>递减，原子操作</summary>
        /// <param name="key">键</param>
        /// <param name="value">变化量</param>
        /// <returns></returns>
        public override Double Decrement(String key, Double value)
        {
            var item = GetOrAddItem(key, k => 0d);
            return item.Dec(value);
        }
        #endregion

        #region 集合操作
        /// <summary>获取列表</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public override IList<T> GetList<T>(String key)
        {
            var item = GetOrAddItem(key, k => new List<T>());
            return item.Visit() as IList<T>;
        }

        /// <summary>获取哈希</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public override IDictionary<String, T> GetDictionary<T>(String key)
        {
            var item = GetOrAddItem(key, k => new ConcurrentDictionary<String, T>());
            return item.Visit() as IDictionary<String, T>;
        }

        /// <summary>获取队列</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public override IProducerConsumer<T> GetQueue<T>(String key)
        {
            var item = GetOrAddItem(key, k => new MemoryQueue<T>());
            return item.Visit() as IProducerConsumer<T>;
        }

        /// <summary>获取栈</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public override IProducerConsumer<T> GetStack<T>(String key)
        {
            var item = GetOrAddItem(key, k => new MemoryQueue<T>(new ConcurrentStack<T>()));
            return item.Visit() as IProducerConsumer<T>;
        }

        /// <summary>获取Set</summary>
        /// <remarks>基于HashSet，非线程安全</remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public override ICollection<T> GetSet<T>(String key)
        {
            var item = GetOrAddItem(key, k => new HashSet<T>());
            return item.Visit() as ICollection<T>;
        }

        /// <summary>获取 或 添加 缓存项</summary>
        /// <param name="key"></param>
        /// <param name="valueFactory"></param>
        /// <returns></returns>
        protected CacheItem GetOrAddItem(String key, Func<String, Object> valueFactory)
        {
            var expire = Expire;

            CacheItem ci = null;
            do
            {
                if (_cache.TryGetValue(key, out var item)) return item;

                if (ci == null) ci = new CacheItem(valueFactory(key), expire);
            } while (!_cache.TryAdd(key, ci));

            Interlocked.Increment(ref _count);

            return ci;
        }
        #endregion

        #region 缓存项
        /// <summary>缓存项</summary>
        protected class CacheItem
        {
            private Object _Value;
            /// <summary>数值</summary>
            public Object Value { get => _Value; set => _Value = value; }

            /// <summary>过期时间</summary>
            public Int64 ExpiredTime { get; set; }

            /// <summary>是否过期</summary>
            public Boolean Expired => ExpiredTime <= Runtime.TickCount64;

            /// <summary>访问时间</summary>
            public Int64 VisitTime { get; private set; }

            /// <summary>构造缓存项</summary>
            /// <param name="value"></param>
            /// <param name="expire"></param>
            public CacheItem(Object value, Int32 expire) => Set(value, expire);

            /// <summary>设置数值和过期时间</summary>
            /// <param name="value"></param>
            /// <param name="expire">过期时间，秒</param>
            public void Set(Object value, Int32 expire)
            {
                Value = value;

                var now = VisitTime = Runtime.TickCount64;
                if (expire <= 0)
                    ExpiredTime = Int64.MaxValue;
                else
                    ExpiredTime = now + expire * 1000L;
            }

            /// <summary>更新访问时间并返回数值</summary>
            /// <returns></returns>
            public Object Visit()
            {
                VisitTime = Runtime.TickCount64;
                return Value;
            }

            /// <summary>递增</summary>
            /// <param name="value"></param>
            /// <returns></returns>
            public Int64 Inc(Int64 value)
            {
                // 原子操作
                Int64 newValue;
                Object oldValue;
                do
                {
                    oldValue = _Value ?? 0;
                    newValue = oldValue.ToLong() + value.ToLong();
                } while (Interlocked.CompareExchange(ref _Value, newValue, oldValue) != oldValue);

                Visit();

                return newValue;
            }

            /// <summary>递增</summary>
            /// <param name="value"></param>
            /// <returns></returns>
            public Double Inc(Double value)
            {
                // 原子操作
                Double newValue;
                Object oldValue;
                do
                {
                    oldValue = _Value ?? 0;
                    newValue = oldValue.ToDouble() + value.ToDouble();
                } while (Interlocked.CompareExchange(ref _Value, newValue, oldValue) != oldValue);

                Visit();

                return newValue;
            }

            /// <summary>递减</summary>
            /// <param name="value"></param>
            /// <returns></returns>
            public Int64 Dec(Int64 value)
            {
                // 原子操作
                Int64 newValue;
                Object oldValue;
                do
                {
                    oldValue = _Value ?? 0;
                    newValue = oldValue.ToLong() - value.ToLong();
                } while (Interlocked.CompareExchange(ref _Value, newValue, oldValue) != oldValue);

                Visit();

                return newValue;
            }

            /// <summary>递减</summary>
            /// <param name="value"></param>
            /// <returns></returns>
            public Double Dec(Double value)
            {
                // 原子操作
                Double newValue;
                Object oldValue;
                do
                {
                    oldValue = _Value ?? 0;
                    newValue = oldValue.ToDouble() - value.ToDouble();
                } while (Interlocked.CompareExchange(ref _Value, newValue, oldValue) != oldValue);

                Visit();

                return newValue;
            }
        }
        #endregion

        #region 清理过期缓存
        /// <summary>清理会话计时器</summary>
        private TimerX clearTimer;

        /// <summary>移除过期的缓存项</summary>
        private void RemoveNotAlive(Object state)
        {
            var tx = clearTimer;
            if (tx != null /*&& tx.Period == 60_000*/) tx.Period = Period * 1000;

            var dic = _cache;
            if (_count == 0 && !dic.Any()) return;

            // 过期时间升序，用于缓存满以后删除
            var slist = new SortedList<Int64, IList<String>>();
            // 超出个数
            var flag = true;
            if (Capacity <= 0 || _count <= Capacity) flag = false;

            // 60分钟之内过期的数据，进入LRU淘汰
            var now = Runtime.TickCount64;
            var exp = now + 3600_000;
            var k = 0;

            // 这里先计算，性能很重要
            var list = new List<String>();
            foreach (var item in dic)
            {
                var ci = item.Value;
                if (ci.ExpiredTime <= now)
                    list.Add(item.Key);
                else
                {
                    k++;
                    if (flag && ci.ExpiredTime < exp)
                    {
                        if (!slist.TryGetValue(ci.VisitTime, out var ss))
                            slist.Add(ci.VisitTime, ss = new List<String>());

                        ss.Add(item.Key);
                    }
                }
            }

            // 如果满了，删除前面
            if (flag && slist.Count > 0 && _count - list.Count > Capacity)
            {
                var over = _count - list.Count - Capacity;
                for (var i = 0; i < slist.Count && over > 0; i++)
                {
                    var ss = slist.Values[i];
                    if (ss != null && ss.Count > 0)
                    {
                        foreach (var item in ss)
                        {
                            if (over <= 0) break;

                            list.Add(item);
                            over--;
                            k--;
                        }
                    }
                }

                XTrace.WriteLine("[{0}]满，{1:n0}>{2:n0}，删除[{3:n0}]个", Name, _count, Capacity, list.Count);
            }

            foreach (var item in list)
            {
                OnExpire(item);
                _cache.Remove(item);
            }

            // 修正
            _count = k;
        }

        /// <summary>缓存过期</summary>
        /// <param name="key"></param>
        protected virtual void OnExpire(String key) => KeyExpired?.Invoke(this, new EventArgs<String>(key));
        #endregion

        #region 持久化
        private const String MAGIC = "apisixCache";
        private const Byte _Ver = 1;
        /// <summary>保存到数据流</summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public void Save(Stream stream)
        {
            var bn = new Binary
            {
                Stream = stream,
                EncodeInt = true,
            };

            // 头部，幻数、版本和标记
            bn.Write(MAGIC.GetBytes(), 0, MAGIC.Length);
            bn.Write(_Ver);
            bn.Write(0);

            bn.WriteSize(_cache.Count);
            foreach (var item in _cache)
            {
                var ci = item.Value;

                // Key+Expire+Empty
                // Key+Expire+TypeCode+Value
                // Key+Expire+TypeCode+Type+Length+Value
                bn.Write(item.Key);
                bn.Write((Int32)(ci.ExpiredTime / 1000));

                var type = ci.Value?.GetType();
                if (type == null)
                {
                    bn.Write((Byte)TypeCode.Empty);
                }
                else
                {
                    var code = type.GetTypeCode();
                    bn.Write((Byte)code);

                    if (code != TypeCode.Object)
                        bn.Write(ci.Value);
                    else
                    {
                        bn.Write(type.FullName);
                        bn.Write(Binary.FastWrite(ci.Value));
                    }
                }
            }
        }

        /// <summary>从数据流加载</summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public void Load(Stream stream)
        {
            var bn = new Binary
            {
                Stream = stream,
                EncodeInt = true,
            };

            // 头部，幻数、版本和标记
            var magic = bn.ReadBytes(MAGIC.Length).ToStr();
            if (magic != MAGIC) throw new InvalidDataException();

            var ver = bn.Read<Byte>();
            _ = bn.Read<Byte>();

            // 版本兼容
            if (ver > _Ver) throw new InvalidDataException($"MemoryCache[ver={_Ver}]无法支持较新的版本[{ver}]");

            var count = bn.ReadSize();
            while (count-- > 0)
            {
                // Key+Expire+Empty
                // Key+Expire+TypeCode+Value
                // Key+Expire+TypeCode+Type+Length+Value
                var key = bn.Read<String>();
                var exp = bn.Read<Int32>();
                var code = (TypeCode)bn.ReadByte();

                Object value = null;
                if (code == TypeCode.Empty)
                {
                }
                else if (code != TypeCode.Object)
                {
                    value = bn.Read(Type.GetType("System." + code));
                }
                else
                {
                    var typeName = bn.Read<String>();
                    //var type = Type.GetType(typeName);
                    var type = typeName.GetTypeEx();

                    var pk = bn.Read<Packet>();
                    value = pk;
                    if (type != null)
                    {
                        var bn2 = new Binary() { Stream = pk.GetStream(), EncodeInt = true };
                        value = bn2.Read(type);
                    }
                }

                Set(key, value, exp - (Int32)(Runtime.TickCount64 / 1000));
            }
        }

        /// <summary>保存到文件</summary>
        /// <param name="file"></param>
        /// <param name="compressed"></param>
        /// <returns></returns>
        public Int64 Save(String file, Boolean compressed) => file.AsFile().OpenWrite(compressed, s => Save(s));

        /// <summary>从文件加载</summary>
        /// <param name="file"></param>
        /// <param name="compressed"></param>
        /// <returns></returns>
        public Int64 Load(String file, Boolean compressed) => file.AsFile().OpenRead(compressed, s => Load(s));
        #endregion

        #region 性能测试
        /// <summary>使用指定线程测试指定次数</summary>
        /// <param name="times">次数</param>
        /// <param name="threads">线程</param>
        /// <param name="rand">随机读写</param>
        /// <param name="batch">批量操作</param>
        public override Int64 BenchOne(Int64 times, Int32 threads, Boolean rand, Int32 batch)
        {
            if (rand)
                times *= 100;
            else
                times *= 1000;

            return base.BenchOne(times, threads, rand, batch);
        }
        #endregion
    }


    /// <summary>生产者消费者</summary>
    /// <typeparam name="T"></typeparam>
    public class MemoryQueue<T> : DisposeBase, IProducerConsumer<T>
    {
        private readonly IProducerConsumerCollection<T> _collection;
        private readonly SemaphoreSlim _occupiedNodes;

        /// <summary>实例化内存队列</summary>
        public MemoryQueue()
        {
            _collection = new ConcurrentQueue<T>();
            _occupiedNodes = new SemaphoreSlim(0);
        }

        /// <summary>实例化内存队列</summary>
        /// <param name="collection"></param>
        public MemoryQueue(IProducerConsumerCollection<T> collection)
        {
            _collection = collection;
            _occupiedNodes = new SemaphoreSlim(collection.Count);
        }

        /// <summary>元素个数</summary>
        public Int32 Count => _collection.Count;

        /// <summary>集合是否为空</summary>
        public Boolean IsEmpty
        {
            get
            {
                if (_collection is ConcurrentQueue<T> queue) return queue.IsEmpty;
                if (_collection is ConcurrentStack<T> stack) return stack.IsEmpty;

                throw new NotSupportedException();
            }
        }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void Dispose(Boolean disposing)
        {
            base.Dispose(disposing);

            _occupiedNodes.TryDispose();
        }

        /// <summary>生产添加</summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public Int32 Add(params T[] values)
        {
            var count = 0;
            foreach (var item in values)
            {
                if (_collection.TryAdd(item))
                {
                    count++;
                    _occupiedNodes.Release();
                }
            }

            return count;
        }

        /// <summary>消费获取</summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public IEnumerable<T> Take(Int32 count = 1)
        {
            if (count <= 0) yield break;

            for (var i = 0; i < count; i++)
            {
                if (!_occupiedNodes.Wait(0)) break;
                if (!_collection.TryTake(out var item)) break;

                yield return item;
            }
        }

        /// <summary>消费一个</summary>
        /// <param name="timeout">超时。默认0秒，永久等待</param>
        /// <returns></returns>
        public T TakeOne(Int32 timeout = 0)
        {
            if (!_occupiedNodes.Wait(0))
            {
                if (timeout <= 0 || !_occupiedNodes.Wait(timeout * 1000)) return default;
            }

            return _collection.TryTake(out var item) ? item : default;
        }

        /// <summary>消费获取，异步阻塞</summary>
        /// <param name="timeout">超时。单位秒，0秒表示永久等待</param>
        /// <returns></returns>
        public async Task<T> TakeOneAsync(Int32 timeout = 0)
        {
            if (!_occupiedNodes.Wait(0))
            {
                if (timeout <= 0) return default;

                if (!await _occupiedNodes.WaitAsync(timeout * 1000)) return default;
            }

            return _collection.TryTake(out var item) ? item : default;
        }

        /// <summary>消费获取，异步阻塞</summary>
        /// <param name="timeout">超时。单位秒，0秒表示永久等待</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns></returns>
        public async Task<T> TakeOneAsync(Int32 timeout, CancellationToken cancellationToken)
        {
            if (!_occupiedNodes.Wait(0, cancellationToken))
            {
                if (timeout <= 0) return default;

                if (!await _occupiedNodes.WaitAsync(timeout * 1000, cancellationToken)) return default;
            }

            return _collection.TryTake(out var item) ? item : default;
        }

        /// <summary>确认消费</summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        public Int32 Acknowledge(params String[] keys) => 0;
    }

    #endregion
}
