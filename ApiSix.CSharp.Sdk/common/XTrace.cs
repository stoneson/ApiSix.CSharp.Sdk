using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Threading;
using System.ComponentModel;
using System.IO;
using System.Runtime;
using System.Text;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Timers;

namespace ApiSix.CSharp
{
    #region ILog
    /// <summary>日志等级</summary>
    public enum LogLevel : System.Byte
    {
        /// <summary>打开所有日志记录</summary>
        All = 0,

        /// <summary>最低调试。细粒度信息事件对调试应用程序非常有帮助</summary>
        Debug,

        /// <summary>普通消息。在粗粒度级别上突出强调应用程序的运行过程</summary>
        Info,

        /// <summary>警告</summary>
        Warn,

        /// <summary>错误</summary>
        Error,

        /// <summary>严重错误</summary>
        Fatal,

        /// <summary>关闭所有日志记录</summary>
        Off = 0xFF
    }
    /// <summary>写日志事件参数</summary>
    public class WriteLogEventArgs : EventArgs
    {
        #region 属性
        /// <summary>日志等级</summary>
        public LogLevel Level { get; set; }

        /// <summary>日志信息</summary>
        public String Message { get; set; }

        /// <summary>异常</summary>
        public Exception Exception { get; set; }

        /// <summary>时间</summary>
        public DateTime Time { get; set; }

        /// <summary>线程编号</summary>
        public Int32 ThreadID { get; set; }

        /// <summary>是否线程池线程</summary>
        public Boolean IsPool { get; set; }

        /// <summary>是否Web线程</summary>
        public Boolean IsWeb { get; set; }

        /// <summary>线程名</summary>
        public String ThreadName { get; set; }

        /// <summary>任务编号</summary>
        public Int32 TaskID { get; set; }
        #endregion

        #region 构造
        /// <summary>实例化一个日志事件参数</summary>
        internal WriteLogEventArgs() { }
        #endregion

        #region 线程专有实例
        /*2015-06-01 @宁波-小董
         * 将Current以及Set方法组从internal修改为Public
         * 原因是 Logger在进行扩展时，重载OnWrite需要用到该静态属性以及方法，internal无法满足扩展要求
         * */
        [ThreadStatic]
        private static WriteLogEventArgs _Current;
        /// <summary>线程专有实例。线程静态，每个线程只用一个，避免GC浪费</summary>
        public static WriteLogEventArgs Current => _Current ??= new WriteLogEventArgs();
        #endregion

        #region 方法
        /// <summary>初始化为新日志</summary>
        /// <param name="level">日志等级</param>
        /// <returns>返回自身，链式写法</returns>
        public WriteLogEventArgs Set(LogLevel level)
        {
            Level = level;

            return this;
        }

        /// <summary>初始化为新日志</summary>
        /// <param name="message">日志</param>
        /// <param name="exception">异常</param>
        /// <returns>返回自身，链式写法</returns>
        public WriteLogEventArgs Set(String message, Exception exception)
        {
            Message = message;
            Exception = exception;

            Init();

            return this;
        }

        void Init()
        {
            Time = DateTime.Now;
            var thread = Thread.CurrentThread;
            ThreadID = thread.ManagedThreadId;
            IsPool = thread.IsThreadPoolThread;
            ThreadName = CurrentThreadName ?? thread.Name;

            var tid = Task.CurrentId;
            TaskID = tid != null ? tid.Value : -1;

            //IsWeb = System.Web.HttpContext.Current != null;
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString()
        {
            if (Exception != null) Message += Exception.GetMessage();

            var name = ThreadName;
            if (name.IsNullOrEmpty()) name = TaskID >= 0 ? TaskID + "" : "-";
            if (name.EqualIgnoreCase("Threadpool worker", ".NET ThreadPool Worker")) name = "P";
            if (name.EqualIgnoreCase("IO Threadpool worker")) name = "IO";
            if (name.EqualIgnoreCase(".NET Long Running Task")) name = "LongTask";

            return $"{Time:HH:mm:ss.fff} {ThreadID,2} {(IsPool ? (IsWeb ? 'W' : 'Y') : 'N')} {name} {Message}";
        }
        #endregion

        #region 日志线程名
        [ThreadStatic]
        private static String _threadName;
        /// <summary>设置当前线程输出日志时的线程名</summary>
        public static String CurrentThreadName { get => _threadName; set => _threadName = value; }
        #endregion
    }
    /// <summary>日志接口</summary>
    /// <remarks>
    /// </remarks>
    public interface ILog
    {
        /// <summary>写日志</summary>
        /// <param name="level">日志级别</param>
        /// <param name="format">格式化字符串</param>
        /// <param name="args">格式化参数</param>
        void Write(LogLevel level, String format, params Object[] args);

        /// <summary>调试日志</summary>
        /// <param name="format">格式化字符串</param>
        /// <param name="args">格式化参数</param>
        void Debug(String format, params Object[] args);

        /// <summary>信息日志</summary>
        /// <param name="format">格式化字符串</param>
        /// <param name="args">格式化参数</param>
        void Info(String format, params Object[] args);

        /// <summary>警告日志</summary>
        /// <param name="format">格式化字符串</param>
        /// <param name="args">格式化参数</param>
        void Warn(String format, params Object[] args);

        /// <summary>错误日志</summary>
        /// <param name="format">格式化字符串</param>
        /// <param name="args">格式化参数</param>
        void Error(String format, params Object[] args);

