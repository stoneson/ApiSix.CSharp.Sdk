using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Net;
using System.Collections.Concurrent;
using System.Collections;
using System.Drawing;

namespace ApiSix.CSharp
{
    #region IBinary
    /// <summary>二进制序列化接口</summary>
    public interface IBinary : IFormatterX
    {
        #region 属性
        /// <summary>编码整数</summary>
        Boolean EncodeInt { get; set; }

        /// <summary>小端字节序。默认false大端</summary>
        Boolean IsLittleEndian { get; set; }

        ///// <summary>使用指定大小的FieldSizeAttribute特性，默认false</summary>
        //Boolean UseFieldSize { get; set; }

        /// <summary>要忽略的成员</summary>
        ICollection<String> IgnoreMembers { get; set; }

        /// <summary>处理器列表</summary>
        IList<IBinaryHandler> Handlers { get; }
        #endregion

        #region 写入
        /// <summary>写入字节</summary>
        /// <param name="value"></param>
        void Write(Byte value);

        /// <summary>将字节数组部分写入当前流，不写入数组长度。</summary>
        /// <param name="buffer">包含要写入的数据的字节数组。</param>
        /// <param name="offset">buffer 中开始写入的起始点。</param>
        /// <param name="count">要写入的字节数。</param>
        void Write(Byte[] buffer, Int32 offset, Int32 count);

        /// <summary>写入大小</summary>
        /// <param name="size">要写入的大小值</param>
        /// <returns>返回特性指定的固定长度，如果没有则返回-1</returns>
        Int32 WriteSize(Int32 size);
        #endregion

        #region 读取
        /// <summary>读取字节</summary>
        /// <returns></returns>
        Byte ReadByte();

        /// <summary>从当前流中将 count 个字节读入字节数组</summary>
        /// <param name="count">要读取的字节数。</param>
        /// <returns></returns>
        Byte[] ReadBytes(Int32 count);

        /// <summary>读取大小</summary>
        /// <returns></returns>
        Int32 ReadSize();
        #endregion
    }

    /// <summary>二进制读写处理器接口</summary>
    public interface IBinaryHandler : IHandler<IBinary>
    {
    }

    /// <summary>二进制读写处理器基类</summary>
    public abstract class BinaryHandlerBase : HandlerBase<IBinary, IBinaryHandler>, IBinaryHandler
    {
    }

    /// <summary>序列化接口</summary>
    public interface IFormatterX
    {
        #region 属性
        /// <summary>数据流</summary>
        Stream Stream { get; set; }

        /// <summary>主对象</summary>
        Stack<Object> Hosts { get; }

        /// <summary>成员</summary>
        MemberInfo Member { get; set; }

        /// <summary>字符串编码，默认utf-8</summary>
        Encoding Encoding { get; set; }

        /// <summary>序列化属性而不是字段。默认true</summary>
        Boolean UseProperty { get; set; }

        /// <summary>用户对象。存放序列化过程中使用的用户自定义对象</summary>
        Object UserState { get; set; }
        #endregion

        #region 方法
        /// <summary>写入一个对象</summary>
        /// <param name="value">目标对象</param>
        /// <param name="type">类型</param>
        /// <returns></returns>
        Boolean Write(Object value, Type type = null);

        /// <summary>读取指定类型对象</summary>
        /// <param name="type"></param>
        /// <returns></returns>
        Object Read(Type type);

        /// <summary>读取指定类型对象</summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T Read<T>();

        /// <summary>尝试读取指定类型对象</summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        Boolean TryRead(Type type, ref Object value);
        #endregion

        #region 调试日志
        /// <summary>日志提供者</summary>
        ILog Log { get; set; }
        #endregion
    }

    /// <summary>序列化处理器接口</summary>
    /// <typeparam name="THost"></typeparam>
    public interface IHandler<THost> where THost : IFormatterX
    {
        /// <summary>宿主读写器</summary>
        THost Host { get; set; }

        /// <summary>优先级</summary>
        Int32 Priority { get; set; }

        /// <summary>写入一个对象</summary>
        /// <param name="value">目标对象</param>
        /// <param name="type">类型</param>
        /// <returns></returns>
        Boolean Write(Object value, Type type);

        /// <summary>尝试读取指定类型对象</summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        Boolean TryRead(Type type, ref Object value);
    }

    /// <summary>序列化接口</summary>
    public abstract class FormatterBase //: IFormatterX
    {
        #region 属性
        /// <summary>数据流。默认实例化一个内存数据流</summary>
        public virtual Stream Stream { get; set; } = new MemoryStream();

        /// <summary>主对象</summary>
        public Stack<Object> Hosts { get; private set; } = new Stack<Object>();

        /// <summary>成员</summary>
        public MemberInfo Member { get; set; }

        /// <summary>字符串编码，默认utf-8</summary>
        public Encoding Encoding { get; set; } = Encoding.UTF8;

        /// <summary>序列化属性而不是字段。默认true</summary>
        public Boolean UseProperty { get; set; } = true;

        /// <summary>用户对象。存放序列化过程中使用的用户自定义对象</summary>
        public Object UserState { get; set; }
        #endregion

        #region 方法
        /// <summary>获取流里面的数据</summary>
        /// <returns></returns>
        public Byte[] GetBytes()
        {
            var ms = Stream;
            var pos = ms.Position;
            var start = 0;
            if (pos == 0 || pos == start) return new Byte[0];

            if (ms is MemoryStream ms2 && pos == ms.Length && start == 0)
                return ms2.ToArray();

            ms.Position = start;

            var buf = new Byte[pos - start];
            ms.Read(buf, 0, buf.Length);
            return buf;
        }

        /// <summary>获取流里面的数据包</summary>
        /// <returns></returns>
        public Packet GetPacket()
        {
            Stream.Position = 0;
            return new Packet(Stream);
        }
        #endregion

        #region 跟踪日志
        /// <summary>日志提供者</summary>
        public ILog Log { get; set; } = Logger.Null;

        /// <summary>输出日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public virtual void WriteLog(String format, params Object[] args) => Log?.Info(format, args);
        #endregion
    }

    /// <summary>读写处理器基类</summary>
    public abstract class HandlerBase<THost, THandler> : IHandler<THost>
        where THost : IFormatterX
        where THandler : IHandler<THost>
    {
        /// <summary>宿主读写器</summary>
        public THost Host { get; set; }

        /// <summary>优先级</summary>
        public Int32 Priority { get; set; }

        /// <summary>写入一个对象</summary>
        /// <param name="value">目标对象</param>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public abstract Boolean Write(Object value, Type type);

        /// <summary>尝试读取指定类型对象</summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public abstract Boolean TryRead(Type type, ref Object value);

        /// <summary>输出日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLog(String format, params Object[] args) => Host.Log.Info(format, args);
    }
    #endregion

    #region BinaryGeneral
    /// <summary>二进制基础类型处理器</summary>
    public class BinaryGeneral : BinaryHandlerBase
    {
        private static readonly DateTime _dt1970 = new DateTime(1970, 1, 1);

        /// <summary>实例化</summary>
        public BinaryGeneral() => Priority = 10;

        /// <summary>写入一个对象</summary>
        /// <param name="value">目标对象</param>
        /// <param name="type">类型</param>
        /// <returns>是否处理成功</returns>
        public override Boolean Write(Object value, Type type)
        {
            if (value == null && type != typeof(String)) return false;

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    Host.Write((Byte)((Boolean)value ? 1 : 0));
                    return true;
                case TypeCode.Byte:
                case TypeCode.SByte:
                    Host.Write(Convert.ToByte(value));
                    return true;
                case TypeCode.Char:
                    Write((Char)value);
                    return true;
                case TypeCode.DBNull:
                case TypeCode.Empty:
                    Host.Write(0);
                    return true;
                case TypeCode.DateTime:
                    var n = ((DateTime)value - _dt1970).TotalSeconds;
                    Write((UInt32)n);
                    return true;
                case TypeCode.Decimal:
                    Write((Decimal)value);
                    return true;
                case TypeCode.Double:
                    Write((Double)value);
                    return true;
                case TypeCode.Int16:
                    Write((Int16)value);
                    return true;
                case TypeCode.Int32:
                    Write((Int32)value);
                    return true;
                case TypeCode.Int64:
                    Write((Int64)value);
                    return true;
                case TypeCode.Object:
                    break;
                case TypeCode.Single:
                    Write((Single)value);
                    return true;
                case TypeCode.String:
                    Write((String)value);
                    return true;
                case TypeCode.UInt16:
                    Write((UInt16)value);
                    return true;
                case TypeCode.UInt32:
                    Write((UInt32)value);
                    return true;
                case TypeCode.UInt64:
                    Write((UInt64)value);
                    return true;
                default:
                    break;
            }

