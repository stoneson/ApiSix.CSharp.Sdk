using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;

namespace ApiSix.CSharp
{
    #region IXml
    /// <summary>二进制序列化接口</summary>
    public interface IXml : IFormatterX
    {
        #region 属性
        ///// <summary>编码</summary>
        //Encoding Encoding { get; set; }

        /// <summary>处理器列表</summary>
        List<IXmlHandler> Handlers { get; }

        /// <summary>使用注释</summary>
        Boolean UseComment { get; set; }
        #endregion

        #region 方法
        /// <summary>写入一个对象</summary>
        /// <param name="value">目标对象</param>
        /// <param name="name">名称</param>
        /// <param name="type">类型</param>
        /// <returns></returns>
        Boolean Write(Object value, String name = null, Type type = null);

        /// <summary>获取Xml写入器</summary>
        /// <returns></returns>
        XmlWriter GetWriter();

        /// <summary>获取Xml读取器</summary>
        /// <returns></returns>
        XmlReader GetReader();
        #endregion
    }

    /// <summary>二进制读写处理器接口</summary>
    public interface IXmlHandler : IHandler<IXml>
    {
        ///// <summary>读取一个对象</summary>
        ///// <param name="value"></param>
        ///// <returns></returns>
        //Boolean Read(Object value);
    }

    /// <summary>Xml读写处理器基类</summary>
    public abstract class XmlHandlerBase : HandlerBase<IXml, IXmlHandler>, IXmlHandler
    {
        //private IXml _Host;
        ///// <summary>宿主读写器</summary>
        //public IXml Host { get { return _Host; } set { _Host = value; } }

        //private Int32 _Priority;
        ///// <summary>优先级</summary>
        //public Int32 Priority { get { return _Priority; } set { _Priority = value; } }

        ///// <summary>写入一个对象</summary>
        ///// <param name="value">目标对象</param>
        ///// <param name="type">类型</param>
        ///// <returns></returns>
        //public abstract Boolean Write(Object value, Type type);
    }

    /// <summary>Xml基础类型处理器</summary>
    public class XmlGeneral : XmlHandlerBase
    {
        /// <summary>实例化</summary>
        public XmlGeneral()
        {
            Priority = 10;
        }

        /// <summary>写入一个对象</summary>
        /// <param name="value">目标对象</param>
        /// <param name="type">类型</param>
        /// <returns>是否处理成功</returns>
        public override Boolean Write(Object value, Type type)
        {
            if (value == null && type != typeof(String)) return false;

            var writer = Host.GetWriter();

            // 枚举 写入字符串
            if (type.IsEnum)
            {
                if (Host is Xml xml && xml.EnumString)
                    writer.WriteValue(value + "");
                else
                    writer.WriteValue(value.ToLong());

                return true;
            }

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    writer.WriteValue((Boolean)value);
                    return true;
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.Char:
                    writer.WriteValue(Convert.ToChar(value));
                    return true;
                case TypeCode.DBNull:
                case TypeCode.Empty:
                    writer.WriteValue(0);
                    return true;
                case TypeCode.DateTime:
                    writer.WriteValue(((DateTime)value).ToFullString());
                    return true;
                case TypeCode.Decimal:
                    writer.WriteValue((Decimal)value);
                    return true;
                case TypeCode.Double:
                    writer.WriteValue((Double)value);
                    return true;
                case TypeCode.Single:
                    writer.WriteValue((Single)value);
                    return true;
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                    writer.WriteValue(Convert.ToInt32(value));
                    return true;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    writer.WriteValue(Convert.ToInt64(value));
                    return true;
                case TypeCode.String:
                    writer.WriteValue(value + "");
                    return true;
                case TypeCode.Object:
                    break;
                default:
                    break;
            }

            if (type == typeof(Guid))
            {
                writer.WriteValue(((Guid)value).ToString());
                return true;
            }

            if (type == typeof(DateTimeOffset))
            {
                //writer.WriteValue((DateTimeOffset)value);
                writer.WriteValue(((DateTimeOffset)value) + "");
                return true;
            }

            if (type == typeof(TimeSpan))
            {
                writer.WriteValue(((TimeSpan)value) + "");
                return true;
            }

            if (type == typeof(Byte[]))
            {
                var buf = value as Byte[];
                writer.WriteBase64(buf, 0, buf.Length);
                return true;
            }