        /// <summary>严重错误日志</summary>
        /// <param name="format">格式化字符串</param>
        /// <param name="args">格式化参数</param>
        void Fatal(String format, params Object[] args);

        /// <summary>是否启用日志</summary>
        Boolean Enable { get; set; }

        /// <summary>日志等级，只输出大于等于该级别的日志，默认Info</summary>
        LogLevel Level { get; set; }
    }

    /// <summary>日志基类。提供日志的基本实现</summary>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public abstract class Logger : ILog
    {
        #region 主方法
        /// <summary>调试日志</summary>
        /// <param name="format">格式化字符串</param>
        /// <param name="args">格式化参数</param>
        public virtual void Debug(String format, params Object[] args) => Write(LogLevel.Debug, format, args);

        /// <summary>信息日志</summary>
        /// <param name="format">格式化字符串</param>
        /// <param name="args">格式化参数</param>
        public virtual void Info(String format, params Object[] args) => Write(LogLevel.Info, format, args);

        /// <summary>警告日志</summary>
        /// <param name="format">格式化字符串</param>
        /// <param name="args">格式化参数</param>
        public virtual void Warn(String format, params Object[] args) => Write(LogLevel.Warn, format, args);

        /// <summary>错误日志</summary>
        /// <param name="format">格式化字符串</param>
        /// <param name="args">格式化参数</param>
        public virtual void Error(String format, params Object[] args) => Write(LogLevel.Error, format, args);

        /// <summary>严重错误日志</summary>
        /// <param name="format">格式化字符串</param>
        /// <param name="args">格式化参数</param>
        public virtual void Fatal(String format, params Object[] args) => Write(LogLevel.Fatal, format, args);
        #endregion

        #region 核心方法
        /// <summary>写日志</summary>
        /// <param name="level"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public virtual void Write(LogLevel level, String format, params Object[] args)
        {
            if (Enable && level >= Level) OnWrite(level, format, args);
        }

        /// <summary>写日志</summary>
        /// <param name="level"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected abstract void OnWrite(LogLevel level, String format, params Object[] args);
        #endregion

        #region 辅助方法
        /// <summary>格式化参数，特殊处理异常和时间</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual String Format(String format, Object[] args)
        {
            //处理时间的格式化
            if (args != null && args.Length > 0)
            {
                // 特殊处理异常
                if (args.Length == 1 && args[0] is Exception ex && (format.IsNullOrEmpty() || format == "{0}"))
                    return ex.GetMessage();

                for (var i = 0; i < args.Length; i++)
                {
                    if (args[i] != null && args[i].GetType() == typeof(DateTime))
                    {
                        // 根据时间值的精确度选择不同的格式化输出
                        var dt = (DateTime)args[i];
                        if (dt.Millisecond > 0)
                            args[i] = dt.ToString("yyyy-MM-dd HH:mm:ss.fff");
                        else if (dt.Hour > 0 || dt.Minute > 0 || dt.Second > 0)
                            args[i] = dt.ToString("yyyy-MM-dd HH:mm:ss");
                        else
                            args[i] = dt.ToString("yyyy-MM-dd");
                    }
                }
            }
            if (args == null || args.Length <= 0) return format;

            //format = format.Replace("{", "{{").Replace("}", "}}");

            return String.Format(format, args);
        }
        #endregion

        #region 属性
        /// <summary>是否启用日志。默认true</summary>
        public virtual Boolean Enable { get; set; } = true;

        private LogLevel? _Level;
        /// <summary>日志等级，只输出大于等于该级别的日志，默认Info</summary>
        public virtual LogLevel Level
        {
            get
            {
                if (_Level != null) return _Level.Value;

                return LogLevel.Debug;
            }
            set { _Level = value; }
        }
        #endregion

        #region 静态空实现
        /// <summary>空日志实现</summary>
        public static ILog Null { get; } = new NullLogger();

        class NullLogger : Logger
        {
            public override Boolean Enable { get => false; set { } }

            protected override void OnWrite(LogLevel level, String format, params Object[] args) { }
        }
        #endregion

        #region 日志头
        /// <summary>输出日志头，包含所有环境信息</summary>
        protected static String GetHead()
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();
            var name = String.Empty;
            var ver = Environment.Version + "";
            var target = "";
            var asm = Assembly.GetEntryAssembly();
            if (asm != null)
            {
                if (String.IsNullOrEmpty(name))
                {
                    var att = asm.GetCustomAttribute<AssemblyTitleAttribute>();
                    if (att != null) name = att.Title;
                }

                if (String.IsNullOrEmpty(name))
                {
                    var att = asm.GetCustomAttribute<AssemblyProductAttribute>();
                    if (att != null) name = att.Product;
                }

                if (String.IsNullOrEmpty(name))
                {
                    var att = asm.GetCustomAttribute<AssemblyDescriptionAttribute>();
                    if (att != null) name = att.Description;
                }

                var tar = asm.GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>();
                if (tar != null) target = !tar.FrameworkDisplayName.IsNullOrEmpty() ? tar.FrameworkDisplayName : tar.FrameworkName;
            }
#if !NETFRAMEWORK
            target = RuntimeInformation.FrameworkDescription;
#endif

            if (String.IsNullOrEmpty(name))
            {
                try
                {
                    name = process.ProcessName;
                }
                catch { }
            }
            var sb = new StringBuilder();
            sb.AppendFormat("#Software: {0}\r\n", name);
            sb.AppendFormat("#ProcessID: {0}{1}\r\n", process.Id, Environment.Is64BitProcess ? " x64" : "");
            sb.AppendFormat("#AppDomain: {0}\r\n", AppDomain.CurrentDomain.FriendlyName);

            var fileName = String.Empty;
            // MonoAndroid无法识别MainModule，致命异常
            try
            {
                fileName = process.MainModule.FileName;
            }
            catch { }
            if (fileName.IsNullOrEmpty() || fileName.EndsWithIgnoreCase("dotnet", "dotnet.exe"))
            {
                try
                {
                    fileName = process.StartInfo.FileName;
                }
                catch { }
            }
            if (!fileName.IsNullOrEmpty()) sb.AppendFormat("#FileName: {0}\r\n", fileName);

            // 应用域目录
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            sb.AppendFormat("#BaseDirectory: {0}\r\n", baseDir);

            // 当前目录。如果由别的进程启动，默认的当前目录就是父级进程的当前目录
            var curDir = Environment.CurrentDirectory;
            //if (!curDir.EqualIC(baseDir) && !(curDir + "\\").EqualIC(baseDir))
            if (!baseDir.EqualIgnoreCase(curDir, curDir + "\\", curDir + "/"))
                sb.AppendFormat("#CurrentDirectory: {0}\r\n", curDir);

            var basePath = PathHelper.BasePath;
            if (basePath != baseDir)
                sb.AppendFormat("#BasePath: {0}\r\n", basePath);

            // 临时目录
            sb.AppendFormat("#TempPath: {0}\r\n", Path.GetTempPath());

            // 命令行不为空，也不是文件名时，才输出
            // 当使用cmd启动程序时，这里就是用户输入的整个命令行，所以可能包含空格和各种符号
            var line = Environment.CommandLine;
            if (!line.IsNullOrEmpty())
                sb.AppendFormat("#CommandLine: {0}\r\n", line);

            var apptype = "";
            if (Runtime.IsWeb)
                apptype = "Web";
            else if (!Environment.UserInteractive)
                apptype = "Service";
            else if (Runtime.IsConsole)
                apptype = "Console";
            else
                apptype = "WinForm";

            if (Runtime.Container) apptype += "(Container)";

            sb.AppendFormat("#ApplicationType: {0}\r\n", apptype);
            sb.AppendFormat("#CLR: {0}, {1}\r\n", ver, target);

            var os = "";
            // 获取丰富的机器信息，需要提注册 MachineInfo.RegisterAsync
            var mi = MachineInfo.Current;
            if (mi != null)
            {
                os = mi.OSName + " " + mi.OSVersion;
            }
            else
            {
                // 特别识别Linux发行版
                os = Environment.OSVersion + "";
                if (Runtime.Linux) os = MachineInfo.GetLinuxName();
            }

            sb.AppendFormat("#OS: {0}, {1}/{2}\r\n", os, Environment.MachineName, Environment.UserName);
            sb.AppendFormat("#CPU: {0}\r\n", Environment.ProcessorCount);
            if (mi != null)
            {
                sb.AppendFormat("#Memory: {0:n0}M/{1:n0}M\r\n", mi.AvailableMemory / 1024 / 1024, mi.Memory / 1024 / 1024);
                sb.AppendFormat("#Processor: {0}\r\n", mi.Processor);
                if (!mi.Product.IsNullOrEmpty()) sb.AppendFormat("#Product: {0}\r\n", mi.Product);
                if (mi.Temperature > 0) sb.AppendFormat("#Temperature: {0}\r\n", mi.Temperature);
            }
            sb.AppendFormat("#GC: IsServerGC={0}, LatencyMode={1}\r\n", GCSettings.IsServerGC, GCSettings.LatencyMode);

            ThreadPool.GetMinThreads(out var minWorker, out var minIO);
            ThreadPool.GetMaxThreads(out var maxWorker, out var maxIO);
            ThreadPool.GetAvailableThreads(out var avaWorker, out var avaIO);
            sb.AppendFormat("#ThreadPool: Min={0}/{1}, Max={2}/{3}, Available={4}/{5}\r\n", minWorker, minIO, maxWorker, maxIO, avaWorker, avaIO);

            sb.AppendFormat("#SystemStarted: {0}\r\n", TimeSpan.FromMilliseconds(Runtime.TickCount64));
            sb.AppendFormat("#Date: {0:yyyy-MM-dd}\r\n", DateTime.Now);
            sb.AppendFormat("#字段: 时间 线程ID 线程池Y/网页W/普通N/定时T 线程名/任务ID 消息内容\r\n");
            sb.AppendFormat("#Fields: Time ThreadID Kind Name Message\r\n");

            return sb.ToString();
        }
        #endregion
    }