            return false;
        }

        /// <summary>尝试读取指定类型对象</summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override Boolean TryRead(Type type, ref Object value)
        {
            if (type == null)
            {
                if (value == null) return false;
                type = value.GetType();
            }

            var code = Type.GetTypeCode(type);
            switch (code)
            {
                case TypeCode.Boolean:
                    value = Host.ReadByte() > 0;
                    return true;
                case TypeCode.Byte:
                case TypeCode.SByte:
                    value = Host.ReadByte();
                    return true;
                case TypeCode.Char:
                    value = ReadChar();
                    return true;
                case TypeCode.DBNull:
                    value = DBNull.Value;
                    return true;
                case TypeCode.DateTime:
                    value = _dt1970.AddSeconds(ReadUInt32());
                    return true;
                case TypeCode.Decimal:
                    value = ReadDecimal();
                    return true;
                case TypeCode.Double:
                    value = ReadDouble();
                    return true;
                case TypeCode.Empty:
                    value = null;
                    return true;
                case TypeCode.Int16:
                    value = ReadInt16();
                    return true;
                case TypeCode.Int32:
                    value = ReadInt32();
                    return true;
                case TypeCode.Int64:
                    value = ReadInt64();
                    return true;
                case TypeCode.Object:
                    break;
                case TypeCode.Single:
                    value = ReadSingle();
                    return true;
                case TypeCode.String:
                    value = ReadString();
                    return true;
                case TypeCode.UInt16:
                    value = ReadUInt16();
                    return true;
                case TypeCode.UInt32:
                    value = ReadUInt32();
                    return true;
                case TypeCode.UInt64:
                    value = ReadUInt64();
                    return true;
                default:
                    break;
            }

            return false;
        }

        #region 基元类型写入
        #region 字节
        /// <summary>将一个无符号字节写入</summary>
        /// <param name="value">要写入的无符号字节。</param>
        public virtual void Write(Byte value) => Host.Write(value);

        /// <summary>将字节数组写入，如果设置了UseSize，则先写入数组长度。</summary>
        /// <param name="buffer">包含要写入的数据的字节数组。</param>
        public virtual void Write(Byte[] buffer)
        {
            // 可能因为FieldSize设定需要补充0字节
            if (buffer == null || buffer.Length == 0)
            {
                var size = Host.WriteSize(0);
                if (size > 0) Host.Write(new Byte[size], 0, -1);
            }
            else
            {
                var size = Host.WriteSize(buffer.Length);
                if (size > 0)
                {
                    // 写入数据，超长截断，不足补0
                    if (buffer.Length >= size)
                        Host.Write(buffer, 0, size);
                    else
                    {
                        Host.Write(buffer, 0, buffer.Length);
                        Host.Write(new Byte[size - buffer.Length], 0, -1);
                    }
                }
                else
                {
                    // 非FieldSize写入
                    Host.Write(buffer, 0, buffer.Length);
                }
            }
        }

        /// <summary>将字节数组部分写入当前流，不写入数组长度。</summary>
        /// <param name="buffer">包含要写入的数据的字节数组。</param>
        /// <param name="offset">buffer 中开始写入的起始点。</param>
        /// <param name="count">要写入的字节数。</param>
        public virtual void Write(Byte[] buffer, Int32 offset, Int32 count)
        {
            if (buffer == null || buffer.Length <= 0 || count <= 0 || offset >= buffer.Length) return;

            Host.Write(buffer, offset, count);
        }

        /// <summary>写入字节数组，自动计算长度</summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="count">数量</param>
        private void Write(Byte[] buffer, Int32 count)
        {
            if (buffer == null) return;

            if (count < 0 || count > buffer.Length) count = buffer.Length;

            Write(buffer, 0, count);
        }
        #endregion

        #region 有符号整数
        /// <summary>将 2 字节有符号整数写入当前流，并将流的位置提升 2 个字节。</summary>
        /// <param name="value">要写入的 2 字节有符号整数。</param>
        public virtual void Write(Int16 value)
        {
            if (Host.EncodeInt)
                WriteEncoded(value);
            else
                WriteIntBytes(BitConverter.GetBytes(value));
        }

        /// <summary>将 4 字节有符号整数写入当前流，并将流的位置提升 4 个字节。</summary>
        /// <param name="value">要写入的 4 字节有符号整数。</param>
        public virtual void Write(Int32 value)
        {
            if (Host.EncodeInt)
                WriteEncoded(value);
            else
                WriteIntBytes(BitConverter.GetBytes(value));
        }

        /// <summary>将 8 字节有符号整数写入当前流，并将流的位置提升 8 个字节。</summary>
        /// <param name="value">要写入的 8 字节有符号整数。</param>
        public virtual void Write(Int64 value)
        {
            if (Host.EncodeInt)
                WriteEncoded(value);
            else
                WriteIntBytes(BitConverter.GetBytes(value));
        }

        /// <summary>判断字节顺序</summary>
        /// <param name="buffer">缓冲区</param>
        private void WriteIntBytes(Byte[] buffer)
        {
            if (buffer == null || buffer.Length <= 0) return;

            // 如果不是小端字节顺序，则倒序
            if (!Host.IsLittleEndian) Array.Reverse(buffer);

            Write(buffer, 0, buffer.Length);
        }
        #endregion

        #region 无符号整数
        /// <summary>将 2 字节无符号整数写入当前流，并将流的位置提升 2 个字节。</summary>
        /// <param name="value">要写入的 2 字节无符号整数。</param>
        //[CLSCompliant(false)]
        public virtual void Write(UInt16 value) => Write((Int16)value);

        /// <summary>将 4 字节无符号整数写入当前流，并将流的位置提升 4 个字节。</summary>
        /// <param name="value">要写入的 4 字节无符号整数。</param>
        //[CLSCompliant(false)]
        public virtual void Write(UInt32 value) => Write((Int32)value);

        /// <summary>将 8 字节无符号整数写入当前流，并将流的位置提升 8 个字节。</summary>
        /// <param name="value">要写入的 8 字节无符号整数。</param>
        //[CLSCompliant(false)]
        public virtual void Write(UInt64 value) => Write((Int64)value);
        #endregion

        #region 浮点数
        /// <summary>将 4 字节浮点值写入当前流，并将流的位置提升 4 个字节。</summary>
        /// <param name="value">要写入的 4 字节浮点值。</param>
        public virtual void Write(Single value) => Write(BitConverter.GetBytes(value), -1);

        /// <summary>将 8 字节浮点值写入当前流，并将流的位置提升 8 个字节。</summary>
        /// <param name="value">要写入的 8 字节浮点值。</param>
        public virtual void Write(Double value) => Write(BitConverter.GetBytes(value), -1);

        /// <summary>将一个十进制值写入当前流，并将流位置提升十六个字节。</summary>
        /// <param name="value">要写入的十进制值。</param>
        protected virtual void Write(Decimal value)
        {
            var data = Decimal.GetBits(value);
            for (var i = 0; i < data.Length; i++)
            {
                Write(data[i]);
            }
        }
        #endregion

        #region 字符串
        /// <summary>将 Unicode 字符写入当前流，并根据所使用的 Encoding 和向流中写入的特定字符，提升流的当前位置。</summary>
        /// <param name="ch">要写入的非代理项 Unicode 字符。</param>
        public virtual void Write(Char ch) => Write(Convert.ToByte(ch));

        /// <summary>将字符数组部分写入当前流，并根据所使用的 Encoding（可能还根据向流中写入的特定字符），提升流的当前位置。</summary>
        /// <param name="chars">包含要写入的数据的字符数组。</param>
        /// <param name="index">chars 中开始写入的起始点。</param>
        /// <param name="count">要写入的字符数。</param>
        public virtual void Write(Char[] chars, Int32 index, Int32 count)
        {
            if (chars == null)
            {
                //Host.WriteSize(0);
                // 可能因为FieldSize设定需要补充0字节
                Write(new Byte[0]);
                return;
            }

            if (chars.Length <= 0 || count <= 0 || index >= chars.Length)
            {
                //Host.WriteSize(0);
                // 可能因为FieldSize设定需要补充0字节
                Write(new Byte[0]);
                return;
            }

            // 先用写入字节长度
            var buffer = Host.Encoding.GetBytes(chars, index, count);
            Write(buffer);
        }

