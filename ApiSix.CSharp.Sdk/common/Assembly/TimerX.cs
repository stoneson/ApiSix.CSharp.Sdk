using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Collections;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace ApiSix.CSharp
{   
    /// <summary>线程池助手</summary>
    public class ThreadPoolX : DisposeBase
    {
        #region 全局线程池助手
        static ThreadPoolX()
        {
            // 在这个同步异步大量混合使用的时代，需要更多的初始线程来屏蔽各种对TPL的不合理使用
            ThreadPool.GetMinThreads(out var wt, out var io);
            if (wt < 32 || io < 32)
            {
                if (wt < 32) wt = 32;
                if (io < 32) io = 32;
                ThreadPool.SetMinThreads(wt, io);
            }
        }

        /// <summary>初始化线程池
        /// </summary>
        public static void Init() { }

        /// <summary>带异常处理的线程池任务调度，不允许异常抛出，以免造成应用程序退出，同时不会捕获上下文</summary>
        /// <param name="callback"></param>
        [DebuggerHidden]
        public static void QueueUserWorkItem(Action callback)
        {
            if (callback == null) return;

            ThreadPool.UnsafeQueueUserWorkItem(s =>
            {
                try
                {
                    callback();
                }
                catch (Exception ex)
                {
                    XTrace.WriteException(ex);
                }
            }, null);

            //Instance.QueueWorkItem(callback);
        }

        /// <summary>带异常处理的线程池任务调度，不允许异常抛出，以免造成应用程序退出，同时不会捕获上下文</summary>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        [DebuggerHidden]
        public static void QueueUserWorkItem<T>(Action<T> callback, T state)
        {
            if (callback == null) return;

            ThreadPool.UnsafeQueueUserWorkItem(s =>
            {
                try
                {
                    callback(state);
                }
                catch (Exception ex)
                {
                    XTrace.WriteException(ex);
                }
            }, null);

            //Instance.QueueWorkItem(() => callback(state));
        }
        #endregion
    }

    /// <summary>定时器调度器</summary>
    public class TimerScheduler
    {
        #region 静态
        private TimerScheduler(String name) => Name = name;

        private static readonly Dictionary<String, TimerScheduler> _cache = new Dictionary<string, TimerScheduler>();

        /// <summary>创建指定名称的调度器</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static TimerScheduler Create(String name)
        {
            if (_cache.TryGetValue(name, out var ts)) return ts;
            lock (_cache)
            {
                if (_cache.TryGetValue(name, out ts)) return ts;

                ts = new TimerScheduler(name);
                _cache[name] = ts;

                return ts;
            }
        }

        /// <summary>默认调度器</summary>
        public static TimerScheduler Default { get; } = Create("Default");

        [ThreadStatic]
        private static TimerScheduler _Current =null;
        /// <summary>当前调度器</summary>
        public static TimerScheduler Current { get => _Current; private set => _Current = value; }
        #endregion

        #region 属性
        /// <summary>名称</summary>
        public String Name { get; private set; }

        /// <summary>定时器个数</summary>
        public Int32 Count { get; private set; }

        /// <summary>最大耗时。超过时报警告日志，默认500ms</summary>
        public Int32 MaxCost { get; set; } = 500;

        private Thread thread = null;
        private Int32 _tid;

        private TimerX[] Timers = new TimerX[0];
        #endregion

        /// <summary>把定时器加入队列</summary>
        /// <param name="timer"></param>
        public void Add(TimerX timer)
        {
            if (timer == null) throw new ArgumentNullException(nameof(timer));

            //using var span = DefaultTracer.Instance?.NewSpan("timer:Add", timer.ToString());

            timer.Id = Interlocked.Increment(ref _tid);
            WriteLog("Timer.Add {0}", timer);

            lock (this)
            {
                var list = new List<TimerX>(Timers);
                if (list.Contains(timer)) return;
                list.Add(timer);

                Timers = list.ToArray();

                Count++;

                if (thread == null)
                {
                    thread = new Thread(Process)
                    {
                        Name = Name == "Default" ? "T" : Name,
                        IsBackground = true
                    };
                    thread.Start();

                    WriteLog("启动定时调度器：{0}", Name);
                }

                Wake();
            }
        }

        /// <summary>从队列删除定时器</summary>
        /// <param name="timer"></param>
        /// <param name="reason"></param>
        public void Remove(TimerX timer, String reason)
        {
            if (timer == null || timer.Id == 0) return;

            //using var span = DefaultTracer.Instance?.NewSpan("timer:Remove", reason + " " + timer);
            WriteLog("Timer.Remove {0} reason:{1}", timer, reason);

            lock (this)
            {
                timer.Id = 0;

                var list = new List<TimerX>(Timers);
                if (list.Contains(timer))
                {
                    list.Remove(timer);
                    Timers = list.ToArray();

                    Count--;
                }
            }
        }

        private AutoResetEvent _waitForTimer = null;
        private Int32 _period = 10;

        /// <summary>唤醒处理</summary>
        public void Wake()
        {
            var e = _waitForTimer;
            if (e != null)
            {
                var swh = e.SafeWaitHandle;
                if (swh != null && !swh.IsClosed) e.Set();
            }
        }

        /// <summary>调度主程序</summary>
        /// <param name="state"></param>
        private void Process(Object state)
        {
            Current = this;
            while (true)
            {
                // 准备好定时器列表
                var arr = Timers;

                // 如果没有任务，则销毁线程
                if (arr.Length == 0 && _period == 60_000)
                {
                    WriteLog("没有可用任务，销毁线程");

                    var th = thread;
                    thread = null;
                    //th?.Abort();

                    break;
                }

                try
                {
                    var now = Runtime.TickCount64;

                    // 设置一个较大的间隔，内部会根据处理情况调整该值为最合理值
                    _period = 60_000;
                    foreach (var timer in arr)
                    {
                        if (!timer.Calling && CheckTime(timer, now))
                        {
                            // 是否能够执行
                            if (timer.CanExecute == null || timer.CanExecute())
                            {
                                // 必须在主线程设置状态，否则可能异步线程还没来得及设置开始状态，主线程又开始了新的一轮调度
                                timer.Calling = true;
                                if (timer.IsAsyncTask)
                                    Task.Factory.StartNew(ExecuteAsync, timer);
                                else if (!timer.Async)
                                    Execute(timer);
                                else
                                    //Task.Factory.StartNew(() => ProcessItem(timer));
                                    // 不需要上下文流动
                                    ThreadPool.UnsafeQueueUserWorkItem(s =>
                                    {
                                        try
                                        {
                                            Execute(s);
                                        }
                                        catch (Exception ex)
                                        {
                                            XTrace.WriteException(ex);
                                        }
                                    }, timer);
                                // 内部线程池，让异步任务有公平竞争CPU的机会
                                //ThreadPoolX.QueueUserWorkItem(Execute, timer);
                            }
                            // 即使不能执行，也要设置下一次的时间
                            else
                            {
                                OnFinish(timer);
                            }
                        }
                    }
                }
                catch (ThreadAbortException) { break; }
                catch (ThreadInterruptedException) { break; }
                catch { }

                _waitForTimer ??= new AutoResetEvent(false);
                if (_period > 0) _waitForTimer.WaitOne(_period, true);
            }
        }

        /// <summary>检查定时器是否到期</summary>
        /// <param name="timer"></param>
        /// <param name="now"></param>
        /// <returns></returns>
        private Boolean CheckTime(TimerX timer, Int64 now)
        {
            // 删除过期的，为了避免占用过多CPU资源，TimerX禁止小于10ms的任务调度
            var p = timer.Period;
            if (p < 10 && p > 0) //if (p is < 10 and > 0)
            {
                // 周期0表示只执行一次
                if (p < 10 && p > 0) 
                    XTrace.WriteLine("为了避免占用过多CPU资源，TimerX禁止小于{1}ms<10ms的任务调度，关闭任务{0}", timer, p);
                timer.Dispose();
                return false;
            }

            var ts = timer.NextTick - now;
            if (ts > 0)
            {
                // 缩小间隔，便于快速调用
                if (ts < _period) _period = (Int32)ts;

                return false;
            }

            return true;
        }

        /// <summary>处理每一个定时器</summary>
        /// <param name="state"></param>
        private void Execute(Object state)
        {
            if (!(state is  TimerX timer)) return;

            TimerX.Current = timer;

            // 控制日志显示
            WriteLogEventArgs.CurrentThreadName = Name == "Default" ? "T" : Name;

            timer.hasSetNext = false;

            //DefaultSpan.Current = null;
            //using var span = timer.Tracer?.NewSpan(timer.TracerName ?? $"timer:Execute", timer.Timers + "");
            var sw = Stopwatch.StartNew();
            try
            {
                // 弱引用判断
                var target = timer.Target.Target;
                if (target == null && !timer.Method.IsStatic)
                {
                    Remove(timer, "委托已不存在（GC回收委托所在对象）");
                    timer.Dispose();
                    return;
                }

                var func = timer.Method.As<TimerCallback>(target);
                func(timer.State);
            }
            catch (ThreadAbortException) { throw; }
            catch (ThreadInterruptedException) { throw; }
            // 如果用户代码没有拦截错误，则这里拦截，避免出错了都不知道怎么回事
            catch (Exception ex)
            {
                //span?.SetError(ex, null);
                XTrace.WriteException(ex);
            }
            finally
            {
                sw.Stop();

                OnExecuted(timer, (Int32)sw.ElapsedMilliseconds);
            }
        }

        /// <summary>处理每一个定时器</summary>
        /// <param name="state"></param>
        private async void ExecuteAsync(Object state)
        {
            if (!(state is  TimerX timer)) return;

            TimerX.Current = timer;

            // 控制日志显示
            WriteLogEventArgs.CurrentThreadName = Name == "Default" ? "T" : Name;

            timer.hasSetNext = false;

            //DefaultSpan.Current = null;
            //using var span = timer.Tracer?.NewSpan(timer.TracerName ?? $"timer:ExecuteAsync", timer.Timers + "");
            var sw = Stopwatch.StartNew();
            try
            {
                // 弱引用判断
                var target = timer.Target.Target;
                if (target == null && !timer.Method.IsStatic)
                {
                    Remove(timer, "委托已不存在（GC回收委托所在对象）");
                    timer.Dispose();
                    return;
                }

                var func = timer.Method.As<Func<Object, Task>>(target);
                await func(timer.State);
            }
            catch (ThreadAbortException) { throw; }
            catch (ThreadInterruptedException) { throw; }
            // 如果用户代码没有拦截错误，则这里拦截，避免出错了都不知道怎么回事
            catch (Exception ex)
            {
                //span?.SetError(ex, null);
                XTrace.WriteException(ex);
            }
            finally
            {
                sw.Stop();

                OnExecuted(timer, (Int32)sw.ElapsedMilliseconds);
            }
        }

        private void OnExecuted(TimerX timer, Int32 ms)
        {
            timer.Cost = timer.Cost == 0 ? ms : (timer.Cost + ms) / 2;

            if (ms > MaxCost && !timer.Async && !timer.IsAsyncTask) XTrace.WriteLine("任务 {0} 耗时过长 {1:n0}ms，建议使用异步任务Async=true", timer, ms);

            timer.Timers++;
            OnFinish(timer);

            timer.Calling = false;

            TimerX.Current = null;

            // 控制日志显示
            WriteLogEventArgs.CurrentThreadName = null;

            // 调度线程可能在等待，需要唤醒
            Wake();
        }

        private void OnFinish(TimerX timer)
        {
            // 如果内部设置了下一次时间，则不再递加周期
            var p = timer.SetAndGetNextTime();

            // 清理一次性定时器
            if (p <= 0)
            {
                Remove(timer, "Period<=0");
                timer.Dispose();
            }
            else if (p < _period)
                _period = p;
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString() => Name;

        #region 设置
        /// <summary>日志</summary>
        public ILog Log { get; set; } = Logger.Null;

        private void WriteLog(String format, params Object[] args) => Log?.Info(Name + format, args);
        #endregion
    }

    /// <summary>轻量级Cron表达式</summary>
    /// <remarks>
    /// 基本构成：秒+分+时+天+月+星期
    /// 每段构成：
    ///     * 所有可能的值，该类型片段全部可选
    ///     , 列出枚举值
    ///     - 范围，横杠表示的一个区间可选
    ///     / 指定数值的增量，在上述可选数字内，间隔多少选一个
    ///     ? 不指定值，仅日期和星期域支持该字符
    ///     # 确定每个月第几个星期几，仅星期域支持该字符
    ///     数字，具体某个数值可选
    ///     逗号多选，逗号分隔的多个数字或区间可选
    /// </remarks>
    /// <example>
    /// */2 每两秒一次
    /// 0,1,2 * * * * 每分钟的0秒1秒2秒各一次
    /// 5/20 * * * * 每分钟的5秒25秒45秒各一次
    /// * 1-10,13,25/3 * * * 每小时的1分4分7分10分13分25分，每一秒各一次
    /// 0 0 0 1 * * 每个月1日的0点整
    /// 0 0 2 * * 1-5 每个工作日的凌晨2点
    /// 
    /// 星期部分采用Linux和.NET风格，0表示周日，1表示周一。
    /// 可设置Sunday为1，1表示周日，2表示周一。
    /// 
    /// 参考文档 https://help.aliyun.com/document_detail/64769.html
    /// </example>
    public class Cron
    {
        #region 属性
        /// <summary>秒数集合</summary>
        public Int32[] Seconds;

        /// <summary>分钟集合</summary>
        public Int32[] Minutes;

        /// <summary>小时集合</summary>
        public Int32[] Hours;

        /// <summary>日期集合</summary>
        public Int32[] DaysOfMonth;

        /// <summary>月份集合</summary>
        public Int32[] Months;

        /// <summary>星期集合。key是星期数，value是第几个，负数表示倒数</summary>
        public IDictionary<Int32, Int32> DaysOfWeek;

        /// <summary>星期天偏移量。周日对应的数字，默认0。1表示周日时，2表示周一</summary>
        public Int32 Sunday { get; set; }

        private String _expression;
        #endregion

        #region 构造
        /// <summary>实例化Cron表达式</summary>
        public Cron() { }

        /// <summary>实例化Cron表达式</summary>
        /// <param name="expression"></param>
        public Cron(String expression) => Parse(expression);

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString() => _expression;
        #endregion

        #region 方法
        /// <summary>指定时间是否位于表达式之内</summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public Boolean IsTime(DateTime time)
        {
            // 基础时间判断
            if (!Seconds.Contains(time.Second) ||
                !Minutes.Contains(time.Minute) ||
                !Hours.Contains(time.Hour) ||
                !DaysOfMonth.Contains(time.Day) ||
                !Months.Contains(time.Month)
                ) return false;

            var w = (Int32)time.DayOfWeek + Sunday;
            if (!DaysOfWeek.TryGetValue(w, out var index)) return false;

            // 第几个星期几判断
            if (index > 0)
            {
                var start = new DateTime(time.Year, time.Month, 1);
                for (var dt = start; dt <= time.Date; dt = dt.AddDays(1))
                {
                    if (dt.DayOfWeek == time.DayOfWeek) index--;
                }
                if (index != 0) return false;
            }
            else if (index < 0)
            {
                var start = new DateTime(time.Year, time.Month, 1);
                for (var dt = start.AddMonths(1).AddDays(-1); dt >= time.Date; dt = dt.AddDays(-1))
                {
                    if (dt.DayOfWeek == time.DayOfWeek) index++;
                }
                if (index != 0) return false;
            }

            return true;
        }

        /// <summary>分析表达式</summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public Boolean Parse(String expression)
        {
            var ss = expression.Split(' ');
            if (ss.Length == 0) return false;

            if (!TryParse(ss[0], 0, 60, out var vs)) return false;
            Seconds = vs;
            if (!TryParse(ss.Length > 1 ? ss[1] : "*", 0, 60, out vs)) return false;
            Minutes = vs;
            if (!TryParse(ss.Length > 2 ? ss[2] : "*", 0, 24, out vs)) return false;
            Hours = vs;
            if (!TryParse(ss.Length > 3 ? ss[3] : "*", 1, 32, out vs)) return false;
            DaysOfMonth = vs;
            if (!TryParse(ss.Length > 4 ? ss[4] : "*", 1, 13, out vs)) return false;
            Months = vs;

            var dic = new Dictionary<Int32, Int32>();
            if (!TryParseWeek(ss.Length > 5 ? ss[5] : "*", 0, 7, dic)) return false;
            DaysOfWeek = dic;

            _expression = expression;

            return true;
        }

        private static Boolean TryParse(String value, Int32 start, Int32 max, out Int32[] vs)
        {
            // 固定值，最为常见，优先计算
            if (Int32.TryParse(value, out var n))
            {
                vs = new Int32[] { n };
                return true;
            }

            var rs = new List<Int32>();
            vs = null;

            // 递归处理混合值
            if (value.Contains(','))
            {
                foreach (var item in value.Split(','))
                {
                    if (!TryParse(item, start, max, out var arr)) return false;
                    if (arr.Length > 0) rs.AddRange(arr);
                }
                vs = rs.ToArray();
                return true;
            }

            // 步进值
            var step = 1;
            var p = value.IndexOf('/');
            if (p > 0)
            {
                step = value.Substring(p + 1).ToInt(); //value[(p + 1)..].ToInt();
                value = value.Substring(0, p); //value[..p];
            }

            // 连续范围
            var s = start;
            if (value == "*" || value== "?")
                s = 0;
            else if ((p = value.IndexOf('-')) > 0)
            {
                s = value.Substring(0, p).ToInt(); //value[..p].ToInt();
                max = value.Substring(p + 1).ToInt() + 1; //value[(p + 1)..].ToInt() + 1;
            }
            else if (Int32.TryParse(value, out n))
                s = n;
            else
                return false;

            for (var i = s; i < max; i += step)
            {
                if (i >= start) rs.Add(i);
            }

            vs = rs.ToArray();
            return true;
        }

        private static Boolean TryParseWeek(String value, Int32 start, Int32 max, IDictionary<Int32, Int32> weeks)
        {
            // 固定值，最为常见，优先计算
            if (Int32.TryParse(value, out var n))
            {
                weeks[n] = 0;
                return true;
            }

            // 递归处理混合值
            if (value.Contains(','))
            {
                foreach (var item in value.Split(','))
                {
                    if (!TryParseWeek(item, start, max, weeks)) return false;
                }
                return true;
            }

            // 步进值
            var step = 1;
            var v = value;
            var p = value.IndexOf('/');
            if (p > 0)
            {
                step = value.Substring(p + 1).ToInt();
                v = value.Substring(0, p);
            }

            // 第几个星期几
            var index = 0;
            p = v.IndexOf('#');
            if (p > 0)
            {
                var str = v.Substring(p + 1);
                if (str.StartsWithIgnoreCase("L"))
                    index = -str.Substring(1).ToInt();
                else
                    index = str.ToInt();
                v = v.Substring(0, p);
                step = 7;
            }

            // 连续范围
            var s = start;
            if (v == "*" || v== "?")
                s = 0;
            else if ((p = v.IndexOf('-')) > 0)
            {
                s = v.Substring(0, p).ToInt();
                max = v.Substring(p + 1).ToInt() + 1;
                step = 1;
            }
            else if (Int32.TryParse(v, out n))
                s = n;
            else
                return false;

            for (var i = s; i < max; i += step)
            {
                if (i >= start) weeks.Add(i, index);
            }

            return true;
        }

        /// <summary>获得指定时间之后的下一次执行时间，不含指定时间</summary>
        /// <remarks>
        /// 如果指定时间带有毫秒，则向前对齐。如09:14.123的"15 * * *"下一次是10:15而不是09：15
        /// </remarks>
        /// <param name="time">从该时间秒的下一秒算起的下一个执行时间</param>
        /// <returns>下一次执行时间（秒级），如果没有匹配则返回最小时间</returns>
        public DateTime GetNext(DateTime time)
        {
            // 如果指定时间带有毫秒，则向前对齐。如09:14.123格式化为09:15，计算下一次就从09:16开始
            var start = time.Trim();
            if (start != time)
                start = start.AddSeconds(2);
            else
                start = start.AddSeconds(1);

            // 设置末尾，避免死循环越界
            var end = time.AddYears(1);
            for (var dt = start; dt < end; dt = dt.AddSeconds(1))
            {
                if (IsTime(dt)) return dt;
            }

            return DateTime.MinValue;
        }

        /// <summary>获得与指定时间时间符合表达式的最远时间（秒级）</summary>
        /// <param name="time"></param>
        public DateTime GetPrevious(DateTime time)
        {
            // 如果指定时间带有毫秒，则向前对齐。如09:14.123格式化为09:15，计算下一次就从09:16开始
            var start = time.Trim();
            if (start != time)
                start = start.AddSeconds(-1);
            else
                start = start.AddSeconds(-2);

            // 设置末尾，避免死循环越界
            var end = time.AddYears(-1);
            var last = false;
            for (var dt = start; dt > end; dt = dt.AddSeconds(-1))//过去一年内
            {
                if (last == false)
                {
                    last = IsTime(dt);//找真值
                }
                else
                {
                    if (IsTime(dt) == false)//真值找到了找假值
                    {
                        return dt.AddSeconds(1);//减多了，返回真值
                    }
                }
                //if (last == true && IsTime(dt) == false) return dt.AddSeconds(1);
                //last = IsTime(dt);
            }

            return DateTime.MinValue;
        }
        #endregion
    }

    /// <summary>不可重入的定时器，支持Cron</summary>
    /// <remarks>
    /// 
    /// 为了避免系统的Timer可重入的问题，差别在于本地调用完成后才开始计算时间间隔。这实际上也是经常用到的。
    /// 
    /// 因为挂载在静态列表上，必须从外部主动调用<see cref="IDisposable.Dispose"/>才能销毁定时器。
    /// 但是要注意GC回收定时器实例。
    /// 
    /// 该定时器不能放入太多任务，否则适得其反！
    /// 
    /// TimerX必须维持对象，否则Scheduler也没有维持对象时，大家很容易一起被GC回收。
    /// </remarks>
    public class TimerX : IDisposable
    {
        #region 属性
        /// <summary>编号</summary>
        public Int32 Id { get; internal set; }

        /// <summary>所属调度器</summary>
        public TimerScheduler Scheduler { get; private set; }

        /// <summary>目标对象。弱引用，使得调用方对象可以被GC回收</summary>
        internal readonly WeakReference Target;

        /// <summary>委托方法</summary>
        internal readonly MethodInfo Method;

        internal readonly Boolean IsAsyncTask;

        /// <summary>获取/设置 用户数据</summary>
        public Object State { get; set; }

        /// <summary>基准时间。开机时间</summary>
        private static DateTime _baseTime;

        private Int64 _nextTick;
        /// <summary>下一次执行时间。开机以来嘀嗒数，无惧时间回拨问题</summary>
        public Int64 NextTick => _nextTick;

        /// <summary>获取/设置 下一次调用时间</summary>
        public DateTime NextTime => _baseTime.AddMilliseconds(_nextTick);

        /// <summary>获取/设置 调用次数</summary>
        public Int32 Timers { get; internal set; }

        /// <summary>获取/设置 间隔周期。毫秒，设为0或-1则只调用一次</summary>
        public Int32 Period { get; set; }

        /// <summary>获取/设置 异步执行任务。默认false</summary>
        public Boolean Async { get; set; }

        /// <summary>获取/设置 绝对精确时间执行。默认false</summary>
        public Boolean Absolutely { get; set; }

        /// <summary>调用中</summary>
        public Boolean Calling { get; internal set; }

        /// <summary>平均耗时。毫秒</summary>
        public Int32 Cost { get; internal set; }

        /// <summary>判断任务是否执行的委托。一般跟异步配合使用，避免频繁从线程池借出线程</summary>
        public Func<Boolean> CanExecute { get; set; }

        /// <summary>Cron表达式，实现复杂的定时逻辑</summary>
        public Cron Cron => _cron;

        /// <summary>链路追踪。追踪每一次定时事件</summary>
        //public ITracer Tracer { get; set; }

        /// <summary>链路追踪名称。默认使用方法名</summary>
        public String TracerName { get; set; }

        private DateTime _AbsolutelyNext;
        private Cron _cron;
        #endregion

        #region 静态
#if NET45
        private static readonly ThreadLocal<TimerX> _Current = new ThreadLocal<TimerX>();
#else
    private static readonly AsyncLocal<TimerX> _Current = new AsyncLocal<TimerX>();
#endif
        /// <summary>当前定时器</summary>
        public static TimerX Current { get => _Current.Value; set => _Current.Value = value; }
        #endregion

        #region 构造
        private TimerX(Object target, MethodInfo method, Object state, String scheduler = null)
        {
            Target = new WeakReference(target);
            Method = method;
            State = state;

            // 使用开机滴答作为定时调度基准
            _nextTick = Runtime.TickCount64;
            _baseTime = DateTime.Now.AddMilliseconds(-_nextTick);

            Scheduler = (scheduler == null || scheduler.IsNullOrEmpty()) ? TimerScheduler.Default : TimerScheduler.Create(scheduler);
            //Scheduler.Add(this);

            TracerName = $"timer:{method.Name}";
        }

        private void Init(Int64 ms)
        {
            SetNextTick(ms);

            Scheduler.Add(this);
        }

        /// <summary>实例化一个不可重入的定时器</summary>
        /// <param name="callback">委托</param>
        /// <param name="state">用户数据</param>
        /// <param name="dueTime">多久之后开始。毫秒</param>
        /// <param name="period">间隔周期。毫秒</param>
        /// <param name="scheduler">调度器</param>
        public TimerX(TimerCallback callback, Object state, Int32 dueTime, Int32 period, String scheduler = null) : this(callback.Target, callback.Method, state, scheduler)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            if (dueTime < 0) throw new ArgumentOutOfRangeException(nameof(dueTime));

            Period = period;

            Init(dueTime);
        }

        /// <summary>实例化一个不可重入的定时器</summary>
        /// <param name="callback">委托</param>
        /// <param name="state">用户数据</param>
        /// <param name="dueTime">多久之后开始。毫秒</param>
        /// <param name="period">间隔周期。毫秒</param>
        /// <param name="scheduler">调度器</param>
        public TimerX(Func<Object, Task> callback, Object state, Int32 dueTime, Int32 period, String scheduler = null) : this(callback.Target, callback.Method, state, scheduler)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            if (dueTime < 0) throw new ArgumentOutOfRangeException(nameof(dueTime));

            IsAsyncTask = true;
            Async = true;
            Period = period;

            Init(dueTime);
        }

        /// <summary>实例化一个绝对定时器，指定时刻执行，跟当前时间和SetNext无关</summary>
        /// <param name="callback">委托</param>
        /// <param name="state">用户数据</param>
        /// <param name="startTime">绝对开始时间</param>
        /// <param name="period">间隔周期。毫秒</param>
        /// <param name="scheduler">调度器</param>
        public TimerX(TimerCallback callback, Object state, DateTime startTime, Int32 period, String scheduler = null) : this(callback.Target, callback.Method, state, scheduler)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            if (startTime <= DateTime.MinValue) throw new ArgumentOutOfRangeException(nameof(startTime));
            if (period <= 0) throw new ArgumentOutOfRangeException(nameof(period));

            Period = period;
            Absolutely = true;

            var now = DateTime.Now;
            var next = startTime;
            while (next < now) next = next.AddMilliseconds(period);

            var ms = (Int64)(next - now).TotalMilliseconds;
            _AbsolutelyNext = next;
            Init(ms);
        }

        /// <summary>实例化一个绝对定时器，指定时刻执行，跟当前时间和SetNext无关</summary>
        /// <param name="callback">委托</param>
        /// <param name="state">用户数据</param>
        /// <param name="startTime">绝对开始时间</param>
        /// <param name="period">间隔周期。毫秒</param>
        /// <param name="scheduler">调度器</param>
        public TimerX(Func<Object, Task> callback, Object state, DateTime startTime, Int32 period, String scheduler = null) : this(callback.Target, callback.Method, state, scheduler)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            if (startTime <= DateTime.MinValue) throw new ArgumentOutOfRangeException(nameof(startTime));
            if (period <= 0) throw new ArgumentOutOfRangeException(nameof(period));

            IsAsyncTask = true;
            Async = true;
            Period = period;
            Absolutely = true;

            var now = DateTime.Now;
            var next = startTime;
            while (next < now) next = next.AddMilliseconds(period);

            var ms = (Int64)(next - now).TotalMilliseconds;
            _AbsolutelyNext = next;
            Init(ms);
        }

        /// <summary>实例化一个Cron定时器</summary>
        /// <param name="callback">委托</param>
        /// <param name="state">用户数据</param>
        /// <param name="cronExpression">Cron表达式</param>
        /// <param name="scheduler">调度器</param>
        public TimerX(TimerCallback callback, Object state, String cronExpression, String scheduler = null) : this(callback.Target, callback.Method, state, scheduler)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            if (cronExpression.IsNullOrEmpty()) throw new ArgumentNullException(nameof(cronExpression));

            _cron = new Cron();
            if (!_cron.Parse(cronExpression)) throw new ArgumentException("无效的Cron表达式", nameof(cronExpression));

            Absolutely = true;

            var now = DateTime.Now;
            var next = _cron.GetNext(now);
            var ms = (Int64)(next - now).TotalMilliseconds;
            _AbsolutelyNext = next;
            Init(ms);
            //Init(_AbsolutelyNext = _cron.GetNext(DateTime.Now));
        }

        /// <summary>实例化一个Cron定时器</summary>
        /// <param name="callback">委托</param>
        /// <param name="state">用户数据</param>
        /// <param name="cronExpression">Cron表达式</param>
        /// <param name="scheduler">调度器</param>
        public TimerX(Func<Object, Task> callback, Object state, String cronExpression, String scheduler = null) : this(callback.Target, callback.Method, state, scheduler)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            if (cronExpression.IsNullOrEmpty()) throw new ArgumentNullException(nameof(cronExpression));

            _cron = new Cron();
            if (!_cron.Parse(cronExpression)) throw new ArgumentException("无效的Cron表达式", nameof(cronExpression));

            IsAsyncTask = true;
            Async = true;
            Absolutely = true;

            var now = DateTime.Now;
            var next = _cron.GetNext(now);
            var ms = (Int64)(next - now).TotalMilliseconds;
            _AbsolutelyNext = next;
            Init(ms);
            //Init(_AbsolutelyNext = _cron.GetNext(DateTime.Now));
        }

        /// <summary>销毁定时器</summary>
        public void Dispose()
        {
            Dispose(true);

            // 告诉GC，不要调用析构函数
            GC.SuppressFinalize(this);
        }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(Boolean disposing)
        {
            if (disposing)
            {
                // 释放托管资源
            }

            // 释放非托管资源
            Scheduler?.Remove(this, disposing ? "Dispose" : "GC");
        }
        #endregion

        #region 方法
        /// <summary>是否已设置下一次时间</summary>
        internal Boolean hasSetNext;

        private void SetNextTick(Int64 ms)
        {
            // 使用开机滴答来做定时调度，无惧时间回拨，每次修正时间基准
            var tick = Runtime.TickCount64;
            _baseTime = DateTime.Now.AddMilliseconds(-tick);
            _nextTick = tick + ms;
        }

        /// <summary>设置下一次运行时间</summary>
        /// <param name="ms">小于等于0表示马上调度</param>
        public void SetNext(Int32 ms)
        {
            //NextTime = DateTime.Now.AddMilliseconds(ms);

            SetNextTick(ms);

            hasSetNext = true;

            Scheduler.Wake();
        }

        /// <summary>设置下一次执行时间，并获取间隔</summary>
        /// <returns>返回下一次执行的间隔时间，不能小于等于0，否则定时器被销毁</returns>
        internal Int32 SetAndGetNextTime()
        {
            // 如果已设置
            var period = Period;
            var nowTick = Runtime.TickCount64;
            if (hasSetNext)
            {
                var ts = (Int32)(_nextTick - nowTick);
                return ts > 0 ? ts : period;
            }

            if (Absolutely)
            {
                // Cron以当前时间开始计算下一次
                // 绝对时间还没有到时，不计算下一次
                var now = DateTime.Now;
                DateTime next;
                if (_cron != null)
                {
                    next = _cron.GetNext(now);

                    // 如果cron计算得到的下一次时间过近，则需要重新计算
                    if ((next - now).TotalMilliseconds < 1000) next = _cron.GetNext(next);
                }
                else
                {
                    // 能够处理基准时间变大，但不能处理基准时间变小
                    next = _AbsolutelyNext;
                    while (next < now) next = next.AddMilliseconds(period);
                }

                // 即使基准时间改变，也不影响绝对时间定时器的执行时刻
                _AbsolutelyNext = next;
                var ts = (Int32)Math.Round((next - now).TotalMilliseconds);
                SetNextTick(ts);

                return ts > 0 ? ts : period;
            }
            else
            {
                //NextTime = DateTime.Now.AddMilliseconds(period);
                SetNextTick(period);

                return period;
            }
        }
        #endregion

        #region 静态方法
        /// <summary>延迟执行一个委托。特别要小心，很可能委托还没被执行，对象就被gc回收了</summary>
        /// <param name="callback"></param>
        /// <param name="ms"></param>
        /// <returns></returns>
        public static TimerX Delay(TimerCallback callback, Int32 ms) => new TimerX(callback, null, ms, 0) { Async = true };

        private static TimerX _NowTimer = null;
        private static DateTime _Now;
        /// <summary>当前时间。定时读取系统时间，避免频繁读取系统时间造成性能瓶颈</summary>
        public static DateTime Now
        {
            get
            {
                if (_NowTimer == null)
                {
                    lock (TimerScheduler.Default)
                    {
                        if (_NowTimer == null)
                        {
                            // 多线程下首次访问Now可能取得空时间
                            _Now = DateTime.Now;

                            _NowTimer = new TimerX(CopyNow, null, 0, 500);
                        }
                    }
                }

                return _Now;
            }
        }

        private static void CopyNow(Object state) => _Now = DateTime.Now;
        #endregion

        #region 辅助
        /// <summary>已重载</summary>
        /// <returns></returns>
        public override String ToString() => $"[{Id}]{Method.DeclaringType?.Name}.{Method.Name} ({(_cron != null ? _cron.ToString() : (Period + "ms"))})";
        #endregion
    }
}