    #region 静态空实现
    class NullLogger : Logger
    {
        public override Boolean Enable { get => false; set { } }

        protected override void OnWrite(LogLevel level, String format, params Object[] args) { }
    }
    #endregion

    /// <summary>复合日志提供者，多种方式输出</summary>
    public class CompositeLog : Logger
    {
        /// <summary>日志提供者集合</summary>
        public List<ILog> Logs { get; set; } = new List<ILog>();

        /// <summary>日志等级，只输出大于等于该级别的日志，默认Info，打开ApiSix.Debug时默认为最低的Debug</summary>
        public override LogLevel Level
        {
            get => base.Level; set
            {
                base.Level = value;

                foreach (var item in Logs)
                {
                    // 使用外层层级
                    item.Level = Level;
                }
            }
        }

        /// <summary>实例化</summary>
        public CompositeLog() { }

        /// <summary>实例化</summary>
        /// <param name="log"></param>
        public CompositeLog(ILog log) { Logs.Add(log); Level = log.Level; }

        /// <summary>实例化</summary>
        /// <param name="log1"></param>
        /// <param name="log2"></param>
        public CompositeLog(ILog log1, ILog log2)
        {
            Add(log1).Add(log2);
            Level = log1.Level;
            if (Level > log2.Level) Level = log2.Level;
        }