        /// <summary>写入字符串</summary>
        /// <param name="value">要写入的值。</param>
        public virtual void Write(String value)
        {
            if (value == null || value.Length == 0)
            {
                //Host.WriteSize(0);
                Write(new Byte[0]);
                return;
            }

            // 先用写入字节长度
            var buffer = Host.Encoding.GetBytes(value);
            Write(buffer);
        }
        #endregion
        #endregion

        #region 基元类型读取
        #region 字节
        /// <summary>从当前流中读取下一个字节，并使流的当前位置提升 1 个字节。</summary>
        /// <returns></returns>
        public virtual Byte ReadByte() => Host.ReadByte();

        /// <summary>从当前流中将 count 个字节读入字节数组，如果count小于0，则先读取字节数组长度。</summary>
        /// <param name="count">要读取的字节数。</param>
        /// <returns></returns>
        public virtual Byte[] ReadBytes(Int32 count)
        {
            if (count < 0) count = Host.ReadSize();

            if (count <= 0) return null;

            var max = IOHelper.MaxSafeArraySize;
            if (count > max) throw new XException("安全需要，不允许读取超大变长数组 {0:n0}>{1:n0}", count, max);

            var buffer = Host.ReadBytes(count);

            return buffer;
        }
        #endregion

        #region 有符号整数
        /// <summary>读取整数的字节数组，某些写入器（如二进制写入器）可能需要改变字节顺序</summary>
        /// <param name="count">数量</param>
        /// <returns></returns>
        protected virtual Byte[] ReadIntBytes(Int32 count)
        {
            var buffer = ReadBytes(count);

            // 如果不是小端字节顺序，则倒序
            if (!Host.IsLittleEndian) Array.Reverse(buffer);

            return buffer;
        }

        /// <summary>从当前流中读取 2 字节有符号整数，并使流的当前位置提升 2 个字节。</summary>
        /// <returns></returns>
        public virtual Int16 ReadInt16()
        {
            if (Host.EncodeInt)
                return ReadEncodedInt16();
            else
                return BitConverter.ToInt16(ReadIntBytes(2), 0);
        }

        /// <summary>从当前流中读取 4 字节有符号整数，并使流的当前位置提升 4 个字节。</summary>
        /// <returns></returns>
        public virtual Int32 ReadInt32()
        {
            if (Host.EncodeInt)
                return ReadEncodedInt32();
            else
                return BitConverter.ToInt32(ReadIntBytes(4), 0);
        }

        /// <summary>从当前流中读取 8 字节有符号整数，并使流的当前位置向前移动 8 个字节。</summary>
        /// <returns></returns>
        public virtual Int64 ReadInt64()
        {
            if (Host.EncodeInt)
                return ReadEncodedInt64();
            else
                return BitConverter.ToInt64(ReadIntBytes(8), 0);
        }
        #endregion

        #region 无符号整数
        /// <summary>使用 Little-Endian 编码从当前流中读取 2 字节无符号整数，并将流的位置提升 2 个字节。</summary>
        /// <returns></returns>
        //[CLSCompliant(false)]
        public virtual UInt16 ReadUInt16() => (UInt16)ReadInt16();

        /// <summary>从当前流中读取 4 字节无符号整数并使流的当前位置提升 4 个字节。</summary>
        /// <returns></returns>
        //[CLSCompliant(false)]
        public virtual UInt32 ReadUInt32() => (UInt32)ReadInt32();

        /// <summary>从当前流中读取 8 字节无符号整数并使流的当前位置提升 8 个字节。</summary>
        /// <returns></returns>
        //[CLSCompliant(false)]
        public virtual UInt64 ReadUInt64() => (UInt64)ReadInt64();
        #endregion

        #region 浮点数
        /// <summary>从当前流中读取 4 字节浮点值，并使流的当前位置提升 4 个字节。</summary>
        /// <returns></returns>
        public virtual Single ReadSingle() => BitConverter.ToSingle(ReadBytes(4), 0);

        /// <summary>从当前流中读取 8 字节浮点值，并使流的当前位置提升 8 个字节。</summary>
        /// <returns></returns>
        public virtual Double ReadDouble() => BitConverter.ToDouble(ReadBytes(8), 0);
        #endregion

        #region 字符串
        /// <summary>从当前流中读取下一个字符，并根据所使用的 Encoding 和从流中读取的特定字符，提升流的当前位置。</summary>
        /// <returns></returns>
        public virtual Char ReadChar() => Convert.ToChar(ReadByte());

        /// <summary>从当前流中读取一个字符串。字符串有长度前缀，一次 7 位地被编码为整数。</summary>
        /// <returns></returns>
        public virtual String ReadString()
        {
            // 先读长度
            var n = Host.ReadSize();
            //if (n > 1000) n = Host.ReadSize();
            if (n <= 0) return null;
            //if (n == 0) return String.Empty;

            var buffer = ReadBytes(n);
            var enc = Host.Encoding ?? Encoding.UTF8;

            var str = enc.GetString(buffer);
            if ((Host as Binary).TrimZero && str != null) str = str.Trim('\0');

            return str;
        }
        #endregion

        #region 其它
        /// <summary>从当前流中读取十进制数值，并将该流的当前位置提升十六个字节。</summary>
        /// <returns></returns>
        public virtual Decimal ReadDecimal()
        {
            var data = new Int32[4];
            for (var i = 0; i < data.Length; i++)
            {
                data[i] = ReadInt32();
            }
            return new Decimal(data);
        }
        #endregion

        #region 7位压缩编码整数
        /// <summary>以压缩格式读取16位整数</summary>
        /// <returns></returns>
        public Int16 ReadEncodedInt16()
        {
            Byte b;
            Int16 rs = 0;
            Byte n = 0;
            while (true)
            {
                b = ReadByte();
                // 必须转为Int16，否则可能溢出
                rs += (Int16)((b & 0x7f) << n);
                if ((b & 0x80) == 0) break;

                n += 7;
                if (n >= 16) throw new FormatException("数字值过大，无法使用压缩格式读取！");
            }
            return rs;
        }

        /// <summary>以压缩格式读取32位整数</summary>
        /// <returns></returns>
        public Int32 ReadEncodedInt32()
        {
            Byte b;
            var rs = 0;
            Byte n = 0;
            while (true)
            {
                b = ReadByte();
                // 必须转为Int32，否则可能溢出
                rs += (b & 0x7f) << n;
                if ((b & 0x80) == 0) break;

                n += 7;
                if (n >= 32) throw new FormatException("数字值过大，无法使用压缩格式读取！");
            }
            return rs;
        }

        /// <summary>以压缩格式读取64位整数</summary>
        /// <returns></returns>
        public Int64 ReadEncodedInt64()
        {
            Byte b;
            Int64 rs = 0;
            Byte n = 0;
            while (true)
            {
                b = ReadByte();
                // 必须转为Int64，否则可能溢出
                rs += (Int64)(b & 0x7f) << n;
                if ((b & 0x80) == 0) break;

                n += 7;
                if (n >= 64) throw new FormatException("数字值过大，无法使用压缩格式读取！");
            }
            return rs;
        }
        #endregion
        #endregion

        #region 7位压缩编码整数
        [ThreadStatic]
        private static Byte[] _encodes;
        /// <summary>
        /// 以7位压缩格式写入16位整数，小于7位用1个字节，小于14位用2个字节。
        /// 由每次写入的一个字节的第一位标记后面的字节是否还是当前数据，所以每个字节实际可利用存储空间只有后7位。
        /// </summary>
        /// <param name="value">数值</param>
        /// <returns>实际写入字节数</returns>
        public Int32 WriteEncoded(Int16 value)
        {
            if (_encodes == null) _encodes = new Byte[16];

            var count = 0;
            var num = (UInt16)value;
            while (num >= 0x80)
            {
                _encodes[count++] = (Byte)(num | 0x80);
                num = (UInt16)(num >> 7);
            }
            _encodes[count++] = (Byte)num;

            Write(_encodes, 0, count);

            return count;
        }

        /// <summary>
        /// 以7位压缩格式写入32位整数，小于7位用1个字节，小于14位用2个字节。
        /// 由每次写入的一个字节的第一位标记后面的字节是否还是当前数据，所以每个字节实际可利用存储空间只有后7位。
        /// </summary>
        /// <param name="value">数值</param>
        /// <returns>实际写入字节数</returns>
        public Int32 WriteEncoded(Int32 value)
        {
            if (_encodes == null) _encodes = new Byte[16];

            var count = 0;
            var num = (UInt32)value;
            while (num >= 0x80)
            {
                _encodes[count++] = (Byte)(num | 0x80);
                num >>= 7;
            }
            _encodes[count++] = (Byte)num;

            Write(_encodes, 0, count);

            return count;
        }

