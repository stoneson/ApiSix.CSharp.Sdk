using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace ApiSix.CSharp
{

    /// <summary>对象池接口</summary>
    /// <remarks>
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    public interface IPool<T>
    {
        /// <summary>对象池大小</summary>
        Int32 Max { get; set; }

        /// <summary>获取</summary>
        /// <returns></returns>
        T Get();

        /// <summary>归还</summary>
        /// <param name="value"></param>
        Boolean Put(T value);

        /// <summary>清空</summary>
        Int32 Clear();
    }

    /// <summary>轻量级对象池。数组无锁实现，高性能</summary>
    /// <remarks>
    /// 内部 1+N 的存储结果，保留最热的一个对象在外层，便于快速存取。
    /// 数组具有极快的查找速度，结构体确保没有GC操作。
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    public class Pool<T> : IPool<T> where T : class
    {
        #region 属性
        /// <summary>对象池大小。默认CPU*2，初始化后改变无效</summary>
        public Int32 Max { get; set; }

        private Item[] _items;
        private T _current;

        struct Item
        {
            public T Value;
        }
        #endregion

        #region 构造
        /// <summary>实例化对象池。默认大小CPU*2</summary>
        /// <param name="max"></param>
        public Pool(Int32 max = 0)
        {
            if (max <= 0) max = Environment.ProcessorCount * 2;

            Max = max;
        }

        private void Init()
        {
            if (_items != null) return;
            lock (this)
            {
                if (_items != null) return;

                _items = new Item[Max - 1];
            }
        }
        #endregion

        #region 方法
        /// <summary>获取</summary>
        /// <returns></returns>
        public virtual T Get()
        {
            // 最热的一个对象在外层，便于快速存取
            var val = _current;
            if (val != null && Interlocked.CompareExchange(ref _current, null, val) == val) return val;

            Init();

            var items = _items;
            for (var i = 0; i < items.Length; i++)
            {
                val = items[i].Value;
                if (val != null && Interlocked.CompareExchange(ref items[i].Value, null, val) == val) return val;
            }

            return OnCreate();
        }

        /// <summary>归还</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual Boolean Put(T value)
        {
            // 最热的一个对象在外层，便于快速存取
            if (_current == null && Interlocked.CompareExchange(ref _current, value, null) == null) return true;

            Init();

            var items = _items;
            for (var i = 0; i < items.Length; ++i)
            {
                if (Interlocked.CompareExchange(ref items[i].Value, value, null) == null) return true;
            }

            return false;
        }

        /// <summary>清空</summary>
        /// <returns></returns>
        public virtual Int32 Clear()
        {
            var count = 0;

            if (_current != null)
            {
                _current = null;
                count++;
            }

            var items = _items;
            for (var i = 0; i < items.Length; ++i)
            {
                if (items[i].Value != null)
                {
                    items[i].Value = null;
                    count++;
                }
            }
            _items = null;

            return count;
        }
        #endregion

        #region 重载
        /// <summary>创建实例</summary>
        /// <returns></returns>
        protected virtual T OnCreate() => typeof(T).CreateInstance() as T;
        #endregion
    }

    /// <summary>对象池扩展</summary>
    /// <remarks>
    /// </remarks>
    public static class Pool
    {
        #region 扩展
        #endregion

        #region StringBuilder
        /// <summary>字符串构建器池</summary>
        public static IPool<StringBuilder> StringBuilder { get; set; } = new StringBuilderPool();

        /// <summary>归还一个字符串构建器到对象池</summary>
        /// <param name="sb"></param>
        /// <param name="requireResult">是否需要返回结果</param>
        /// <returns></returns>
        public static String Put(this StringBuilder sb, Boolean requireResult = false)
        {
            if (sb == null) return null;

            var str = requireResult ? sb.ToString() : null;

            Pool.StringBuilder.Put(sb);

            return str;
        }

        /// <summary>字符串构建器池</summary>
        public class StringBuilderPool : Pool<StringBuilder>
        {
            /// <summary>初始容量。默认100个</summary>
            public Int32 InitialCapacity { get; set; } = 100;

            /// <summary>最大容量。超过该大小时不进入池内，默认4k</summary>
            public Int32 MaximumCapacity { get; set; } = 4 * 1024;

            /// <summary>创建</summary>
            /// <returns></returns>
            protected override StringBuilder OnCreate() => new System.Text.StringBuilder(InitialCapacity);

            /// <summary>归还</summary>
            /// <param name="value"></param>
            /// <returns></returns>
            public override Boolean Put(StringBuilder value)
            {
                if (value.Capacity > MaximumCapacity) return false;

                value.Clear();

                return base.Put(value);
            }
        }
        #endregion

        #region MemoryStream
        /// <summary>内存流池</summary>
        public static IPool<MemoryStream> MemoryStream { get; set; } = new MemoryStreamPool();

        /// <summary>归还一个内存流到对象池</summary>
        /// <param name="ms"></param>
        /// <param name="requireResult">是否需要返回结果</param>
        /// <returns></returns>
        public static Byte[] Put(this MemoryStream ms, Boolean requireResult = false)
        {
            if (ms == null) return null;

            var buf = requireResult ? ms.ToArray() : null;

            Pool.MemoryStream.Put(ms);

            return buf;
        }

        /// <summary>内存流池</summary>
        public class MemoryStreamPool : Pool<MemoryStream>
        {
            /// <summary>初始容量。默认1024个</summary>
            public Int32 InitialCapacity { get; set; } = 1024;

            /// <summary>最大容量。超过该大小时不进入池内，默认64k</summary>
            public Int32 MaximumCapacity { get; set; } = 64 * 1024;

            /// <summary>创建</summary>
            /// <returns></returns>
            protected override MemoryStream OnCreate() => new System.IO.MemoryStream(InitialCapacity);

            /// <summary>归还</summary>
            /// <param name="value"></param>
            /// <returns></returns>
            public override Boolean Put(MemoryStream value)
            {
                if (value.Capacity > MaximumCapacity) return false;

                value.Position = 0;
                value.SetLength(0);

                return base.Put(value);
            }
        }
        #endregion
    }

    /// <summary>资源池。支持空闲释放，主要用于数据库连接池和网络连接池</summary>
    /// <typeparam name="T"></typeparam>
    public class ObjectPool<T> : DisposeBase, IPool<T>
    {
        #region 属性
        /// <summary>名称</summary>
        public String Name { get; set; }

        private Int32 _FreeCount;
        /// <summary>空闲个数</summary>
        public Int32 FreeCount => _FreeCount;

        private Int32 _BusyCount;
        /// <summary>繁忙个数</summary>
        public Int32 BusyCount => _BusyCount;

        /// <summary>最大个数。默认100，0表示无上限</summary>
        public Int32 Max { get; set; } = 100;

        /// <summary>最小个数。默认1</summary>
        public Int32 Min { get; set; } = 1;

        /// <summary>空闲清理时间。最小个数之上的资源超过空闲时间时被清理，默认10s</summary>
        public Int32 IdleTime { get; set; } = 10;

        /// <summary>完全空闲清理时间。最小个数之下的资源超过空闲时间时被清理，默认0s永不清理</summary>
        public Int32 AllIdleTime { get; set; } = 0;

        /// <summary>基础空闲集合。只保存最小个数，最热部分</summary>
        private readonly ConcurrentStack<Item> _free = new ConcurrentStack<Item>();

        /// <summary>扩展空闲集合。保存最小个数以外部分</summary>
        private readonly ConcurrentQueue<Item> _free2 = new ConcurrentQueue<Item>();

        /// <summary>借出去的放在这</summary>
        private readonly ConcurrentDictionary<T, Item> _busy = new ConcurrentDictionary<T, Item>();

        private readonly Object SyncRoot = new object();
        #endregion

        #region 构造
        /// <summary>实例化一个资源池</summary>
        public ObjectPool()
        {
            var str = GetType().Name;
            if (str.Contains('`')) str = str.Substring(null, "`");
            if (str != "Pool")
                Name = str;
            else
                Name = $"Pool<{typeof(T).Name}>";
        }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void Dispose(Boolean disposing)
        {
            base.Dispose(disposing);

            _timer.TryDispose();

            WriteLog($"Dispose {typeof(T).FullName} FreeCount={FreeCount:n0} BusyCount={BusyCount:n0} Total={Total:n0}");

            Clear();
        }

        private volatile Boolean _inited;
        private void Init()
        {
            if (_inited) return;

            lock (SyncRoot)
            {
                if (_inited) return;
                _inited = true;

                WriteLog($"Init {typeof(T).FullName} Min={Min} Max={Max} IdleTime={IdleTime}s AllIdleTime={AllIdleTime}s");
            }
        }
        #endregion

        #region 内嵌
        class Item
        {
            /// <summary>数值</summary>
            public T Value { get; set; }

            /// <summary>过期时间</summary>
            public DateTime LastTime { get; set; }
        }
        #endregion

        #region 主方法
        /// <summary>借出</summary>
        /// <returns></returns>
        public virtual T Get()
        {
            var sw = Log == null || Log == Logger.Null ? null : Stopwatch.StartNew();
            Interlocked.Increment(ref _Total);

            var success = false;
            Item pi = null;
            do
            {
                // 从空闲集合借一个
                if (_free.TryPop(out pi) || _free2.TryDequeue(out pi))
                {
                    Interlocked.Decrement(ref _FreeCount);

                    success = true;
                }
                else
                {
                    // 超出最大值后，抛出异常
                    var count = BusyCount;
                    if (Max > 0 && count >= Max)
                    {
                        var msg = $"申请失败，已有 {count:n0} 达到或超过最大值 {Max:n0}";

                        WriteLog("Acquire Max " + msg);

                        throw new Exception(Name + " " + msg);
                    }

                    // 借不到，增加
                    pi = new Item
                    {
                        Value = OnCreate(),
                    };

                    if (count == 0) Init();
#if DEBUG
                    WriteLog("Acquire Create Free={0} Busy={1}", FreeCount, count + 1);
#endif

                    Interlocked.Increment(ref _NewCount);
                    success = false;
                }

                // 借出时如果不可用，再次借取
            } while (!OnGet(pi.Value));

            // 最后时间
            pi.LastTime = TimerX.Now;

            // 加入繁忙集合
            _busy.TryAdd(pi.Value, pi);

            Interlocked.Increment(ref _BusyCount);
            if (success) Interlocked.Increment(ref _Success);
            if (sw != null)
            {
                sw.Stop();
                var ms = sw.Elapsed.TotalMilliseconds;

                if (Cost < 0.001)
                    Cost = ms;
                else
                    Cost = (Cost * 3 + ms) / 4;
            }

            return pi.Value;
        }

        /// <summary>借出时是否可用</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected virtual Boolean OnGet(T value) => true;

        /// <summary>申请资源包装项，Dispose时自动归还到池中</summary>
        /// <returns></returns>
        public PoolItem<T> GetItem() => new PoolItem<T>(this, Get());

        /// <summary>归还</summary>
        /// <param name="value"></param>
        public virtual Boolean Put(T value)
        {
            if (value == null) return false;

            // 从繁忙队列找到并移除缓存项
            if (!_busy.TryRemove(value, out var pi))
            {
#if DEBUG
                WriteLog("Put Error");
#endif
                Interlocked.Increment(ref _ReleaseCount);

                return false;
            }

            Interlocked.Decrement(ref _BusyCount);

            // 是否可用
            if (!OnPut(value))
            {
                Interlocked.Increment(ref _ReleaseCount);
                return false;
            }

            if (value is DisposeBase db && db.Disposed)
            {
                Interlocked.Increment(ref _ReleaseCount);
                return false;
            }

            var min = Min;

            // 如果空闲数不足最小值，则返回到基础空闲集合
            if (_FreeCount < min /*|| _free.Count < min*/)
                _free.Push(pi);
            else
                _free2.Enqueue(pi);

            // 最后时间
            pi.LastTime = TimerX.Now;

            Interlocked.Increment(ref _FreeCount);

            // 启动定期清理的定时器
            StartTimer();

            return true;
        }

        /// <summary>归还时是否可用</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected virtual Boolean OnPut(T value) => true;

        /// <summary>清空已有对象</summary>
        public virtual Int32 Clear()
        {
            var count = _FreeCount + _BusyCount;

            //_busy.Clear();
            //_BusyCount = 0;

            //_free.Clear();
            //while (_free2.TryDequeue(out var rs)) ;
            //_FreeCount = 0;

            while (_free.TryPop(out var pi)) OnDispose(pi.Value);
            while (_free2.TryDequeue(out var pi)) OnDispose(pi.Value);
            _FreeCount = 0;

            foreach (var item in _busy)
            {
                OnDispose(item.Key);
            }
            _busy.Clear();
            _BusyCount = 0;

            return count;
        }

        /// <summary>销毁</summary>
        /// <param name="value"></param>
        protected virtual void OnDispose(T value) => value.TryDispose();
        #endregion

        #region 重载
        /// <summary>创建实例</summary>
        /// <returns></returns>
        protected virtual T OnCreate() => (T)typeof(T).CreateInstance();
        #endregion

        #region 定期清理
        private TimerX _timer;

        private void StartTimer()
        {
            if (_timer != null) return;
            lock (this)
            {
                if (_timer != null) return;

                _timer = new TimerX(Work, null, 5000, 5000) { Async = true };
            }
        }

        private void Work(Object state)
        {
            //// 总数小于等于最小个数时不处理
            //if (FreeCount + BusyCount <= Min) return;

            // 遍历并干掉过期项
            var count = 0;

            // 清理过期不还。避免有借没还
            if (!_busy.IsEmpty)
            {
                var exp = TimerX.Now.AddSeconds(-AllIdleTime);
                foreach (var item in _busy)
                {
                    if (item.Value.LastTime < exp)
                    {
                        if (_busy.TryRemove(item.Key, out _))
                        {
                            // 业务层可能故意有借没还
                            //v.TryDispose();

                            Interlocked.Decrement(ref _BusyCount);
                        }
                    }
                }
            }

            // 总数小于等于最小个数时不处理
            if (IdleTime > 0 && !_free2.IsEmpty && FreeCount + BusyCount > Min)
            {
                var exp = TimerX.Now.AddSeconds(-IdleTime);
                // 移除扩展空闲集合里面的超时项
                while (_free2.TryPeek(out var pi) && pi.LastTime < exp)
                {
                    // 取出来销毁
                    if (_free2.TryDequeue(out pi))
                    {
                        pi.Value.TryDispose();

                        count++;
                        Interlocked.Decrement(ref _FreeCount);
                    }
                }
            }

            if (AllIdleTime > 0 && !_free.IsEmpty)
            {
                var exp = TimerX.Now.AddSeconds(-AllIdleTime);
                // 移除基础空闲集合里面的超时项
                while (_free.TryPeek(out var pi) && pi.LastTime < exp)
                {
                    // 取出来销毁
                    if (_free.TryPop(out pi))
                    {
                        pi.Value.TryDispose();

                        count++;
                        Interlocked.Decrement(ref _FreeCount);
                    }
                }
            }

            var ncount = _NewCount;
            var fcount = _ReleaseCount;
            if (count > 0 || ncount > 0 || fcount > 0)
            {
                Interlocked.Add(ref _NewCount, -ncount);
                Interlocked.Add(ref _ReleaseCount, -fcount);

                var p = Total == 0 ? 0 : (Double)Success / Total;

                WriteLog("Release New={6:n0} Release={7:n0} Free={0} Busy={1} 清除过期资源 {2:n0} 项。总请求 {3:n0} 次，命中 {4:p2}，平均 {5:n2}us", FreeCount, BusyCount, count, Total, p, Cost * 1000, ncount, fcount);
            }
        }
        #endregion

        #region 统计
        private Int32 _Total;
        /// <summary>总请求数</summary>
        public Int32 Total => _Total;

        private Int32 _Success;
        /// <summary>成功数</summary>
        public Int32 Success => _Success;

        /// <summary>新创建数</summary>
        private Int32 _NewCount;

        /// <summary>释放数</summary>
        private Int32 _ReleaseCount;

        /// <summary>平均耗时。单位ms</summary>
        private Double Cost;
        #endregion

        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; } = Logger.Null;

        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLog(String format, params Object[] args)
        {
            if (Log == null || !Log.Enable) return;

            Log.Info(Name + "." + format, args);
        }
        #endregion
    }

    /// <summary>资源池包装项，自动归还资源到池中</summary>
    /// <typeparam name="T"></typeparam>
    public class PoolItem<T> : DisposeBase
    {
        #region 属性
        /// <summary>数值</summary>
        public T Value { get; }

        /// <summary>池</summary>
        public IPool<T> Pool { get; }
        #endregion

        #region 构造
        /// <summary>包装项</summary>
        /// <param name="pool"></param>
        /// <param name="value"></param>
        public PoolItem(IPool<T> pool, T value)
        {
            Pool = pool;
            Value = value;
        }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void Dispose(Boolean disposing)
        {
            base.Dispose(disposing);

            Pool.Put(Value);
        }
        #endregion
    }
}