        /// <summary>添加一个日志提供者</summary>
        /// <param name="log"></param>
        /// <returns></returns>
        public CompositeLog Add(ILog log) { Logs.Add(log); return this; }

        /// <summary>删除日志提供者</summary>
        /// <param name="log"></param>
        /// <returns></returns>
        public CompositeLog Remove(ILog log) { if (Logs.Contains(log)) Logs.Remove(log); return this; }

        /// <summary>写日志</summary>
        /// <param name="level"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected override void OnWrite(LogLevel level, String format, params Object[] args)
        {
            if (Logs != null)
            {
                foreach (var item in Logs)
                {
                    item.Write(level, format, args);
                }
            }
        }

        /// <summary>从复合日志提供者中提取指定类型的日志提供者</summary>
        /// <typeparam name="TLog"></typeparam>
        /// <returns></returns>
        public TLog Get<TLog>() where TLog : class
        {
            foreach (var item in Logs)
            {
                if (item != null)
                {
                    if (item is TLog) return item as TLog;

                    // 递归获取内层日志
                    if (item is CompositeLog cmp)
                    {
                        var log = cmp.Get<TLog>();
                        if (log != null) return log;
                    }
                }
            }

            return null;
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString()
        {
            var sb = new StringBuilder();
            sb.Append(GetType().Name);

            foreach (var item in Logs)
            {
                sb.Append(' ');
                sb.Append(item + "");
            }

            return sb.ToString();
        }
    }
    /// <summary>文本文件日志类。提供向文本文件写日志的能力</summary>
    /// <remarks>
    /// 两大用法：
    /// 1，Create(path, fileFormat) 指定日志目录和文件名格式
    /// 2，CreateFile(path) 指定文件，一直往里面写
    /// 
    /// 2015-06-01 为了继承TextFileLog，增加了无参构造函数，修改了异步写日志方法为虚方法，可以进行重载
    /// </remarks>
    public class TextFileLog : Logger, IDisposable
    {
        #region 属性
        /// <summary>日志目录</summary>
        public String LogPath { get; set; } = "logs";

        /// <summary>日志文件格式。默认{0:yyyy_MM_dd}.log</summary>
        public String FileFormat { get; set; } = "{0:yyyy_MM_dd}.log";

        /// <summary>日志文件上限。超过上限后拆分新日志文件，默认10MB，0表示不限制大小</summary>
        public Int32 MaxBytes { get; set; } = 10;

        /// <summary>日志文件备份。超过备份数后，最旧的文件将被删除，默认100，0表示不限制个数</summary>
        public Int32 Backups { get; set; } = 100;

        private readonly Boolean _isFile = false;

        /// <summary>是否当前进程的第一次写日志</summary>
        private Boolean _isFirst = false;
        #endregion

        #region 构造
        /// <summary>该构造函数没有作用，为了继承而设置</summary>
        public TextFileLog() { }

        internal TextFileLog(String path, Boolean isfile, String fileFormat = null)
        {
            LogPath = path;
            _isFile = isfile;

            //var set = Setting.Current;
            if (!fileFormat.IsNullOrEmpty())
                FileFormat = fileFormat;
            else
                FileFormat = "{0:yyyy_MM_dd}.log";


            _Timer = new TimerX(DoWriteAndClose, null, 0_000, 5_000) { Async = true };
        }

        private static readonly ConcurrentDictionary<String, TextFileLog> cache = new ConcurrentDictionary<string, TextFileLog>(StringComparer.OrdinalIgnoreCase);
        /// <summary>每个目录的日志实例应该只有一个，所以采用静态创建</summary>
        /// <param name="path">日志目录或日志文件路径</param>
        /// <param name="fileFormat"></param>
        /// <returns></returns>
        public static TextFileLog Create(String path, String fileFormat = null)
        {
            //if (path.IsNullOrEmpty()) path = XTrace.LogPath;
            if (path.IsNullOrEmpty()) path = "Logs";

            var key = (path + fileFormat).ToLower();
            return cache.GetOrAdd(key, k => new TextFileLog(path, false, fileFormat));
        }

        /// <summary>每个目录的日志实例应该只有一个，所以采用静态创建</summary>
        /// <param name="path">日志目录或日志文件路径</param>
        /// <returns></returns>
        public static TextFileLog CreateFile(String path)
        {
            if (path.IsNullOrEmpty()) throw new ArgumentNullException(nameof(path));

            return cache.GetOrAdd(path, k => new TextFileLog(k, true));
        }

        /// <summary>销毁</summary>
        public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(Boolean disposing)
        {
            _Timer.TryDispose();

            // 销毁前把队列日志输出
            if (Interlocked.CompareExchange(ref _writing, 1, 0) == 0) WriteAndClose(DateTime.MinValue);
        }
        #endregion

        #region 内部方法
        private StreamWriter LogWriter;
        private String CurrentLogFile;
        private Int32 _logFileError;