        /// <summary>
        /// 以7位压缩格式写入64位整数，小于7位用1个字节，小于14位用2个字节。
        /// 由每次写入的一个字节的第一位标记后面的字节是否还是当前数据，所以每个字节实际可利用存储空间只有后7位。
        /// </summary>
        /// <param name="value">数值</param>
        /// <returns>实际写入字节数</returns>
        public Int32 WriteEncoded(Int64 value)
        {
            if (_encodes == null) _encodes = new Byte[16];

            var count = 0;
            var num = (UInt64)value;
            while (num >= 0x80)
            {
                _encodes[count++] = (Byte)(num | 0x80);
                num >>= 7;
            }
            _encodes[count++] = (Byte)num;

            Write(_encodes, 0, count);

            return count;
        }
        #endregion
    }
    #endregion
    #region BinaryNormal
    /// <summary>常用类型编码</summary>
    public class BinaryNormal : BinaryHandlerBase
    {
        /// <summary>初始化</summary>
        public BinaryNormal() => Priority = 12;

        /// <summary>写入</summary>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public override Boolean Write(Object value, Type type)
        {
            if (type == typeof(Guid))
            {
                if (!(value is  Guid guid)) guid = Guid.Empty;
                Write(guid.ToByteArray(), -1);
                return true;
            }
            else if (type == typeof(Byte[]))
            {
                //Write((Byte[])value);
                var bn = Host as Binary;
                var bc = bn.GetHandler<BinaryGeneral>();
                bc.Write((Byte[])value);

                return true;
            }
            else if (type == typeof(Packet))
            {
                var bn = Host as Binary;
                if (value is Packet pk)
                {
                    Host.WriteSize(pk.Total);
                    pk.CopyTo(Host.Stream);
                }
                else
                {
                    Host.WriteSize(0);
                }

                return true;
            }
            else if (type == typeof(Char[]))
            {
                //Write((Char[])value);
                var bn = Host as Binary;
                var bc = bn.GetHandler<BinaryGeneral>();
                bc.Write((Char[])value, 0, -1);

                return true;
            }
            else if (type == typeof(IPAddress))
            {
                Host.Write(((IPAddress)value).GetAddressBytes());
                return true;
            }
            else if (type == typeof(IPEndPoint))
            {
                var ep = value as IPEndPoint;
                Host.Write(ep.Address.GetAddressBytes());
                Host.Write((UInt16)ep.Port);
                return true;
            }

            return false;
        }

        /// <summary>写入字节数组，自动计算长度</summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="count">数量</param>
        private void Write(Byte[] buffer, Int32 count)
        {
            if (buffer == null) return;

            if (count < 0 || count > buffer.Length) count = buffer.Length;

            Host.Write(buffer, 0, count);
        }

        /// <summary>读取</summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override Boolean TryRead(Type type, ref Object value)
        {
            if (type == typeof(Guid))
            {
                value = new Guid(ReadBytes(16));
                return true;
            }
            else if (type == typeof(Byte[]))
            {
                value = ReadBytes(-1);
                return true;
            }
            else if (type == typeof(Packet))
            {
                var buf = ReadBytes(-1);
                value = new Packet(buf);
                return true;
            }
            else if (type == typeof(Char[]))
            {
                value = ReadChars(-1);
                return true;
            }
            else if (type == typeof(IPAddress))
            {
                value = new IPAddress(ReadBytes(-1));
                return true;
            }
            else if (type == typeof(IPEndPoint))
            {
                var ip = new IPAddress(ReadBytes(-1));
                var port = Host.Read<UInt16>();
                value = new IPEndPoint(ip, port);
                return true;
            }

            return false;
        }

        /// <summary>从当前流中将 count 个字节读入字节数组，如果count小于0，则先读取字节数组长度。</summary>
        /// <param name="count">要读取的字节数。</param>
        /// <returns></returns>
        protected virtual Byte[] ReadBytes(Int32 count)
        {
            var bn = Host as Binary;
            var bc = bn.GetHandler<BinaryGeneral>();

            return bc.ReadBytes(count);
        }

        /// <summary>从当前流中读取 count 个字符，以字符数组的形式返回数据，并根据所使用的 Encoding 和从流中读取的特定字符，提升当前位置。</summary>
        /// <param name="count">要读取的字符数。</param>
        /// <returns></returns>
        public virtual Char[] ReadChars(Int32 count)
        {
            if (count < 0) count = Host.ReadSize();

            // 首先按最小值读取
            var data = ReadBytes(count);

            return Host.Encoding.GetChars(data);
        }
    }
    #endregion
    #region BinaryComposite
    /// <summary>序列化访问上下文</summary>
    public class AccessorContext
    {
        /// <summary>宿主</summary>
        public IFormatterX Host { get; set; }

        /// <summary>对象类型</summary>
        public Type Type { get; set; }

        /// <summary>目标对象</summary>
        public Object Value { get; set; }

        /// <summary>成员</summary>
        public MemberInfo Member { get; set; }

        /// <summary>用户对象。存放序列化过程中使用的用户自定义对象</summary>
        public Object UserState { get; set; }
    }

    /// <summary>字段大小特性。</summary>
    /// <remarks>
    /// 可以通过Size指定字符串或数组的固有大小，为0表示自动计算；
    /// 也可以通过指定参考字段ReferenceName，然后从其中获取大小。
    /// 支持_Header._Questions形式的多层次引用字段。
    /// 
    /// 支持针对单个成员使用多个FieldSize特性，各自指定不同Version版本，以支持不同版本协议的序列化。
    /// 例如JT/T808协议，2011/2019版的相同字段使用不同长度。
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Enum, AllowMultiple = true)]
    public class FieldSizeAttribute : Attribute
    {
        /// <summary>大小。使用<see cref="ReferenceName"/>时，作为偏移量；0表示自动计算大小</summary>
        public Int32 Size { get; set; }

        /// <summary>大小宽度。特定个数的字节表示长度，自动计算时（Size=0）使用，可选0/1/2/4</summary>
        public Int32 SizeWidth { get; set; } = -1;

        /// <summary>参考大小字段名，其中存储了实际大小，使用时获取</summary>
        public String ReferenceName { get; set; }

        /// <summary>协议版本。用于支持多版本协议序列化。例如JT/T808的2011/2019</summary>
        public String Version { get; set; }

        /// <summary>通过Size指定字符串或数组的固有大小，为0表示自动计算</summary>
        /// <param name="size"></param>
        public FieldSizeAttribute(Int32 size) => Size = size;

        /// <summary>指定参考字段ReferenceName，然后从其中获取大小</summary>
        /// <param name="referenceName"></param>
        public FieldSizeAttribute(String referenceName) => ReferenceName = referenceName;

        /// <summary>指定参考字段ReferenceName，然后从其中获取大小</summary>
        /// <param name="referenceName"></param>
        /// <param name="size">在参考字段值基础上的增量，可以是正数负数</param>
        public FieldSizeAttribute(String referenceName, Int32 size) { ReferenceName = referenceName; Size = size; }

        /// <summary>指定大小，指定协议版本，用于支持多版本协议序列化</summary>
        /// <param name="size"></param>
        /// <param name="version"></param>
        public FieldSizeAttribute(Int32 size, String version)
        {
            Size = size;
            Version = version;
        }

        #region 方法
        /// <summary>找到所引用的参考字段</summary>
        /// <param name="target">目标对象</param>
        /// <param name="member">目标对象的成员</param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        private MemberInfo FindReference(Object target, MemberInfo member, out Object value)
        {
            value = null;

            if (member == null) return null;
            if (String.IsNullOrEmpty(ReferenceName)) return null;

            // 考虑ReferenceName可能是圆点分隔的多重结构
            MemberInfo mi = null;
            var type = member.DeclaringType;
            value = target;
            var ss = ReferenceName.Split('.');
            for (var i = 0; i < ss.Length; i++)
            {
                var pi = type.GetPropertyEx(ss[i]);
                if (pi != null)
                {
                    mi = pi;
                    type = pi.PropertyType;
                }
                else
                {
                    var fi = type.GetFieldEx(ss[i]);
                    if (fi != null)
                    {
                        mi = fi;
                        type = fi.FieldType;
                    }
                }

                // 最后一个不需要计算
                if (i < ss.Length - 1)
                {
                    if (mi != null) value = value.GetValue(mi);
                }
            }

            // 目标字段必须是整型
            var tc = Type.GetTypeCode(type);
            if (tc  >= TypeCode.SByte && tc <= TypeCode.UInt64) return mi;

            return null;
        }

        /// <summary>设置目标对象的引用大小值</summary>
        /// <param name="target">目标对象</param>
        /// <param name="member"></param>
        /// <param name="encoding"></param>
        internal void SetReferenceSize(Object target, MemberInfo member, Encoding encoding)
        {
            var mi = FindReference(target, member, out var v);
            if (mi == null) return;

            // 获取当前成员（加了特性）的值
            var value = target.GetValue(member);
            if (value == null) return;

            // 尝试计算大小
            var size = 0;
            if (value is String)
            {
                encoding ??= Encoding.UTF8;

                size = encoding.GetByteCount("" + value);
            }
            else if (value.GetType().IsArray)
            {
                size = (value as Array).Length;
            }
            else if (value is System.Collections.IEnumerable)
            {
                foreach (var item in value as System.Collections.IEnumerable)
                {
                    size++;
                }
            }

            // 给参考字段赋值
            v.SetValue(mi, size - Size);
        }

        /// <summary>获取目标对象的引用大小值</summary>
        /// <param name="target">目标对象</param>
        /// <param name="member"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        internal Boolean TryGetReferenceSize(Object target, MemberInfo member, out Int32 size)
        {
            size = -1;

            var mi = FindReference(target, member, out var v);
            if (mi == null) return false;

            size = Convert.ToInt32(v.GetValue(mi)) + Size;

            return true;
        }
        #endregion
    }

    #region IMemberAccessor
    /// <summary>成员序列化访问器。接口实现者可以在这里完全自定义序列化行为</summary>
    public interface IMemberAccessor
    {
        /// <summary>从数据流中读取消息</summary>
        /// <param name="formatter">序列化</param>
        /// <param name="context">上下文</param>
        /// <returns>是否成功</returns>
        Boolean Read(IFormatterX formatter, AccessorContext context);

        /// <summary>把消息写入到数据流中</summary>
        /// <param name="formatter">序列化</param>
        /// <param name="context">上下文</param>
        Boolean Write(IFormatterX formatter, AccessorContext context);
    }
    /// <summary>成员访问特性。使用自定义逻辑序列化成员</summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public abstract class AccessorAttribute : Attribute, IMemberAccessor
    {
        /// <summary>从数据流中读取消息</summary>
        /// <param name="formatter">序列化</param>
        /// <param name="context">上下文</param>
        /// <returns>是否成功</returns>
        public virtual Boolean Read(IFormatterX formatter, AccessorContext context) => false;

        /// <summary>把消息写入到数据流中</summary>
        /// <param name="formatter">序列化</param>
        /// <param name="context">上下文</param>
        public virtual Boolean Write(IFormatterX formatter, AccessorContext context) => false;
    }
    /// <summary>定长字符串序列化特性</summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class FixedStringAttribute : AccessorAttribute
    {
        /// <summary>长度</summary>
        public Int32 Length { get; set; }

        /// <summary>定长字符串序列化</summary>
        /// <param name="length"></param>
        public FixedStringAttribute(Int32 length) => Length = length;

        /// <summary>从数据流中读取消息</summary>
        /// <param name="formatter">序列化</param>
        /// <param name="context">上下文</param>
        /// <returns>是否成功</returns>
        public override Boolean Read(IFormatterX formatter, AccessorContext context)
        {
            if (formatter is Binary bn)
            {
                var str = bn.ReadFixedString(Length);
                context.Value.SetValue(context.Member, str);

                return true;
            }

            return false;
        }

        /// <summary>把消息写入到数据流中</summary>
        /// <param name="formatter">序列化</param>
        /// <param name="context">上下文</param>
        public override Boolean Write(IFormatterX formatter, AccessorContext context)
        {
            if (formatter is Binary bn)
            {
                var str = context.Value.GetValue(context.Member) as String;
                bn.WriteFixedString(str, Length);

                return true;
            }

            return false;
        }

    }
    /// <summary>完全字符串序列化特性。指示数据流剩下部分全部作为字符串读写</summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class FullStringAttribute : AccessorAttribute
    {
        /// <summary>从数据流中读取消息</summary>
        /// <param name="formatter">序列化</param>
        /// <param name="context">上下文</param>
        /// <returns>是否成功</returns>
        public override Boolean Read(IFormatterX formatter, AccessorContext context)
        {
            if (formatter is Binary bn)
            {
                var buf = bn.Stream.ReadBytes(-1);
                var str = bn.Encoding.GetString(buf);
                if (bn.TrimZero && str != null) str = str.Trim('\0');

                context.Value.SetValue(context.Member, str);

                return true;
            }

            return false;
        }

        /// <summary>把消息写入到数据流中</summary>
        /// <param name="formatter">序列化</param>
        /// <param name="context">上下文</param>
        public override Boolean Write(IFormatterX formatter, AccessorContext context)
        {
            if (formatter is Binary bn)
            {
                var str = context.Value.GetValue(context.Member) as String;
                if (!str.IsNullOrEmpty())
                {
                    var buf = bn.Encoding.GetBytes(str);
                    bn.Write(buf, 0, buf.Length);
                }

                return true;
            }

            return false;
        }

    }
    #endregion

    /// <summary>复合对象处理器</summary>
    public class BinaryComposite : BinaryHandlerBase
    {
        /// <summary>实例化</summary>
        public BinaryComposite() => Priority = 100;

        /// <summary>写入对象</summary>
        /// <param name="value">目标对象</param>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public override Boolean Write(Object value, Type type)
        {
            // 不支持基本类型
            if (Type.GetTypeCode(type) != TypeCode.Object) return false;

            var ims = Host.IgnoreMembers;

            var ms = GetMembers(type).Where(e => !ims.Contains(e.Name)).ToList();
            WriteLog("BinaryWrite类{0} 共有成员{1}个", type.Name, ms.Count);

            if (Host is Binary b && b.UseFieldSize)
            {
                // 遍历成员，寻找FieldSizeAttribute特性，重新设定大小字段的值
                foreach (var member in ms)
                {
                    // 获取FieldSizeAttribute特性
                    var atts = member.GetCustomAttributes<FieldSizeAttribute>();
                    if (atts != null)
                    {
                        foreach (var att in atts)
                        {
                            if (!att.ReferenceName.IsNullOrEmpty() &&
                                (att.Version.IsNullOrEmpty() || att.Version == (Host as Binary).Version))
                                att.SetReferenceSize(value, member, Host.Encoding);
                        }
                    }
                }
            }

            // 如果不是第一层，这里开始必须写对象引用
            if (WriteRef(value)) return true;

            Host.Hosts.Push(value);

            var context = new AccessorContext
            {
                Host = Host,
                Type = type,
                Value = value,
                UserState = Host.UserState
            };

            // 获取成员
            foreach (var member in ms)
            {
                var mtype = GetMemberType(member);
                context.Member = Host.Member = member;

                var v = value is IModel src ? src[member.Name] : value.GetValue(member);
                WriteLog("    {0}.{1} {2}", type.Name, member.Name, v);

                // 成员访问器优先
                if (value is IMemberAccessor ac && ac.Write(Host, context)) continue;
                if (TryGetAccessor(member, out var acc) && acc.Write(Host, context)) continue;

                if (!Host.Write(v, mtype))
                {
                    Host.Hosts.Pop();
                    return false;
                }
            }
            Host.Hosts.Pop();

            return true;
        }

        private Boolean WriteRef(Object value)
        {
            var bn = Host as Binary;
            if (!bn.UseRef) return false;
            if (Host.Hosts.Count == 0) return false;

            if (value == null)
            {
                Host.Write(0);
                return true;
            }

            // 找到对象索引，并写入
            var hs = Host.Hosts.ToArray();
            for (var i = 0; i < hs.Length; i++)
            {
                if (value == hs[i])
                {
                    Host.WriteSize(i + 1);
                    return true;
                }
            }

            // 埋下自己
            Host.WriteSize(Host.Hosts.Count + 1);

            return false;
        }

        /// <summary>尝试读取指定类型对象</summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override Boolean TryRead(Type type, ref Object value)
        {
            if (type == typeof(Object)) return false;
            if (type == null)
            {
                if (value == null) return false;
                type = value.GetType();
            }

            // 不支持基本类型
            if (Type.GetTypeCode(type) != TypeCode.Object) return false;

            // 不支持基类不是Object的特殊类型
            if (!type.As<Object>()) return false;

            var ims = Host.IgnoreMembers;

            var ms = GetMembers(type).Where(e => !ims.Contains(e.Name)).ToList();
            WriteLog("BinaryRead类{0} 共有成员{1}个", type.Name, ms.Count);

            // 读取对象引用
            if (ReadRef(ref value)) return true;

            value ??= type.CreateInstance();

            Host.Hosts.Push(value);

            var context = new AccessorContext
            {
                Host = Host,
                Type = type,
                Value = value,
                UserState = Host.UserState
            };

            // 获取成员
            for (var i = 0; i < ms.Count; i++)
            {
                var member = ms[i];

                var mtype = GetMemberType(member);
                context.Member = Host.Member = member;
                WriteLog("    {0}.{1}", member.DeclaringType.Name, member.Name);

                // 成员访问器优先
                if (value is IMemberAccessor ac && ac.Read(Host, context)) continue;
                if (TryGetAccessor(member, out var acc) && acc.Read(Host, context)) continue;

                // 数据流不足时，放弃读取目标成员，并认为整体成功
                var hs = Host.Stream;
                if (hs.CanSeek && hs.Position >= hs.Length) break;

                Object v = null;
                v = value is IModel src ? src[member.Name] : value.GetValue(member);
                if (!Host.TryRead(mtype, ref v))
                {
                    Host.Hosts.Pop();
                    return false;
                }

                if (value is IModel dst)
                    dst[member.Name] = v;
                else
                    value.SetValue(member, v);
            }
            Host.Hosts.Pop();

            return true;
        }

        private Boolean ReadRef(ref Object value)
        {
            var bn = Host as Binary;
            if (!bn.UseRef) return false;
            if (Host.Hosts.Count == 0) return false;

            var rf = bn.ReadEncodedInt32();
            if (rf == 0)
            {
                //value = null;
                return true;
            }

            // 找到对象索引
            var hs = Host.Hosts.ToArray();
            // 如果引用是对象数加一，说明有对象紧跟着
            if (rf == hs.Length + 1) return false;

            if (rf < 0 || rf > hs.Length) throw new XException("无法在 {0} 个对象中找到引用 {1}", hs.Length, rf);

            value = hs[rf - 1];

            return true;
        }

        #region 获取成员
        /// <summary>获取成员</summary>
        /// <param name="type"></param>
        /// <param name="baseFirst"></param>
        /// <returns></returns>
        protected virtual List<MemberInfo> GetMembers(Type type, Boolean baseFirst = true)
        {
            if (Host.UseProperty)
                return type.GetProperties(baseFirst).Cast<MemberInfo>().ToList();
            else
                return type.GetFields(baseFirst).Cast<MemberInfo>().ToList();
        }

        private static Type GetMemberType(MemberInfo member)
        {
            return member.MemberType switch
            {
                MemberTypes.Field => (member as FieldInfo).FieldType,
                MemberTypes.Property => (member as PropertyInfo).PropertyType,
                _ => throw new NotSupportedException(),
            };
        }

        private static readonly ConcurrentDictionary<MemberInfo, IMemberAccessor> _cache = new ConcurrentDictionary<MemberInfo, IMemberAccessor>();
        private static Boolean TryGetAccessor(MemberInfo member, out IMemberAccessor acc)
        {
            if (_cache.TryGetValue(member, out acc)) return acc != null;

            var atts = member.GetCustomAttributes();
            acc = atts.FirstOrDefault(e => e is IMemberAccessor) as IMemberAccessor;

            _cache[member] = acc;

            return acc != null;
        }
        #endregion
    }
    #endregion
    #region BinaryList
    /// <summary>列表数据编码</summary>
    public class BinaryList : BinaryHandlerBase
    {
        /// <summary>初始化</summary>
        public BinaryList() => Priority = 20;

        /// <summary>写入</summary>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public override Boolean Write(Object value, Type type)
        {
            if (!type.As<IList>() && !(value is  IList)) return false;

            // 先写入长度
            if (!(value is  IList list) || list.Count == 0)
            {
                Host.WriteSize(0);
                return true;
            }

            Host.WriteSize(list.Count);

            // 循环写入数据
            foreach (var item in list)
            {
                Host.Write(item);
            }

            return true;
        }

        /// <summary>读取</summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override Boolean TryRead(Type type, ref Object value)
        {
            if (!type.As<IList>() && !type.As(typeof(IList<>))) return false;

            // 先读取长度
            var count = Host.ReadSize();
            if (count == 0) return true;

            if (value == null && type != null)
            {
                // 数组的创建比较特别
                if (type.As<Array>())
                    value = Array.CreateInstance(type.GetElementTypeEx(), count);
                else
                    value = type.CreateInstance();
            }

            // 子元素类型
            var elmType = type.GetElementTypeEx();

            var list = value as IList;
            // 如果是数组，则需要先加起来，再
            //if (value is Array) list = typeof(IList<>).MakeGenericType(value.GetType().GetElementTypeEx()).CreateInstance() as IList;
            for (var i = 0; i < count; i++)
            {
                Object obj = null;
                if (!Host.TryRead(elmType, ref obj)) return false;

                if (value is Array)
                    list[i] = obj;
                else
                    list.Add(obj);
            }

            return true;
        }
    }
    #endregion
    #region BinaryDictionary
    /// <summary>字典数据编码</summary>
    public class BinaryDictionary : BinaryHandlerBase
    {
        /// <summary>初始化</summary>
        public BinaryDictionary() => Priority = 30;

        /// <summary>写入一个对象</summary>
        /// <param name="value">目标对象</param>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public override Boolean Write(Object value, Type type)
        {
            if (!(value is  IDictionary dic)) return false;

            // 先写入长度
            if (dic.Count == 0)
            {
                Host.WriteSize(0);
                return true;
            }

            Host.WriteSize(dic.Count);

            // 循环写入数据
            foreach (DictionaryEntry item in dic)
            {
                Host.Write(item.Key);
                Host.Write(item.Value);
            }

            return true;
        }

        /// <summary>尝试读取指定类型对象</summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override Boolean TryRead(Type type, ref Object value)
        {
            if (!type.As<IDictionary>() && !type.As(typeof(IDictionary<,>))) return false;

            // 子元素类型
            var gs = type.GetGenericArguments();
            if (gs.Length != 2) throw new NotSupportedException($"字典类型仅支持 {typeof(Dictionary<,>).FullName}");

            var keyType = gs[0];
            var valType = gs[1];

            // 先读取长度
            var count = Host.ReadSize();
            if (count == 0) return true;

            // 创建字典
            if (value == null && type != null)
            {
                value = type.CreateInstance();
            }

            var dic = value as IDictionary;

            for (var i = 0; i < count; i++)
            {
                Object key = null;
                Object val = null;
                if (!Host.TryRead(keyType, ref key)) return false;
                if (!Host.TryRead(valType, ref val)) return false;

                dic[key] = val;
            }

            return true;
        }
    }
    #endregion