            if (type == typeof(Char[]))
            {
                writer.WriteValue(new String((Char[])value));
                return true;
            }

            return false;
        }

        /// <summary>尝试读取</summary>
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

            var reader = Host.GetReader();

            if (type == typeof(Guid))
            {
                value = new Guid(reader.ReadContentAsString());
                return true;
            }
            else if (type == typeof(Byte[]))
            {
                // 用字符串长度作为预设缓冲区的长度
                var buf = new Byte[reader.Value.Length];
                var count = reader.ReadContentAsBase64(buf, 0, buf.Length);
                value = buf.ReadBytes(0, count);
                return true;
            }
            else if (type == typeof(Char[]))
            {
                value = reader.ReadContentAsString().ToCharArray();
                return true;
            }
            else if (type == typeof(DateTimeOffset))
            {
                //value = reader.ReadContentAs(type, null);
                value = DateTimeOffset.Parse(reader.ReadContentAsString());
                return true;
            }
            else if (type == typeof(TimeSpan))
            {
                value = TimeSpan.Parse(reader.ReadContentAsString());
                return true;
            }

            var code = Type.GetTypeCode(type);
            if (code == TypeCode.Object) return false;

            // 读取异构Xml时可能报错
            var v = (reader.NodeType == XmlNodeType.Element ? reader.ReadElementContentAsString() : reader.ReadContentAsString()) + "";

            // 枚举
            if (type.IsEnum)
            {
                value = Enum.Parse(type, v);
                return true;
            }

            switch (code)
            {
                case TypeCode.Boolean:
                    value = v.ToBoolean();
                    return true;
                case TypeCode.Byte:
                    value = Byte.Parse(v, NumberStyles.HexNumber);
                    return true;
                case TypeCode.Char:
                    if (v.Length > 0) value = v[0];
                    return true;
                case TypeCode.DBNull:
                    value = DBNull.Value;
                    return true;
                case TypeCode.DateTime:
                    value = v.ToDateTime();
                    return true;
                case TypeCode.Decimal:
                    value = (Decimal)v.ToDouble();
                    return true;
                case TypeCode.Double:
                    value = v.ToDouble();
                    return true;
                case TypeCode.Empty:
                    value = null;
                    return true;
                case TypeCode.Int16:
                    value = (Int16)v.ToInt();
                    return true;
                case TypeCode.Int32:
                    value = v.ToInt();
                    return true;
                case TypeCode.Int64:
                    value = Int64.Parse(v);
                    return true;
                case TypeCode.Object:
                    break;
                case TypeCode.SByte:
                    value = SByte.Parse(v, NumberStyles.HexNumber);
                    return true;
                case TypeCode.Single:
                    value = (Single)v.ToDouble();
                    return true;
                case TypeCode.String:
                    value = v;
                    return true;
                case TypeCode.UInt16:
                    value = (UInt16)v.ToInt();
                    return true;
                case TypeCode.UInt32:
                    value = (UInt32)v.ToInt();
                    return true;
                case TypeCode.UInt64:
                    value = UInt64.Parse(v);
                    return true;
                default:
                    break;
            }