        /// <summary>初始化日志记录文件</summary>
        private StreamWriter InitLog(String logfile)
        {
            try
            {
                logfile.EnsureDirectory(true);

                var stream = new FileStream(logfile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                var writer = new StreamWriter(stream, Encoding.UTF8);

                // 写日志头
                if (!_isFirst)
                {
                    _isFirst = true;

                    // 因为指定了编码，比如UTF8，开头就会写入3个字节，所以这里不能拿长度跟0比较
                    if (writer.BaseStream.Length > 10) writer.WriteLine();

                    writer.Write(GetHead());
                }

                _logFileError = 0;
                return LogWriter = writer;
            }
            catch (Exception ex)
            {
                _logFileError++;
                Console.WriteLine("创建日志文件失败：{0}", ex.Message);
                return null;
            }
        }

        /// <summary>获取日志文件路径</summary>
        /// <returns></returns>
        private String GetLogFile()
        {
            // 单日志文件
            if (_isFile) return LogPath.GetBasePath();

            // 目录多日志文件
            var logfile = LogPath.CombinePath(String.Format(FileFormat, TimerX.Now, Level)).GetBasePath();

            // 是否限制文件大小
            if (MaxBytes == 0) return logfile;

            // 找到今天第一个未达到最大上限的文件
            var max = MaxBytes * 1024L * 1024L;
            var ext = Path.GetExtension(logfile);
            var name = logfile.TrimEnd(ext);
            for (var i = 1; i < 1024; i++)
            {
                if (i > 1) logfile = $"{name}_{i}{ext}";

                var fi = logfile.AsFile();
                if (!fi.Exists || fi.Length < max) return logfile;
            }

            return null;
        }
        #endregion

        #region 异步写日志
        private readonly TimerX _Timer;
        private readonly ConcurrentQueue<String> _Logs = new ConcurrentQueue<string>();
        private volatile Int32 _logCount;
        private Int32 _writing;
        private DateTime _NextClose;

        /// <summary>写文件</summary>
        protected virtual void WriteFile()
        {
            var writer = LogWriter;

            var now = TimerX.Now;
            var logFile = GetLogFile();
            if (!_isFile && logFile != CurrentLogFile)
            {
                writer.TryDispose();
                writer = null;

                CurrentLogFile = logFile;
                _logFileError = 0;
            }

            // 错误过多时不再尝试创建日志文件。下一天更换日志文件名后，将会再次尝试
            if (writer == null && _logFileError >= 3) return;

            // 初始化日志读写器
            if (writer == null) writer = InitLog(logFile);
            if (writer == null) return;

            // 依次把队列日志写入文件
            while (_Logs.TryDequeue(out var str))
            {
                Interlocked.Decrement(ref _logCount);

                // 写日志。TextWriter.WriteLine内需要拷贝，浪费资源
                //writer.WriteLine(str);
                writer.Write(str);
                writer.WriteLine();
            }

            // 写完一批后，刷一次磁盘
            writer?.Flush();

            // 连续5秒没日志，就关闭
            _NextClose = now.AddSeconds(5);
        }

        /// <summary>关闭文件</summary>
        private void DoWriteAndClose(Object state)
        {
            // 同步写日志
            if (Interlocked.CompareExchange(ref _writing, 1, 0) == 0) WriteAndClose(_NextClose);

            // 检查文件是否超过上限
            if (!_isFile && Backups > 0)
            {
                // 判断日志目录是否已存在
                var di = LogPath.GetBasePath().AsDirectory();
                if (di.Exists)
                {
                    // 删除*.del
                    try
                    {
                        var dels = di.GetFiles("*.del");
                        if (dels != null && dels.Length > 0)
                        {
                            foreach (var item in dels)
                            {
                                item.Delete();
                            }
                        }
                    }
                    catch { }

                    var ext = Path.GetExtension(FileFormat);
                    var fis = di.GetFiles("*" + ext);
                    if (fis != null && fis.Length > Backups)
                    {
                        // 删除最旧的文件
                        var retain = fis.Length - Backups;
                        fis = fis.OrderBy(e => e.CreationTime).Take(retain).ToArray();
                        foreach (var item in fis)
                        {
                            OnWrite(LogLevel.Info, "日志文件达到上限 {0}，删除 {1}，大小 {2:n0}Byte", Backups, item.Name, item.Length);
                            try
                            {
                                item.Delete();
                            }
                            catch
                            {
                                item.MoveTo(item.FullName + ".del");
                            }
                        }
                    }
                }
            }
        }

        /// <summary>写入队列日志并关闭文件</summary>
        protected virtual void WriteAndClose(DateTime closeTime)
        {
            try
            {
                // 处理残余
                var writer = LogWriter;
                if (!_Logs.IsEmpty) WriteFile();

                // 连续5秒没日志，就关闭
                if (writer != null && closeTime < TimerX.Now)
                {
                    writer.TryDispose();
                    LogWriter = null;
                }
            }
            finally
            {
                _writing = 0;
            }
        }
        #endregion

        #region 写日志
        /// <summary>写日志</summary>
        /// <param name="level"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected override void OnWrite(LogLevel level, String format, params Object[] args)
        {
            // 据@夏玉龙反馈，如果不给Log目录写入权限，日志队列积压将会导致内存暴增
            if (_logCount > 100) return;

            var e = WriteLogEventArgs.Current.Set(level);
            // 特殊处理异常对象
            if (args != null && args.Length == 1 && args[0] is Exception ex && (format.IsNullOrEmpty() || format == "{0}"))
                e = e.Set(null, ex);
            else
                e = e.Set(Format(format, args), null);

            // 推入队列
            _Logs.Enqueue(e.ToString());
            Interlocked.Increment(ref _logCount);

            // 异步写日志，实时。即使这里错误，定时器那边仍然会补上
            if (Interlocked.CompareExchange(ref _writing, 1, 0) == 0)
            {
                // 调试级别 或 致命错误 同步写日志
                if (XTrace.LogLevel <= LogLevel.Debug || Level >= LogLevel.Error)
                {
                    try
                    {
                        WriteFile();
                    }
                    finally
                    {
                        _writing = 0;
                    }
                }
                else
                {
                    ThreadPool.UnsafeQueueUserWorkItem(s =>
                    {
                        try
                        {
                            WriteFile();
                        }
                        catch { }
                        finally
                        {
                            _writing = 0;
                        }
                    }, null);
                }
            }
        }
        #endregion

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString() => $"{GetType().Name} {LogPath}";
        #endregion

    }
    /// <summary>等级日志提供者，不同等级分不同日志输出</summary>
    public class LevelLog : Logger
    {
        private IDictionary<LogLevel, ILog> _logs = new Dictionary<LogLevel, ILog>();