#if __WIN__
#region BinaryColor
    /// <summary>颜色处理器。</summary>
    public class BinaryColor : BinaryHandlerBase
    {
        /// <summary>实例化</summary>
        public BinaryColor() => Priority = 50;

        /// <summary>写入对象</summary>
        /// <param name="value">目标对象</param>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public override Boolean Write(Object value, Type type)
        {
            if (type != typeof(Color)) return false;

            var color = (Color)value;
            WriteLog("WriteColor {0}", color);

            Host.Write(color.A);
            Host.Write(color.R);
            Host.Write(color.G);
            Host.Write(color.B);

            return true;
        }

        /// <summary>尝试读取指定类型对象</summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override Boolean TryRead(Type type, ref Object value)
        {
            if (type != typeof(Color)) return false;

            var a = Host.ReadByte();
            var r = Host.ReadByte();
            var g = Host.ReadByte();
            var b = Host.ReadByte();
            var color = Color.FromArgb(a, r, g, b);
            WriteLog("ReadColor {0}", color);
            value = color;

            return true;
        }
    }
#endregion
    #region BinaryFont
    /// <summary>字体处理器。</summary>
    public class BinaryFont : BinaryHandlerBase
    {
        /// <summary>实例化</summary>
        public BinaryFont() => Priority = 50;

        /// <summary>写入对象</summary>
        /// <param name="value">目标对象</param>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public override Boolean Write(Object value, Type type)
        {
            if (type != typeof(Font)) return false;

            // 写入引用
            if (value == null)
            {
                Host.WriteSize(0);
                return true;
            }
            Host.WriteSize(1);

            var font = value as Font;
            WriteLog("WriteFont {0}", font);

            Host.Write(font.Name);
            Host.Write(font.Size);
            Host.Write((Byte)font.Style);

            return true;
        }

        /// <summary>尝试读取指定类型对象</summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override Boolean TryRead(Type type, ref Object value)
        {
            if (type != typeof(Font)) return false;

            // 读引用
            var size = Host.ReadSize();
            if (size == 0) return true;

            if (size != 1) WriteLog("读取引用应该是1，而实际是{0}", size);

            var font = new Font(Host.Read<String>(), Host.Read<Single>(), (FontStyle)Host.ReadByte());
            value = font;
            WriteLog("ReadFont {0}", font);

            return true;
        }
    }
    #endregion