            return false;
        }
    }

    /// <summary>Xml复合对象处理器</summary>
    public class XmlComposite : XmlHandlerBase
    {
        /// <summary>实例化</summary>
        public XmlComposite()
        {
            Priority = 100;
        }

        /// <summary>写入对象</summary>
        /// <param name="value">目标对象</param>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public override Boolean Write(Object value, Type type)
        {
            if (value == null) return false;

            // 不支持基本类型
            if (Type.GetTypeCode(type) != TypeCode.Object) return false;

            var ms = GetMembers(type);
            WriteLog("XmlWrite {0} 成员{1}个", type.Name, ms.Count);

            Host.Hosts.Push(value);

            //var xml = Host as Xml;
            //xml.WriteStart(type);
            try
            {
                // 获取成员
                foreach (var member in GetMembers(type))
                {
                    var mtype = GetMemberType(member);
                    Host.Member = member;

                    var name = member.GetName();
                    var v = value.GetValue(member);
                    WriteLog("    {0}.{1} {2}", type.Name, member.Name, v);

                    if (!Host.Write(v, name, mtype)) return false;
                }
            }
            finally
            {
                //xml.WriteEnd();

                Host.Hosts.Pop();
            }

            return true;
        }

        /// <summary>尝试读取</summary>
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

            // 不支持基本类型
            if (Type.GetTypeCode(type) != TypeCode.Object) return false;
            // 不支持基类不是Object的特殊类型
            //if (type.BaseType != typeof(Object)) return false;
            if (!type.As<Object>()) return false;

            var reader = Host.GetReader();
            var xml = Host as Xml;

            // 判断类名是否一致
            var name = xml.CurrentName;
            if (!CheckName(name, type)) return false;

            var ms = GetMembers(type);
            WriteLog("XmlRead {0} 成员{1}个", type.Name, ms.Count);
            var dic = ms.ToDictionary(e => e.GetName(), e => e);

            if (value == null) value = type.CreateInstance();

            Host.Hosts.Push(value);

            try
            {
                if (reader.NodeType == XmlNodeType.Attribute)
                {
                    foreach (var item in dic)
                    {
                        var member = item.Value;
                        var v = reader.GetAttribute(item.Key);
                        WriteLog("    {0}.{1} {2}", type.Name, member.Name, v);

                        value.SetValue(member, v);
                    }
                }
                else
                {
                    // 获取成员
                    var member = ms[0];
                    while (reader.NodeType != XmlNodeType.None && reader.IsStartElement())
                    {
                        // 找到匹配的元素，否则跳过
                        if (!dic.TryGetValue(reader.Name, out member) || !member.CanWrite)
                        {
                            reader.Skip();
                            continue;
                        }

                        var mtype = GetMemberType(member);
                        Host.Member = member;

                        var v = value.GetValue(member);
                        WriteLog("    {0}.{1} {2}", type.Name, member.Name, v);

                        if (!Host.TryRead(mtype, ref v)) return false;

                        value.SetValue(member, v);
                    }
                }
            }
            finally
            {
                Host.Hosts.Pop();
            }

            return true;
        }

        #region 辅助
        private Boolean CheckName(String name, Type type)
        {
            if (type.Name.EqualIgnoreCase(name)) return true;

            // 当前正在序列化的成员
            var mb = Host.Member;
            if (mb != null)
            {
                var elm = mb.GetCustomAttribute<XmlElementAttribute>();
                if (elm != null) return elm.ElementName.EqualIgnoreCase(name);

                if (mb.Name.EqualIgnoreCase(name)) return true;
            }

            // 检查类型的Root
            var att = type.GetCustomAttribute<XmlRootAttribute>();
            if (att != null) return att.ElementName.EqualIgnoreCase(name);

            return false;
        }
        #endregion

        #region 获取成员
        /// <summary>获取成员</summary>
        /// <param name="type"></param>
        /// <returns></returns>
        protected virtual List<PropertyInfo> GetMembers(Type type) { return type.GetProperties(true).Cast<PropertyInfo>().ToList(); }

        static Type GetMemberType(MemberInfo member)
        {
            return member.MemberType switch
            {
                MemberTypes.Field => (member as FieldInfo).FieldType,
                MemberTypes.Property => (member as PropertyInfo).PropertyType,
                _ => throw new NotSupportedException(),
            };
        }
        #endregion
    }

    /// <summary>列表数据编码</summary>
    public class XmlList : XmlHandlerBase
    {
        /// <summary>初始化</summary>
        public XmlList()
        {
            // 优先级
            Priority = 20;
        }

        /// <summary>写入</summary>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public override Boolean Write(Object value, Type type)
        {
            if (!type.As<IList>() && !(value is IList)) return false;

            if (!(value is IList list) || list.Count == 0) return true;

            WriteLog("XmlWrite {0} 元素{1}项", type.Name, list.Count);

            Host.Hosts.Push(value);

            //var xml = Host as Xml;
            //xml.WriteStart(type);
            try
            {
                // 循环写入数据
                foreach (var item in list)
                {
                    if (!Host.Write(item)) return false;
                }
            }
            finally
            {
                //xml.WriteEnd();

                Host.Hosts.Pop();
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

            var reader = Host.GetReader();

            // 读一次开始，移动到内部第一个元素
            if (reader.NodeType == XmlNodeType.Attribute) reader.ReadStartElement();
            if (!reader.IsStartElement()) return true;

            // 子元素类型
            var elmType = type.GetElementTypeEx();

            if (!(value is IList list) || value is Array) list = typeof(List<>).MakeGenericType(elmType).CreateInstance() as IList;

            // 清空已有数据
            list.Clear();

            while (reader.IsStartElement())
            {
                Object obj = null;
                if (!Host.TryRead(elmType, ref obj)) return false;

                list.Add(obj);
            }

            if (value != list)
            {
                // 数组的创建比较特别
                if (type.As<Array>())
                {
                    var arr = Array.CreateInstance(type.GetElementTypeEx(), list.Count);
                    list.CopyTo(arr, 0);
                    value = arr;
                }
                else
                    value = list;
            }

            // 读一次结束
            if (reader.NodeType == XmlNodeType.EndElement) reader.ReadEndElement();

            return true;
        }
    }
    #endregion

    /// <summary>Xml序列化</summary>
    public class Xml : FormatterBase, IXml
    {
        #region 属性
        /// <summary>深度</summary>
        public Int32 Depth { get; set; }

        /// <summary>处理器列表</summary>
        public List<IXmlHandler> Handlers { get; private set; }

        /// <summary>使用特性</summary>
        public Boolean UseAttribute { get; set; }

        /// <summary>使用注释</summary>
        public Boolean UseComment { get; set; }

        /// <summary>枚举使用字符串。false时使用数字，默认true</summary>
        public Boolean EnumString { get; set; } = true;

        /// <summary>当前名称</summary>
        public String CurrentName { get; set; }
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public Xml()
        {
            // 遍历所有处理器实现
            var list = new List<IXmlHandler>
            {
                new XmlGeneral { Host = this },
                new XmlList { Host = this },
                new XmlComposite { Host = this }
            };
            // 根据优先级排序
            Handlers = list.OrderBy(e => e.Priority).ToList();
        }
        #endregion

        #region 处理器
        /// <summary>添加处理器</summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public Xml AddHandler(IXmlHandler handler)
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
        public Xml AddHandler<THandler>(Int32 priority = 0) where THandler : IXmlHandler, new()
        {
            var handler = new THandler
            {
                Host = this
            };
            if (priority != 0) handler.Priority = priority;

            return AddHandler(handler);
        }
        #endregion

        #region 写入
        /// <summary>写入一个对象</summary>
        /// <param name="value">目标对象</param>
        /// <param name="name">名称</param>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public Boolean Write(Object value, String name = null, Type type = null)
        {
            if (type == null)
            {
                if (value == null) return true;

                type = value.GetType();
            }

            var writer = GetWriter();

            // 检查接口
            if (value is IXmlSerializable)
            {
                (value as IXmlSerializable).WriteXml(writer);
                return true;
            }

            if (String.IsNullOrEmpty(name))
            {
                // 优先采用类型上的XmlRoot特性
                name = type.GetCustomAttributeValue<XmlRootAttribute, String>(true);
                if (String.IsNullOrEmpty(name)) name = GetName(type);
            }

            name = name.Replace('<', '_');
            name = name.Replace('>', '_');
            name = name.Replace('`', '_');
            CurrentName = name;

            // 一般类型为空是顶级调用
            if (Hosts.Count == 0 && Log != null && Log.Enable) WriteLog("XmlWrite {0} {1}", name ?? type.Name, value);

            // 要先写入根
            Depth++;
            if (Depth == 1) writer.WriteStartDocument();

            WriteStart(type);
            try
            {
                foreach (var item in Handlers)
                {
                    if (item.Write(value, type)) return true;
                }

                writer.WriteValue(value);

                return false;
            }
            finally
            {
                WriteEnd();
                if (Depth == 1)
                {
                    writer.WriteEndDocument();
                    writer.Flush();
                }
                Depth--;
            }
        }

        Boolean IFormatterX.Write(Object value, Type type) => Write(value, null, type);

        /// <summary>写入开头</summary>
        /// <param name="type"></param>
        public void WriteStart(Type type)
        {
            var att = UseAttribute;
            if (!att && Member?.GetCustomAttribute<XmlAttributeAttribute>() != null) att = true;
            if (att && !type.IsValueType && type.GetTypeCode() == TypeCode.Object) att = false;

            var writer = GetWriter();

            // 写入注释。写特性时忽略注释
            if (UseComment && !att)
            {
                var des = "";
                if (Member != null) des = Member.GetDisplayName() ?? Member.GetDescription();
                if (des.IsNullOrEmpty() && type != null) des = type.GetDisplayName() ?? type.GetDescription();

                if (!des.IsNullOrEmpty()) writer.WriteComment(des);
            }

            var name = CurrentName;
            if (att)
                writer.WriteStartAttribute(name);
            else
                writer.WriteStartElement(name);
        }

        /// <summary>写入结尾</summary>
        public void WriteEnd()
        {
            var writer = GetWriter();

            if (writer.WriteState != WriteState.Start)
            {
                if (writer.WriteState == WriteState.Attribute)
                    writer.WriteEndAttribute();
                else
                {
                    writer.WriteEndElement();
                    //替换成WriteFullEndElement方法，写入完整的结束标记。解决读取空节点（短结束标记"/ >"）发生错误。
                    //writer.WriteFullEndElement();
                }
            }
        }

        private XmlWriter _Writer;
        /// <summary>获取Xml写入器</summary>
        /// <returns></returns>
        public XmlWriter GetWriter()
        {
            if (_Writer == null)
            {
                var set = new XmlWriterSettings
                {
                    OmitXmlDeclaration = true,
                    //set.Encoding = Encoding.TrimPreamble();
                    Encoding = Encoding,
                    Indent = true
                };

                _Writer = XmlWriter.Create(Stream, set);
            }

            return _Writer;
        }
        #endregion

        #region 读取
        /// <summary>读取指定类型对象</summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public Object Read(Type type)
        {
            var value = type.As<Array>() ? null : type.CreateInstance();
            if (!TryRead(type, ref value)) throw new Exception("读取失败！");

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
        public Boolean TryRead(Type type, ref Object value)
        {
            var reader = GetReader();
            // 移动到第一个元素
            while (reader.NodeType != XmlNodeType.Element) { if (!reader.Read()) return false; }

            if (Hosts.Count == 0 && Log != null && Log.Enable) WriteLog("XmlRead {0} {1}", type.Name, value);

            // 要先写入根
            Depth++;

            var d = reader.Depth;
            ReadStart(type);

            try
            {
                // 如果读取器层级没有递增，说明这是空节点，需要跳过
                if (reader.Depth == d + 1)
                {
                    foreach (var item in Handlers)
                    {
                        if (item.TryRead(type, ref value)) return true;
                    }

                    value = reader.ReadContentAs(type, null);
                }
            }
            finally
            {
                ReadEnd();
                Depth--;
            }

            return true;
        }

        /// <summary>读取开始</summary>
        /// <param name="type"></param>
        public void ReadStart(Type type)
        {
            var att = UseAttribute;
            if (!att && Member?.GetCustomAttribute<XmlAttributeAttribute>() != null) _ = true;

            var reader = GetReader();
            while (reader.NodeType == XmlNodeType.Comment) reader.Skip();

            CurrentName = reader.Name;
            if (reader.HasAttributes)
                reader.MoveToFirstAttribute();
            else
                reader.ReadStartElement();

            while (reader.NodeType == XmlNodeType.Comment || reader.NodeType == XmlNodeType.Whitespace) reader.Skip();
        }

        /// <summary>读取结束</summary>
        public void ReadEnd()
        {
            var reader = GetReader();
            if (reader.NodeType == XmlNodeType.Attribute) reader.Read();
            if (reader.NodeType == XmlNodeType.EndElement) reader.ReadEndElement();
        }

        private XmlReader _Reader;
        /// <summary>获取Xml读取器</summary>
        /// <returns></returns>
        public XmlReader GetReader()
        {
            if (_Reader == null) _Reader = XmlReader.Create(Stream);

            return _Reader;
        }
        #endregion

        #region 辅助方法
        private static String GetName(Type type)
        {
            if (type.HasElementType) return "ArrayOf" + GetName(type.GetElementType());

            var name = type.GetName();
            name = name.Replace("<", "_");
            //name = name.Replace(">", "_");
            name = name.Replace(",", "_");
            name = name.Replace(">", "");
            return name;
        }

        /// <summary>获取字符串</summary>
        /// <returns></returns>
        public String GetString() => GetBytes().ToStr(Encoding);
        #endregion
    }
}