        /// <summary>通过指定路径和文件格式来实例化等级日志，每个等级使用自己的日志输出</summary>
        /// <param name="logPath"></param>
        /// <param name="fileFormat"></param>
        public LevelLog(String logPath, String fileFormat)
        {
            foreach (LogLevel item in Enum.GetValues(typeof(LogLevel)))
            {
                if (item > LogLevel.All && item  < LogLevel.Off)
                {
                    _logs[item] = new TextFileLog(logPath, false, fileFormat) { Level = item };
                }
            }
        }

        /// <summary>写日志</summary>
        /// <param name="level"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected override void OnWrite(LogLevel level, String format, params Object[] args)
        {
            if (_logs.TryGetValue(level, out var log)) log.Write(level, format, args);
        }
    }
    /// <summary>控制台输出日志</summary>
    public class ConsoleLog : Logger
    {
        /// <summary>是否使用多种颜色，默认使用</summary>
        public Boolean UseColor { get; set; } = true;

        /// <summary>写日志</summary>
        /// <param name="level"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected override void OnWrite(LogLevel level, String format, params Object[] args)
        {
            // 吃掉异常，避免应用崩溃
            try
            {
                var e = WriteLogEventArgs.Current.Set(level).Set(Format(format, args), null);

                if (!UseColor)
                {
                    Console.WriteLine(e);
                    return;
                }

                lock (this)
                {
                    var cc = Console.ForegroundColor;
                    switch (level)
                    {
                        case LogLevel.Warn: cc = ConsoleColor.Yellow; break;
                        case LogLevel.Error:
                        case LogLevel.Fatal: cc = ConsoleColor.Red; break;
                        default: cc = GetColor(e.ThreadID); break;
                    }
                    var old = Console.ForegroundColor;
                    Console.ForegroundColor = cc;
                    Console.WriteLine(e);
                    Console.ForegroundColor = old;
                }
            }
            catch { }
        }

        static readonly ConcurrentDictionary<Int32, ConsoleColor> dic = new ConcurrentDictionary<int, ConsoleColor>();
        static readonly ConsoleColor[] colors = new ConsoleColor[] {
        ConsoleColor.Green, ConsoleColor.Cyan, ConsoleColor.Magenta, ConsoleColor.White, ConsoleColor.Yellow,
        ConsoleColor.DarkGreen, ConsoleColor.DarkCyan, ConsoleColor.DarkMagenta, ConsoleColor.DarkRed, ConsoleColor.DarkYellow };
        private static ConsoleColor GetColor(Int32 threadid)
        {
            if (threadid == 1) return ConsoleColor.Gray;

            return dic.GetOrAdd(threadid, k => colors[k % colors.Length]);
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString() => $"{GetType().Name} UseColor={UseColor}";
    }
    #endregion

    /// <summary>日志类，包含跟踪调试功能</summary>
    /// <remarks>
    /// 该静态类包括写日志、写调用栈和Dump进程内存等调试功能。
    ///
    /// 默认写日志到文本文件，可通过修改<see cref="Log"/>属性来增加日志输出方式。
    /// 对于控制台工程，可以直接通过UseConsole方法，把日志输出重定向为控制台输出，并且可以为不同线程使用不同颜色。
    /// </remarks>
    public static class XTrace
    {
        #region 写日志
        /// <summary>文本文件日志</summary>
        private static ILog _Log = Logger.Null;

        /// <summary>日志提供者，默认使用文本文件日志</summary>
        public static ILog Log
        { get { InitLog(); return _Log; } set { _Log = value; } }

        /// <summary>输出日志</summary>
        /// <param name="msg">信息</param>
        public static void WriteLine(String msg)
        {
            if (!InitLog()) return;

            WriteVersion();

            Log.Info(msg);
        }

        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void WriteLine(String format, params Object[] args)
        {
            if (!InitLog()) return;

            WriteVersion();

            Log.Info(format, args);
        }

        ///// <summary>异步写日志</summary>
        ///// <param name="format"></param>
        ///// <param name="args"></param>
        //public static void WriteLineAsync(String format, params Object[] args)
        //{
        //    ThreadPool.QueueUserWorkItem(s => WriteLine(format, args));
        //}