#endif

    #region Binary
    /// <summary>二进制序列化</summary>
    public class Binary : FormatterBase, IBinary
    {
        #region 属性
        /// <summary>使用7位编码整数。默认false不使用</summary>
        public Boolean EncodeInt { get; set; }

        /// <summary>小端字节序。默认false大端</summary>
        public Boolean IsLittleEndian { get; set; }

        /// <summary>使用指定大小的FieldSizeAttribute特性，默认false</summary>
        public Boolean UseFieldSize { get; set; }

        /// <summary>使用对象引用，默认false</summary>
        public Boolean UseRef { get; set; } = false;

        /// <summary>大小宽度。可选0/1/2/4，默认0表示压缩编码整数</summary>
        public Int32 SizeWidth { get; set; }

        /// <summary>解析字符串时，是否清空两头的0字节，默认false</summary>
        public Boolean TrimZero { get; set; }

        /// <summary>协议版本。用于支持多版本协议序列化，配合FieldSize特性使用。例如JT/T808的2011/2019</summary>
        public String Version { get; set; }

        /// <summary>要忽略的成员</summary>
        public ICollection<String> IgnoreMembers { get; set; } = new HashSet<String>();

        /// <summary>处理器列表</summary>
        public IList<IBinaryHandler> Handlers { get; private set; }
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public Binary()
        {
            // 遍历所有处理器实现
            var list = new List<IBinaryHandler>
        {
            new BinaryGeneral { Host = this },
            new BinaryNormal { Host = this },
            new BinaryComposite { Host = this },
            new BinaryList { Host = this },
            new BinaryDictionary { Host = this }
        };
            // 根据优先级排序
            Handlers = list.OrderBy(e => e.Priority).ToList();
        }
        #endregion

        #region 处理器
        /// <summary>添加处理器</summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public Binary AddHandler(IBinaryHandler handler)
        {
            if (handler != null)
            {
                handler.Host = this;
                Handlers.Add(handler);
                // 根据优先级排序
                Handlers = Handlers.OrderBy(e => e.Priority).ToList();
            }

            return this;
        }

        /// <summary>添加处理器</summary>
        /// <typeparam name="THandler"></typeparam>
        /// <param name="priority"></param>
        /// <returns></returns>
        public Binary AddHandler<THandler>(Int32 priority = 0) where THandler : IBinaryHandler, new()
        {
            var handler = new THandler
            {
                Host = this
            };
            if (priority != 0) handler.Priority = priority;

            return AddHandler(handler);
        }

        /// <summary>获取处理器</summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetHandler<T>() where T : class, IBinaryHandler
        {
            foreach (var item in Handlers)
            {
                if (item is T handler) return handler;
            }

            return default;
        }
        #endregion

        #region 写入
        /// <summary>写入一个对象</summary>
        /// <param name="value">目标对象</param>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public virtual Boolean Write(Object value, Type type = null)
        {
            if (type == null)
            {
                if (value == null) return true;

                type = value.GetType();

                // 一般类型为空是顶级调用
                if (Hosts.Count == 0 && Log != null && Log.Enable) WriteLog("BinaryWrite {0} {1}", type.Name, value);
            }

            // 优先 IAccessor 接口
            //if (value is IAccessor acc)
            //{
            //    if (acc.Write(Stream, this)) return true;
            //}

            foreach (var item in Handlers)
            {
                if (item.Write(value, type)) return true;
            }
            return false;
        }

        /// <summary>写入字节</summary>
        /// <param name="value"></param>
        public virtual void Write(Byte value) => Stream.WriteByte(value);

        /// <summary>将字节数组部分写入当前流，不写入数组长度。</summary>
        /// <param name="buffer">包含要写入的数据的字节数组。</param>
        /// <param name="offset">buffer 中开始写入的起始点。</param>
        /// <param name="count">要写入的字节数。</param>
        public virtual void Write(Byte[] buffer, Int32 offset, Int32 count)
        {
            if (count < 0) count = buffer.Length - offset;
            Stream.Write(buffer, offset, count);
        }

        /// <summary>写入大小，如果有FieldSize则返回，否则写入编码的大小并返回-1</summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public virtual Int32 WriteSize(Int32 size)
        {
            var sizeWidth = -1;
            if (UseFieldSize && TryGetFieldSize(out var fieldsize, out sizeWidth)) return fieldsize;

            if (sizeWidth < 0) sizeWidth = SizeWidth;
            switch (sizeWidth)
            {
                case 1:
                    Write((Byte)size);
                    break;
                case 2:
                    Write((Int16)size);
                    break;
                case 4:
                    Write(size);
                    break;
                case 0:
                default:
                    WriteEncoded(size);
                    break;
            }

            return -1;
        }

        [ThreadStatic]
        private static Byte[] _encodes;
        #endregion

        #region 读取
        /// <summary>读取指定类型对象</summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public virtual Object Read(Type type)
        {
            Object value = null;
            if (!TryRead(type, ref value)) throw new Exception($"读取失败，不支持类型{type}！");

            return value;
        }

        /// <summary>读取指定类型对象</summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Read<T>() => (T)Read(typeof(T));

        /// <summary>尝试读取指定类型对象</summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual Boolean TryRead(Type type, ref Object value)
        {
            if (Hosts.Count == 0 && Log != null && Log.Enable) WriteLog("BinaryRead {0} {1}", type.Name, value);

            //// 优先 IAccessor 接口
            //if (value is IAccessor acc)
            //{
            //    if (acc.Read(Stream, this)) return true;
            //}
            //if (value == null && type.As<IAccessor>())
            //{
            //    value = type.CreateInstance();
            //    if (value is IAccessor acc2)
            //    {
            //        if (acc2.Read(Stream, this)) return true;
            //    }
            //}

            foreach (var item in Handlers)
            {
                if (item.TryRead(type, ref value)) return true;
            }
            return false;
        }

        /// <summary>读取字节</summary>
        /// <returns></returns>
        public virtual Byte ReadByte()
        {
            var b = Stream.ReadByte();
            if (b < 0) throw new Exception("数据流超出范围！");
            return (Byte)b;
        }

        /// <summary>从当前流中将 count 个字节读入字节数组</summary>
        /// <param name="count">要读取的字节数。</param>
        /// <returns></returns>
        public virtual Byte[] ReadBytes(Int32 count)
        {
            var buffer = Stream.ReadBytes(count);
            //if (n != count) throw new InvalidDataException($"数据不足，需要{count}，实际{n}");

            return buffer;
        }

        /// <summary>读取大小</summary>
        /// <returns></returns>
        public virtual Int32 ReadSize()
        {
            var sizeWidth = -1;
            if (UseFieldSize && TryGetFieldSize(out var size, out sizeWidth)) return size;

            if (sizeWidth < 0) sizeWidth = SizeWidth;
            return sizeWidth switch
            {
                1 => ReadByte(),
                2 => (Int16)Read(typeof(Int16)),
                4 => (Int32)Read(typeof(Int32)),
                0 => ReadEncodedInt32(),
                _ => -1,
            };
        }

        private Boolean TryGetFieldSize(out Int32 size, out Int32 sizeWidth)
        {
            sizeWidth = -1;
            if (Member is MemberInfo member)
            {
                // 获取FieldSizeAttribute特性
                var atts = member.GetCustomAttributes<FieldSizeAttribute>();
                if (atts != null)
                {
                    foreach (var att in atts)
                    {
                        // 检查版本是否匹配
                        if (att.Version.IsNullOrEmpty() || att.Version == Version)
                        {
                            // 如果指定了引用字段，则找引用字段所表示的长度
                            if (!att.ReferenceName.IsNullOrEmpty() && att.TryGetReferenceSize(Hosts.Peek(), member, out size))
                                return true;

                            // 如果指定了固定大小，直接返回
                            size = att.Size;
                            if (size > 0) return true;

                            // 指定了大小位宽
                            if (att.SizeWidth >= 0)
                            {
                                sizeWidth = att.SizeWidth;
                                return false;
                            }
                        }
                    }
                }
            }

            size = -1;
            return false;
        }
        #endregion

        #region 7位压缩编码整数
        /// <summary>写7位压缩编码整数</summary>
        /// <remarks>
        /// 以7位压缩格式写入32位整数，小于7位用1个字节，小于14位用2个字节。
        /// 由每次写入的一个字节的第一位标记后面的字节是否还是当前数据，所以每个字节实际可利用存储空间只有后7位。
        /// </remarks>
        /// <param name="value">数值</param>
        /// <returns>实际写入字节数</returns>
        public Int32 WriteEncoded(Int32 value)
        {
            if (_encodes == null) _encodes = new Byte[16];

            var count = 0;
            var num = (UInt32)value;
            while (num >= 0x80)
            {
                _encodes[count++] = (Byte)(num | 0x80);
                num >>= 7;
            }
            _encodes[count++] = (Byte)num;

            Write(_encodes, 0, count);

            return count;
        }

        /// <summary>以压缩格式读取16位整数</summary>
        /// <returns></returns>
        public Int16 ReadEncodedInt16()
        {
            Byte b;
            Int16 rs = 0;
            Byte n = 0;
            while (true)
            {
                b = ReadByte();
                // 必须转为Int16，否则可能溢出
                rs += (Int16)((b & 0x7f) << n);
                if ((b & 0x80) == 0) break;

                n += 7;
                if (n >= 16) throw new FormatException("数字值过大，无法使用压缩格式读取！");
            }
            return rs;
        }

        /// <summary>以压缩格式读取32位整数</summary>
        /// <returns></returns>
        public Int32 ReadEncodedInt32()
        {
            Byte b;
            var rs = 0;
            Byte n = 0;
            while (true)
            {
                b = ReadByte();
                // 必须转为Int32，否则可能溢出
                rs += (b & 0x7f) << n;
                if ((b & 0x80) == 0) break;

                n += 7;
                if (n >= 32) throw new FormatException("数字值过大，无法使用压缩格式读取！");
            }
            return rs;
        }

        /// <summary>以压缩格式读取64位整数</summary>
        /// <returns></returns>
        public Int64 ReadEncodedInt64()
        {
            Byte b;
            Int64 rs = 0;
            Byte n = 0;
            while (true)
            {
                b = ReadByte();
                // 必须转为Int64，否则可能溢出
                rs += (Int64)(b & 0x7f) << n;
                if ((b & 0x80) == 0) break;

                n += 7;
                if (n >= 64) throw new FormatException("数字值过大，无法使用压缩格式读取！");
            }
            return rs;
        }
        #endregion

        #region 专用扩展
        /// <summary>读取无符号短整数</summary>
        /// <returns></returns>
        public UInt16 ReadUInt16() => Read<UInt16>();

        /// <summary>读取短整数</summary>
        /// <returns></returns>
        public Int16 ReadInt16() => Read<Int16>();

        /// <summary>读取无符号整数</summary>
        /// <returns></returns>
        public UInt32 ReadUInt32() => Read<UInt32>();

        /// <summary>读取整数</summary>
        /// <returns></returns>
        public Int32 ReadInt32() => Read<Int32>();

        /// <summary>写入字节</summary>
        /// <param name="value"></param>
        public void WriteByte(Byte value) => Write(value);

        /// <summary>写入无符号短整数</summary>
        /// <param name="value"></param>
        public void WriteUInt16(UInt16 value) => Write(value);

        /// <summary>写入短整数</summary>
        /// <param name="value"></param>
        public void WriteInt16(Int16 value) => Write(value);

        /// <summary>写入无符号整数</summary>
        /// <param name="value"></param>
        public void WriteUInt32(UInt32 value) => Write(value);

        /// <summary>写入整数</summary>
        /// <param name="value"></param>
        public void WriteInt32(Int32 value) => Write(value);

        /// <summary>BCD字节转十进制数字</summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Int32 FromBCD(Byte b) => (b >> 4) * 10 + (b & 0x0F);

        /// <summary>十进制数字转BCD字节</summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public static Byte ToBCD(Int32 n) => (Byte)(((n / 10) << 4) | (n % 10));

        /// <summary>读取指定长度的BCD字符串。BCD每个字节存放两个数字</summary>
        /// <param name="len"></param>
        /// <returns></returns>
        public String ReadBCD(Int32 len)
        {
            var buf = ReadBytes(len);
            var cs = new Char[len * 2];
            for (var i = 0; i < len; i++)
            {
                cs[i * 2] = (Char)('0' + (buf[i] >> 4));
                cs[i * 2 + 1] = (Char)('0' + (buf[i] & 0x0F));
            }

            return new String(cs).Trim('\0');
        }

        /// <summary>写入指定长度的BCD字符串。BCD每个字节存放两个数字</summary>
        /// <param name="value"></param>
        /// <param name="max"></param>
        public void WriteBCD(String value, Int32 max)
        {
            var buf = new Byte[max];
            for (Int32 i = 0, j = 0; i < max && j + 1 < value.Length; i++, j += 2)
            {
                var a = (Byte)(value[j] - '0');
                var b = (Byte)(value[j + 1] - '0');
                buf[i] = (Byte)((a << 4) | (b & 0x0F));
            }

            Write(buf, 0, buf.Length);
        }

        /// <summary>写入定长字符串。多余截取，少则补零</summary>
        /// <param name="value"></param>
        /// <param name="max"></param>
        public void WriteFixedString(String value, Int32 max)
        {
            var buf = new Byte[max];
            if (!value.IsNullOrEmpty()) Encoding.GetBytes(value, 0, value.Length, buf, 0);

            Write(buf, 0, buf.Length);
        }

        /// <summary>读取定长字符串。多余截取，少则补零</summary>
        /// <param name="len"></param>
        /// <returns></returns>
        public String ReadFixedString(Int32 len)
        {
            var buf = ReadBytes(len);

            // 剔除头尾非法字符
            Int32 s, e;
            for (s = 0; s < len && (buf[s] == 0x00 || buf[s] == 0xFF); s++) ;
            for (e = len - 1; e >= 0 && (buf[e] == 0x00 || buf[e] == 0xFF); e--) ;

            if (s >= len || e < 0) return null;

            var str = Encoding.GetString(buf, s, e - s + 1);
            if (TrimZero && str != null) str = str.Trim('\0');

            return str;
        }
        #endregion

        #region 跟踪日志
        ///// <summary>使用跟踪流。实际上是重新包装一次Stream，必须在设置Stream后，使用之前</summary>
        //public virtual void EnableTrace()
        //{
        //    var stream = Stream;
        //    if (stream is null or TraceStream) return;

        //    Stream = new TraceStream(stream) { Encoding = Encoding, IsLittleEndian = IsLittleEndian };
        //}
        #endregion

        #region 快捷方法
        /// <summary>快速读取</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="stream">数据流</param>
        /// <param name="encodeInt">使用7位编码整数</param>
        /// <returns></returns>
        public static T FastRead<T>(Stream stream, Boolean encodeInt = true)
        {
            var bn = new Binary() { Stream = stream, EncodeInt = encodeInt };
            return bn.Read<T>();
        }

        /// <summary>快速写入</summary>
        /// <param name="value">对象</param>
        /// <param name="encodeInt">使用7位编码整数</param>
        /// <returns></returns>
        public static Packet FastWrite(Object value, Boolean encodeInt = true)
        {
            // 头部预留8字节，方便加协议头
            var bn = new Binary { EncodeInt = encodeInt };
            bn.Stream.Seek(8, SeekOrigin.Current);
            bn.Write(value);

            var buf = bn.GetBytes();
            return new Packet(buf, 8, buf.Length - 8);
        }

        /// <summary>快速写入</summary>
        /// <param name="value">对象</param>
        /// <param name="stream">目标数据流</param>
        /// <param name="encodeInt">使用7位编码整数</param>
        /// <returns></returns>
        public static void FastWrite(Object value, Stream stream, Boolean encodeInt = true)
        {
            var bn = new Binary
            {
                Stream = stream,
                EncodeInt = encodeInt,
            };
            bn.Write(value);
        }
        #endregion
    }
    #endregion


}