        /// <summary>输出异常日志</summary>
        /// <param name="ex">异常信息</param>
        public static void WriteException(Exception ex)
        {
            if (!InitLog()) return;

            WriteVersion();

            Log.Error("{0}", ex);
        }

        #endregion 写日志

        #region 构造

        static XTrace()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
#if NETCOREAPP
        System.Runtime.Loader.AssemblyLoadContext.Default.Unloading += ctx => OnProcessExit(null, EventArgs.Empty);
#endif

            ThreadPoolX.Init();

            try
            {
                Debug = true;
                LogPath = "Log";
            }
            catch { }
        }

        private static void CurrentDomain_UnhandledException(Object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                // 全局异常埋点
                //DefaultTracer.Instance?.NewError(ex.GetType().Name, ex);
                WriteException(ex);
            }
            if (e.IsTerminating)
            {
                Log.Fatal("异常退出！");

                if (Log is CompositeLog compositeLog)
                {
                    var log = compositeLog.Get<TextFileLog>();
                    log.TryDispose();
                }
            }
        }

        private static void TaskScheduler_UnobservedTaskException(Object sender, UnobservedTaskExceptionEventArgs e)
        {
            if (!e.Observed && e.Exception != null)
            {
                //WriteException(e.Exception);
                foreach (var ex in e.Exception.Flatten().InnerExceptions)
                {
                    // 全局异常埋点
                    //DefaultTracer.Instance?.NewError(ex.GetType().Name, ex);
                    WriteException(ex);
                }
                e.SetObserved();
            }
        }

        private static void OnProcessExit(Object sender, EventArgs e)
        {
            if (Log is CompositeLog compositeLog)
            {
                var log = compositeLog.Get<TextFileLog>();
                log.TryDispose();
            }
        }

        private static readonly Object _lock = new object();
        private static Int32 _initing = 0;

        /// <summary>
        /// 2012.11.05 修正初次调用的时候，由于同步BUG，导致Log为空的问题。
        /// </summary>
        private static Boolean InitLog()
        {
            /*
             * 日志初始化可能会触发配置模块，其内部又写日志导致死循环。
             * 1，外部写日志引发初始化
             * 2，标识日志初始化正在进行中
             * 3，初始化日志提供者
             * 4，此时如果再次引发写入日志，发现正在进行中，放弃写入的日志
             * 5，标识日志初始化已完成
             * 6，正常写入日志
             */

            if (_Log != null && _Log != Logger.Null) return true;
            if (_initing > 0 && _initing == Thread.CurrentThread.ManagedThreadId) return false;

            lock (_lock)
            {
                if (_Log != null && _Log != Logger.Null) return true;

                _initing = Thread.CurrentThread.ManagedThreadId;

                if (LogPath.IsNullOrEmpty() || LogPath == "Log") LogPath = "logs";
                if (LogFileFormat.Contains("{1}"))
                    _Log = new LevelLog(LogPath, LogFileFormat);
                else
                    _Log = TextFileLog.Create(LogPath);

                _initing = 0;
            }

            //WriteVersion();

            return true;
        }

        #endregion 构造

        #region 使用控制台输出

        private static Boolean _useConsole;

        /// <summary>使用控制台输出日志，只能调用一次</summary>
        /// <param name="useColor">是否使用颜色，默认使用</param>
        /// <param name="useFileLog">是否同时使用文件日志，默认使用</param>
        public static void UseConsole(Boolean useColor = true, Boolean useFileLog = true)
        {
            if (_useConsole) return;
            _useConsole = true;

            //if (!Runtime.IsConsole) return;
            Runtime.IsConsole = true;

            // 适当加大控制台窗口
            try
            {
#if !NETFRAMEWORK
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    if (Console.WindowWidth <= 80) Console.WindowWidth = Console.WindowWidth * 3 / 2;
                    if (Console.WindowHeight <= 25) Console.WindowHeight = Console.WindowHeight * 3 / 2;
                }
#else
            if (Console.WindowWidth <= 80) Console.WindowWidth = Console.WindowWidth * 3 / 2;
            if (Console.WindowHeight <= 25) Console.WindowHeight = Console.WindowHeight * 3 / 2;
#endif
            }
            catch { }

            var clg = new ConsoleLog { UseColor = useColor };
            if (useFileLog)
                _Log = new CompositeLog(clg, Log);
            else
                _Log = clg;
        }

        #endregion 使用控制台输出

        #region 控制台禁用快捷编辑

        /// <summary>
        /// 禁用控制台快捷编辑，在UseConsole方法之后调用
        /// </summary>
        public static void DisbleConsoleEdit()
        {
            if (!_useConsole) return;
            try
            {
                if (Runtime.Windows)
                {
                    ConsoleHelper.DisbleQuickEditMode();
                }
            }
            catch
            {
            }
        }

        #endregion 控制台禁用快捷编辑

        #region 控制台禁用关闭按钮

        /// <summary>
        /// 禁用控制台关闭按钮
        /// </summary>
        /// <param name="consoleTitle">控制台程序名称，可使用Console.Title动态设置的值</param>
        public static void DisbleConsoleCloseBtn(string consoleTitle)
        {
            try
            {
                if (Runtime.Windows)
                {
                    ConsoleHelper.DisbleCloseBtn(consoleTitle);
                }
            }
            catch
            {
            }
        }

        #endregion 控制台禁用关闭按钮

        #region 拦截WinForm异常

#if __WIN__
    private static Int32 initWF = 0;
    private static Boolean _ShowErrorMessage;
    //private static String _Title;

    /// <summary>拦截WinForm异常并记录日志，可指定是否用<see cref="MessageBox"/>显示。</summary>
    /// <param name="showErrorMessage">发为捕获异常时，是否显示提示，默认显示</param>
    public static void UseWinForm(Boolean showErrorMessage = true)
    {
        Runtime.IsConsole = false;

        _ShowErrorMessage = showErrorMessage;

        if (initWF > 0 || Interlocked.CompareExchange(ref initWF, 1, 0) != 0) return;
        //if (!Application.MessageLoop) return;

        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException2;
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += Application_ThreadException;
    }

    private static void CurrentDomain_UnhandledException2(Object sender, UnhandledExceptionEventArgs e)
    {
        var show = _ShowErrorMessage && Application.MessageLoop;
        var ex = e.ExceptionObject as Exception;
        var title = e.IsTerminating ? "异常退出" : "出错";
        if (show) MessageBox.Show(ex?.Message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private static void Application_ThreadException(Object sender, ThreadExceptionEventArgs e)
    {
        WriteException(e.Exception);

        var show = _ShowErrorMessage && Application.MessageLoop;
        if (show) MessageBox.Show(e.Exception == null ? "" : e.Exception.Message, "出错", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    /// <summary>在WinForm控件上输出日志，主要考虑非UI线程操作</summary>
    /// <remarks>不是常用功能，为了避免干扰常用功能，保持UseWinForm开头</remarks>
    /// <param name="control">要绑定日志输出的WinForm控件</param>
    /// <param name="useFileLog">是否同时使用文件日志，默认使用</param>
    /// <param name="maxLines">最大行数</param>
    public static void UseWinFormControl(this Control control, Boolean useFileLog = true, Int32 maxLines = 1000)
    {
        var clg = _Log as TextControlLog;
        var ftl = _Log as TextFileLog;
        if (_Log is CompositeLog cmp)
        {
            ftl = cmp.Get<TextFileLog>();
            clg = cmp.Get<TextControlLog>();
        }

        // 控制控制台日志
        if (clg == null) clg = new TextControlLog();
        clg.Control = control;
        clg.MaxLines = maxLines;

        if (!useFileLog)
        {
            Log = clg;
            if (ftl != null) ftl.Dispose();
        }
        else
        {
            if (ftl == null) ftl = TextFileLog.Create(null);
            Log = new CompositeLog(clg, ftl);
        }
    }

    /// <summary>控件绑定到日志，生成混合日志</summary>
    /// <param name="control"></param>
    /// <param name="log"></param>
    /// <param name="maxLines"></param>
    /// <returns></returns>
    public static ILog Combine(this Control control, ILog log, Int32 maxLines = 1000)
    {
        //if (control == null || log == null) return log;

        var clg = new TextControlLog
        {
            Control = control,
            MaxLines = maxLines
        };

        return new CompositeLog(log, clg);
    }

#endif

        #endregion 拦截WinForm异常

        #region 属性
        /// <summary>日志等级，只输出大于等于该级别的日志，All/Debug/Info/Warn/Error/Fatal，默认Info</summary>
        [Description("日志等级。只输出大于等于该级别的日志，All/Debug/Info/Warn/Error/Fatal，默认Info")]
        public static LogLevel LogLevel { get; set; } = LogLevel.Info;
        /// <summary>是否调试。</summary>
        public static Boolean Debug { get; set; }

        /// <summary>文本日志目录</summary>
        public static String LogPath { get; set; }
        /// <summary>日志文件格式。默认{0:yyyy_MM_dd}.log</summary>
        public static String LogFileFormat { get; set; } = "{0:yyyy_MM_dd}.log";
        ///// <summary>临时目录</summary>
        //public static String TempPath { get; set; } = Setting.Current.TempPath;

        #endregion 属性

        #region 版本信息

        private static Int32 _writeVersion;

        /// <summary>输出核心库和启动程序的版本号</summary>
        public static void WriteVersion()
        {
            if (_writeVersion > 0 || Interlocked.CompareExchange(ref _writeVersion, 1, 0) != 0) return;

            var asm = Assembly.GetExecutingAssembly();
            WriteVersion(asm);

            var asm2 = Assembly.GetEntryAssembly();
            if (asm2 != null && asm2 != asm) WriteVersion(asm2);
        }

        /// <summary>输出程序集版本</summary>
        /// <param name="asm"></param>
        public static void WriteVersion(this Assembly asm)
        {
            if (asm == null) return;

            var asmx = AssemblyX.Create(asm);
            if (asmx != null)
            {
                var ver = "";
                var tar = asm.GetCustomAttribute<TargetFrameworkAttribute>();
                if (tar != null)
                {
                    ver = tar.FrameworkDisplayName;
                    if (ver.IsNullOrEmpty()) ver = tar.FrameworkName;
                }

                WriteLine("{0} v{1} Build {2:yyyy-MM-dd HH:mm:ss} {3}", asmx.Name, asmx.FileVersion, asmx.Compile, ver);
                var att = asmx.Asm.GetCustomAttribute<AssemblyCopyrightAttribute>();
                WriteLine("{0} {1}", asmx.Title, att?.Copyright);
            }
        }

        #endregion 版本信息
    }
}
