﻿using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace ApiSix.CSharp
{
    /// <summary>反射工具类</summary>
    /// <remarks>
    /// </remarks>
    public static class Reflect
    {
        #region 静态
        /// <summary>当前反射提供者</summary>
        public static IReflect Provider { get; set; }

        static Reflect() => Provider = new DefaultReflect();// 如果需要使用快速反射，启用下面这一行//Provider = new EmitReflect();
        #endregion

        #region 反射获取
        /// <summary>根据名称获取类型。可搜索当前目录DLL，自动加载</summary>
        /// <param name="typeName">类型名</param>
        /// <returns></returns>
        public static Type GetTypeEx(this String typeName)
        {
            if (String.IsNullOrEmpty(typeName)) return null;

            var type = Type.GetType(typeName);
            if (type != null) return type;

            return Provider.GetType(typeName, false);
        }

        /// <summary>根据名称获取类型。可搜索当前目录DLL，自动加载</summary>
        /// <param name="typeName">类型名</param>
        /// <param name="isLoadAssembly">是否从未加载程序集中获取类型。使用仅反射的方法检查目标类型，如果存在，则进行常规加载</param>
        /// <returns></returns>
        [Obsolete("不再支持isLoadAssembly")]
        public static Type GetTypeEx(this String typeName, Boolean isLoadAssembly)
        {
            if (String.IsNullOrEmpty(typeName)) return null;

            var type = Type.GetType(typeName);
            if (type != null) return type;

            return Provider.GetType(typeName, isLoadAssembly);
        }

        /// <summary>获取方法</summary>
        /// <remarks>用于具有多个签名的同名方法的场合，不确定是否存在性能问题，不建议普通场合使用</remarks>
        /// <param name="type">类型</param>
        /// <param name="name">名称</param>
        /// <param name="paramTypes">参数类型数组</param>
        /// <returns></returns>
        public static MethodInfo GetMethodEx(this Type type, String name, params Type[] paramTypes)
        {
            if (name.IsNullOrEmpty()) return null;

            // 如果其中一个类型参数为空，得用别的办法
            if (paramTypes.Length > 0 && paramTypes.Any(e => e == null)) return Provider.GetMethods(type, name, paramTypes.Length).FirstOrDefault();

            return Provider.GetMethod(type, name, paramTypes);
        }

        /// <summary>获取指定名称的方法集合，支持指定参数个数来匹配过滤</summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <param name="paramCount">参数个数，-1表示不过滤参数个数</param>
        /// <returns></returns>
        public static MethodInfo[] GetMethodsEx(this Type type, String name = null, Int32 paramCount = -1)
        {
            //if (name.IsNullOrEmpty()) return null;

            return Provider.GetMethods(type, name, paramCount);
        }

        /// <summary>获取属性。搜索私有、静态、基类，优先返回大小写精确匹配成员</summary>
        /// <param name="type">类型</param>
        /// <param name="name">名称</param>
        /// <param name="ignoreCase">忽略大小写</param>
        /// <returns></returns>
        public static PropertyInfo GetPropertyEx(this Type type, String name, Boolean ignoreCase = false)
        {
            if (String.IsNullOrEmpty(name)) return null;

            return Provider.GetProperty(type, name, ignoreCase);
        }

        /// <summary>获取字段。搜索私有、静态、基类，优先返回大小写精确匹配成员</summary>
        /// <param name="type">类型</param>
        /// <param name="name">名称</param>
        /// <param name="ignoreCase">忽略大小写</param>
        /// <returns></returns>
        public static FieldInfo GetFieldEx(this Type type, String name, Boolean ignoreCase = false)
        {
            if (String.IsNullOrEmpty(name)) return null;

            return Provider.GetField(type, name, ignoreCase);
        }

        /// <summary>获取成员。搜索私有、静态、基类，优先返回大小写精确匹配成员</summary>
        /// <param name="type">类型</param>
        /// <param name="name">名称</param>
        /// <param name="ignoreCase">忽略大小写</param>
        /// <returns></returns>
        public static MemberInfo GetMemberEx(this Type type, String name, Boolean ignoreCase = false)
        {
            if (String.IsNullOrEmpty(name)) return null;

            return Provider.GetMember(type, name, ignoreCase);
        }

        /// <summary>获取用于序列化的字段</summary>
        /// <remarks>过滤<seealso cref="T:NonSerializedAttribute"/>特性的字段</remarks>
        /// <param name="type"></param>
        /// <param name="baseFirst"></param>
        /// <returns></returns>
        public static IList<FieldInfo> GetFields(this Type type, Boolean baseFirst) => Provider.GetFields(type, baseFirst);

        /// <summary>获取用于序列化的属性</summary>
        /// <remarks>过滤<seealso cref="T:XmlIgnoreAttribute"/>特性的属性和索引器</remarks>
        /// <param name="type"></param>
        /// <param name="baseFirst"></param>
        /// <returns></returns>
        public static IList<PropertyInfo> GetProperties(this Type type, Boolean baseFirst) => Provider.GetProperties(type, baseFirst);
        #endregion

        #region 反射调用
        /// <summary>反射创建指定类型的实例</summary>
        /// <param name="type">类型</param>
        /// <param name="parameters">参数数组</param>
        /// <returns></returns>
        [DebuggerHidden]
        public static Object CreateInstance(this Type type, params Object[] parameters)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            return Provider.CreateInstance(type, parameters);
        }

        /// <summary>反射调用指定对象的方法。target为类型时调用其静态方法</summary>
        /// <param name="target">要调用其方法的对象，如果要调用静态方法，则target是类型</param>
        /// <param name="name">方法名</param>
        /// <param name="parameters">方法参数</param>
        /// <returns></returns>
        public static Object Invoke(this Object target, String name, params Object[] parameters)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            if (TryInvoke(target, name, out var value, parameters)) return value;

            var type = GetType(ref target);
            throw new XException("类{0}中找不到名为{1}的方法！", type, name);
        }

        /// <summary>反射调用指定对象的方法</summary>
        /// <param name="target">要调用其方法的对象，如果要调用静态方法，则target是类型</param>
        /// <param name="name">方法名</param>
        /// <param name="value">数值</param>
        /// <param name="parameters">方法参数</param>
        /// <remarks>反射调用是否成功</remarks>
        public static Boolean TryInvoke(this Object target, String name, out Object value, params Object[] parameters)
        {
            value = null;

            if (String.IsNullOrEmpty(name)) return false;

            var type = GetType(ref target);

            // 参数类型数组
            var ps = parameters.Select(e => e?.GetType()).ToArray();

            // 如果参数数组出现null，则无法精确匹配，可按参数个数进行匹配
            var method = ps.Any(e => e == null) ? GetMethodEx(type, name) : GetMethodEx(type, name, ps);
            if (method == null) method = GetMethodsEx(type, name, ps.Length > 0 ? ps.Length : -1).FirstOrDefault();
            if (method == null) return false;

            value = Invoke(target, method, parameters);

            return true;
        }

        /// <summary>反射调用指定对象的方法</summary>
        /// <param name="target">要调用其方法的对象，如果要调用静态方法，则target是类型</param>
        /// <param name="method">方法</param>
        /// <param name="parameters">方法参数</param>
        /// <returns></returns>
        [DebuggerHidden]
        public static Object Invoke(this Object target, MethodBase method, params Object[] parameters)
        {
            //if (target == null) throw new ArgumentNullException("target");
            if (method == null) throw new ArgumentNullException(nameof(method));
            if (!method.IsStatic && target == null) throw new ArgumentNullException(nameof(target));

            return Provider.Invoke(target, method, parameters);
        }

        /// <summary>反射调用指定对象的方法</summary>
        /// <param name="target">要调用其方法的对象，如果要调用静态方法，则target是类型</param>
        /// <param name="method">方法</param>
        /// <param name="parameters">方法参数字典</param>
        /// <returns></returns>
        [DebuggerHidden]
        public static Object InvokeWithParams(this Object target, MethodBase method, IDictionary parameters)
        {
            //if (target == null) throw new ArgumentNullException("target");
            if (method == null) throw new ArgumentNullException(nameof(method));
            if (!method.IsStatic && target == null) throw new ArgumentNullException(nameof(target));

            return Provider.InvokeWithParams(target, method, parameters);
        }

        /// <summary>获取目标对象指定名称的属性/字段值</summary>
        /// <param name="target">目标对象</param>
        /// <param name="name">名称</param>
        /// <param name="throwOnError">出错时是否抛出异常</param>
        /// <returns></returns>
        [DebuggerHidden]
        public static Object GetValue(this Object target, String name, Boolean throwOnError = true)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            if (TryGetValue(target, name, out var value)) return value;

            if (!throwOnError) return null;

            var type = GetType(ref target);
            throw new ArgumentException("类[" + type.FullName + "]中不存在[" + name + "]属性或字段。");
        }

        /// <summary>获取目标对象指定名称的属性/字段值</summary>
        /// <param name="target">目标对象</param>
        /// <param name="name">名称</param>
        /// <param name="value">数值</param>
        /// <returns>是否成功获取数值</returns>
        internal static Boolean TryGetValue(this Object target, String name, out Object value)
        {
            value = null;

            if (String.IsNullOrEmpty(name)) return false;

            var type = GetType(ref target);

            var mi = type.GetMemberEx(name, true);
            if (mi == null) return false;

            value = target.GetValue(mi);

            return true;
        }

        /// <summary>获取目标对象的成员值</summary>
        /// <param name="target">目标对象</param>
        /// <param name="member">成员</param>
        /// <returns></returns>
        [DebuggerHidden]
        public static Object GetValue(this Object target, MemberInfo member)
        {
            // 有可能跟普通的 PropertyInfo.GetValue(Object target) 搞混了
            if (member == null)
            {
                member = target as MemberInfo;
                target = null;
            }

            if (target is IModel model && member is PropertyInfo) return model[member.Name];

            if (member is PropertyInfo)
                return Provider.GetValue(target, member as PropertyInfo);
            else if (member is FieldInfo)
                return Provider.GetValue(target, member as FieldInfo);
            else
                throw new ArgumentOutOfRangeException(nameof(member));
        }

        /// <summary>设置目标对象指定名称的属性/字段值，若不存在返回false</summary>
        /// <param name="target">目标对象</param>
        /// <param name="name">名称</param>
        /// <param name="value">数值</param>
        /// <remarks>反射调用是否成功</remarks>
        [DebuggerHidden]
        public static Boolean SetValue(this Object target, String name, Object value)
        {
            if (String.IsNullOrEmpty(name)) return false;

            //// 借助 IModel 优化取值赋值，有 IExtend 扩展属性的实体类过于复杂而不支持，例如IEntity就有脏数据问题
            if (target is IModel model && !(target is  IExtend))
            {
                model[name] = value;
                return true;
            }

            var type = GetType(ref target);

            var mi = type.GetMemberEx(name, true);
            if (mi == null) return false;

            target.SetValue(mi, value);

            //throw new ArgumentException("类[" + type.FullName + "]中不存在[" + name + "]属性或字段。");
            return true;
        }

        /// <summary>设置目标对象的成员值</summary>
        /// <param name="target">目标对象</param>
        /// <param name="member">成员</param>
        /// <param name="value">数值</param>
        [DebuggerHidden]
        public static void SetValue(this Object target, MemberInfo member, Object value)
        {
            //// 借助 IModel 优化取值赋值，有 IExtend 扩展属性的实体类过于复杂而不支持，例如IEntity就有脏数据问题
            if (target is IModel model && !(target is IExtend) && member is PropertyInfo)
                model[member.Name] = value;
            else
            if (member is PropertyInfo)
                Provider.SetValue(target, member as PropertyInfo, value);
            else if (member is FieldInfo)
                Provider.SetValue(target, member as FieldInfo, value);
            else
                throw new ArgumentOutOfRangeException(nameof(member));
        }

        /// <summary>从源对象拷贝数据到目标对象</summary>
        /// <param name="target">目标对象</param>
        /// <param name="src">源对象</param>
        /// <param name="deep">递归深度拷贝，直接拷贝成员值而不是引用</param>
        /// <param name="excludes">要忽略的成员</param>
        public static void Copy(this Object target, Object src, Boolean deep = false, params String[] excludes) => Provider.Copy(target, src, deep, excludes);

        /// <summary>从源字典拷贝数据到目标对象</summary>
        /// <param name="target">目标对象</param>
        /// <param name="dic">源字典</param>
        /// <param name="deep">递归深度拷贝，直接拷贝成员值而不是引用</param>
        public static void Copy(this Object target, IDictionary<String, Object> dic, Boolean deep = false) => Provider.Copy(target, dic, deep);
        #endregion

        #region 类型辅助
        /// <summary>获取一个类型的元素类型</summary>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public static Type GetElementTypeEx(this Type type) => Provider.GetElementType(type);

        /// <summary>类型转换</summary>
        /// <param name="value">数值</param>
        /// <param name="conversionType"></param>
        /// <returns></returns>
        public static Object ChangeType(this Object value, Type conversionType) => Provider.ChangeType(value, conversionType);

        /// <summary>类型转换</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public static TResult ChangeType<TResult>(this Object value)
        {
            if (value is TResult result) return result;

            return (TResult)ChangeType(value, typeof(TResult));
        }

        /// <summary>获取类型的友好名称</summary>
        /// <param name="type">指定类型</param>
        /// <param name="isfull">是否全名，包含命名空间</param>
        /// <returns></returns>
        public static String GetName(this Type type, Boolean isfull = false) => Provider.GetName(type, isfull);

        /// <summary>从参数数组中获取类型数组</summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static Type[] GetTypeArray(this Object[] args)
        {
            if (args == null) return Type.EmptyTypes;

            var typeArray = new Type[args.Length];
            for (var i = 0; i < typeArray.Length; i++)
            {
                if (args[i] == null)
                    typeArray[i] = typeof(Object);
                else
                    typeArray[i] = args[i].GetType();
            }
            return typeArray;
        }

        /// <summary>获取成员的类型，字段和属性是它们的类型，方法是返回类型，类型是自身</summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public static Type GetMemberType(this MemberInfo member)
        {
            switch (member.MemberType)
            {
                case MemberTypes.Constructor: return (member as ConstructorInfo).DeclaringType;
                case MemberTypes.Field: return (member as FieldInfo).FieldType;
                case MemberTypes.Method: return (member as MethodInfo).ReturnType;
                case MemberTypes.Property: return (member as PropertyInfo).PropertyType;
                case MemberTypes.TypeInfo:
                case MemberTypes.NestedType : return member as Type;
                default: return null;
            }
        }

        /// <summary>获取类型代码</summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static TypeCode GetTypeCode(this Type type) => Type.GetTypeCode(type);

        /// <summary>是否整数。Byte/Int16/Int32/Int64</summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Boolean IsInt(this Type type)
        {
            return type == typeof(Int32)
                || type == typeof(Int64)
                || type == typeof(Int16)
                || type == typeof(UInt32)
                || type == typeof(UInt64)
                || type == typeof(UInt16)
                || type == typeof(Byte)
                || type == typeof(SByte)
                ;
        }

        /// <summary>是否泛型列表</summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Boolean IsList(this Type type) => type != null && type.IsGenericType && type.As(typeof(IList<>));

        /// <summary>是否泛型字典</summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Boolean IsDictionary(this Type type) => type != null && type.IsGenericType && type.As(typeof(IDictionary<,>));
        #endregion

        #region 插件
        /// <summary>是否能够转为指定基类</summary>
        /// <param name="type"></param>
        /// <param name="baseType"></param>
        /// <returns></returns>
        public static Boolean As(this Type type, Type baseType) => Provider.As(type, baseType);

        /// <summary>是否能够转为指定基类</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Boolean As<T>(this Type type) => Provider.As(type, typeof(T));

        /// <summary>在指定程序集中查找指定基类的子类</summary>
        /// <param name="asm">指定程序集</param>
        /// <param name="baseType">基类或接口</param>
        /// <returns></returns>
        public static IEnumerable<Type> GetSubclasses(this Assembly asm, Type baseType) => Provider.GetSubclasses(asm, baseType);

        /// <summary>在所有程序集中查找指定基类或接口的子类实现</summary>
        /// <param name="baseType">基类或接口</param>
        /// <returns></returns>
        public static IEnumerable<Type> GetAllSubclasses(this Type baseType) => Provider.GetAllSubclasses(baseType);

        ///// <summary>在所有程序集中查找指定基类或接口的子类实现</summary>
        ///// <param name="baseType">基类或接口</param>
        ///// <param name="isLoadAssembly">是否加载为加载程序集</param>
        ///// <returns></returns>
        //[Obsolete]
        //public static IEnumerable<Type> GetAllSubclasses(this Type baseType, Boolean isLoadAssembly) => Provider.GetAllSubclasses(baseType, isLoadAssembly);
        #endregion

        #region 辅助方法
        /// <summary>获取类型，如果target是Type类型，则表示要反射的是静态成员</summary>
        /// <param name="target">目标对象</param>
        /// <returns></returns>
        static Type GetType(ref Object target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            var type = target as Type;
            if (type == null)
                type = target.GetType();
            else
                target = null;

            return type;
        }

        ///// <summary>判断某个类型是否可空类型</summary>
        ///// <param name="type">类型</param>
        ///// <returns></returns>
        //static Boolean IsNullable(Type type)
        //{
        //    //if (type.IsValueType) return false;

        //    if (type.IsGenericType && !type.IsGenericTypeDefinition &&
        //        Object.ReferenceEquals(type.GetGenericTypeDefinition(), typeof(Nullable<>))) return true;

        //    return false;
        //}

        /// <summary>把一个方法转为泛型委托，便于快速反射调用</summary>
        /// <typeparam name="TFunc"></typeparam>
        /// <param name="method"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static TFunc As<TFunc>(this MethodInfo method, Object target = null)
        {
            if (method == null) return default;

            if (target == null)
                return (TFunc)(Object)Delegate.CreateDelegate(typeof(TFunc), method, true);
            else
                return (TFunc)(Object)Delegate.CreateDelegate(typeof(TFunc), target, method, true);
        }


        private static readonly ConcurrentDictionary<PropertyInfo, String> _cache = new ConcurrentDictionary<PropertyInfo, string>();
        /// <summary>获取序列化名称</summary>
        /// <param name="pi"></param>
        /// <returns></returns>
        public static String GetName(this PropertyInfo pi)
        {
            if (_cache.TryGetValue(pi, out var name)) return name;

            if (name.IsNullOrEmpty())
            {
                var att = pi.GetCustomAttribute<DataMemberAttribute>();
                if (att != null && !att.Name.IsNullOrEmpty()) name = att.Name;
            }
            if (name.IsNullOrEmpty())
            {
                var att = pi.GetCustomAttribute<XmlElementAttribute>();
                if (att != null && !att.ElementName.IsNullOrEmpty()) name = att.ElementName;
            }
            if (name.IsNullOrEmpty())
            {
                var att = pi.GetCustomAttribute<JsonPropertyAttribute>();
                if (att != null && !att.PropertyName.IsNullOrEmpty()) name = att.PropertyName;
            }
            if (name.IsNullOrEmpty()) name = pi.Name;

            _cache.TryAdd(pi, name);

            return name;
        }
        #endregion
    }

    #region IReflect
    /// <summary>反射接口</summary>
    /// <remarks>该接口仅用于扩展，不建议外部使用</remarks>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public interface IReflect
    {
        #region 反射获取
        /// <summary>根据名称获取类型</summary>
        /// <param name="typeName">类型名</param>
        /// <param name="isLoadAssembly">是否从未加载程序集中获取类型。使用仅反射的方法检查目标类型，如果存在，则进行常规加载</param>
        /// <returns></returns>
        Type GetType(String typeName, Boolean isLoadAssembly);

        /// <summary>获取方法</summary>
        /// <remarks>用于具有多个签名的同名方法的场合，不确定是否存在性能问题，不建议普通场合使用</remarks>
        /// <param name="type">类型</param>
        /// <param name="name">名称</param>
        /// <param name="paramTypes">参数类型数组</param>
        /// <returns></returns>
        MethodInfo GetMethod(Type type, String name, params Type[] paramTypes);

        /// <summary>获取指定名称的方法集合，支持指定参数个数来匹配过滤</summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <param name="paramCount">参数个数，-1表示不过滤参数个数</param>
        /// <returns></returns>
        MethodInfo[] GetMethods(Type type, String name, Int32 paramCount = -1);

        /// <summary>获取属性</summary>
        /// <param name="type">类型</param>
        /// <param name="name">名称</param>
        /// <param name="ignoreCase">忽略大小写</param>
        /// <returns></returns>
        PropertyInfo GetProperty(Type type, String name, Boolean ignoreCase);

        /// <summary>获取字段</summary>
        /// <param name="type">类型</param>
        /// <param name="name">名称</param>
        /// <param name="ignoreCase">忽略大小写</param>
        /// <returns></returns>
        FieldInfo GetField(Type type, String name, Boolean ignoreCase);

        /// <summary>获取成员</summary>
        /// <param name="type">类型</param>
        /// <param name="name">名称</param>
        /// <param name="ignoreCase">忽略大小写</param>
        /// <returns></returns>
        MemberInfo GetMember(Type type, String name, Boolean ignoreCase);

        /// <summary>获取字段</summary>
        /// <param name="type"></param>
        /// <param name="baseFirst"></param>
        /// <returns></returns>
        IList<FieldInfo> GetFields(Type type, Boolean baseFirst = true);

        /// <summary>获取属性</summary>
        /// <param name="type"></param>
        /// <param name="baseFirst"></param>
        /// <returns></returns>
        IList<PropertyInfo> GetProperties(Type type, Boolean baseFirst = true);
        #endregion

        #region 反射调用
        /// <summary>反射创建指定类型的实例</summary>
        /// <param name="type">类型</param>
        /// <param name="parameters">参数数组</param>
        /// <returns></returns>
        Object CreateInstance(Type type, params Object[] parameters);

        /// <summary>反射调用指定对象的方法</summary>
        /// <param name="target">要调用其方法的对象，如果要调用静态方法，则target是类型</param>
        /// <param name="method">方法</param>
        /// <param name="parameters">方法参数</param>
        /// <returns></returns>
        Object Invoke(Object target, MethodBase method, params Object[] parameters);

        /// <summary>反射调用指定对象的方法</summary>
        /// <param name="target">要调用其方法的对象，如果要调用静态方法，则target是类型</param>
        /// <param name="method">方法</param>
        /// <param name="parameters">方法参数字典</param>
        /// <returns></returns>
        Object InvokeWithParams(Object target, MethodBase method, IDictionary parameters);

        /// <summary>获取目标对象的属性值</summary>
        /// <param name="target">目标对象</param>
        /// <param name="property">属性</param>
        /// <returns></returns>
        Object GetValue(Object target, PropertyInfo property);

        /// <summary>获取目标对象的字段值</summary>
        /// <param name="target">目标对象</param>
        /// <param name="field">字段</param>
        /// <returns></returns>
        Object GetValue(Object target, FieldInfo field);

        /// <summary>设置目标对象的属性值</summary>
        /// <param name="target">目标对象</param>
        /// <param name="property">属性</param>
        /// <param name="value">数值</param>
        void SetValue(Object target, PropertyInfo property, Object value);

        /// <summary>设置目标对象的字段值</summary>
        /// <param name="target">目标对象</param>
        /// <param name="field">字段</param>
        /// <param name="value">数值</param>
        void SetValue(Object target, FieldInfo field, Object value);

        /// <summary>从源对象拷贝数据到目标对象</summary>
        /// <param name="target">目标对象</param>
        /// <param name="src">源对象</param>
        /// <param name="deep">递归深度拷贝，直接拷贝成员值而不是引用</param>
        /// <param name="excludes">要忽略的成员</param>
        void Copy(Object target, Object src, Boolean deep = false, params String[] excludes);

        /// <summary>从源字典拷贝数据到目标对象</summary>
        /// <param name="target">目标对象</param>
        /// <param name="dic">源字典</param>
        /// <param name="deep">递归深度拷贝，直接拷贝成员值而不是引用</param>
        void Copy(Object target, IDictionary<String, Object> dic, Boolean deep = false);
        #endregion

        #region 类型辅助
        /// <summary>获取一个类型的元素类型</summary>
        /// <param name="type">类型</param>
        /// <returns></returns>
        Type GetElementType(Type type);

        /// <summary>类型转换</summary>
        /// <param name="value">数值</param>
        /// <param name="conversionType"></param>
        /// <returns></returns>
        Object ChangeType(Object value, Type conversionType);

        /// <summary>获取类型的友好名称</summary>
        /// <param name="type">指定类型</param>
        /// <param name="isfull">是否全名，包含命名空间</param>
        /// <returns></returns>
        String GetName(Type type, Boolean isfull);
        #endregion

        #region 插件
        /// <summary>是否能够转为指定基类</summary>
        /// <param name="type"></param>
        /// <param name="baseType"></param>
        /// <returns></returns>
        Boolean As(Type type, Type baseType);

        /// <summary>在指定程序集中查找指定基类或接口的所有子类实现</summary>
        /// <param name="asm">指定程序集</param>
        /// <param name="baseType">基类或接口，为空时返回所有类型</param>
        /// <returns></returns>
        IEnumerable<Type> GetSubclasses(Assembly asm, Type baseType);

        /// <summary>在所有程序集中查找指定基类或接口的子类实现</summary>
        /// <param name="baseType">基类或接口</param>
        /// <returns></returns>
        IEnumerable<Type> GetAllSubclasses(Type baseType);

        ///// <summary>在所有程序集中查找指定基类或接口的子类实现</summary>
        ///// <param name="baseType">基类或接口</param>
        ///// <param name="isLoadAssembly">是否加载为加载程序集</param>
        ///// <returns></returns>
        //IEnumerable<Type> GetAllSubclasses(Type baseType, Boolean isLoadAssembly);
        #endregion
    }

    /// <summary>默认反射实现</summary>
    /// <remarks>该接口仅用于扩展，不建议外部使用</remarks>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public class DefaultReflect : IReflect
    {
        #region 反射获取
        /// <summary>根据名称获取类型</summary>
        /// <param name="typeName">类型名</param>
        /// <param name="isLoadAssembly">是否从未加载程序集中获取类型。使用仅反射的方法检查目标类型，如果存在，则进行常规加载</param>
        /// <returns></returns>
        public virtual Type GetType(String typeName, Boolean isLoadAssembly) => AssemblyX.GetType(typeName, isLoadAssembly);

        private static readonly BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        private static readonly BindingFlags bfic = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.IgnoreCase;

        /// <summary>获取方法</summary>
        /// <remarks>用于具有多个签名的同名方法的场合，不确定是否存在性能问题，不建议普通场合使用</remarks>
        /// <param name="type">类型</param>
        /// <param name="name">名称</param>
        /// <param name="paramTypes">参数类型数组</param>
        /// <returns></returns>
        public virtual MethodInfo GetMethod(Type type, String name, params Type[] paramTypes)
        {
            MethodInfo mi = null;
            while (true)
            {
                if (paramTypes == null || paramTypes.Length == 0)
                    mi = type.GetMethod(name, bf);
                else
                    mi = type.GetMethod(name, bf, null, paramTypes, null);
                if (mi != null) return mi;

                type = type.BaseType;
                if (type == null || type == typeof(Object)) break;
            }
            return null;
        }

        /// <summary>获取指定名称的方法集合，支持指定参数个数来匹配过滤</summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <param name="paramCount">参数个数，-1表示不过滤参数个数</param>
        /// <returns></returns>
        public virtual MethodInfo[] GetMethods(Type type, String name, Int32 paramCount = -1)
        {
            var ms = type.GetMethods(bf);
            if (ms == null || ms.Length == 0 || string.IsNullOrWhiteSpace(name))
                return ms;

            var list = new List<MethodInfo>();
            foreach (var item in ms)
            {
                if (item.Name == name)
                {
                    if (paramCount >= 0 && item.GetParameters().Length == paramCount) list.Add(item);
                }
            }
            return list.ToArray();
        }

        /// <summary>获取属性</summary>
        /// <param name="type">类型</param>
        /// <param name="name">名称</param>
        /// <param name="ignoreCase">忽略大小写</param>
        /// <returns></returns>
        public virtual PropertyInfo GetProperty(Type type, String name, Boolean ignoreCase)
        {
            // 父类私有属性的获取需要递归，可见范围则不需要，有些类型的父类为空，比如接口
            while (type != null && type != typeof(Object))
            {
                //var pi = type.GetProperty(name, ignoreCase ? bfic : bf);
                var pi = type.GetProperty(name, bf);
                if (pi != null) return pi;
                if (ignoreCase)
                {
                    pi = type.GetProperty(name, bfic);
                    if (pi != null) return pi;
                }

                type = type.BaseType;
            }
            return null;
        }

        /// <summary>获取字段</summary>
        /// <param name="type">类型</param>
        /// <param name="name">名称</param>
        /// <param name="ignoreCase">忽略大小写</param>
        /// <returns></returns>
        public virtual FieldInfo GetField(Type type, String name, Boolean ignoreCase)
        {
            // 父类私有字段的获取需要递归，可见范围则不需要，有些类型的父类为空，比如接口
            while (type != null && type != typeof(Object))
            {
                //var fi = type.GetField(name, ignoreCase ? bfic : bf);
                var fi = type.GetField(name, bf);
                if (fi != null) return fi;
                if (ignoreCase)
                {
                    fi = type.GetField(name, bfic);
                    if (fi != null) return fi;
                }

                type = type.BaseType;
            }
            return null;
        }

        /// <summary>获取成员</summary>
        /// <param name="type">类型</param>
        /// <param name="name">名称</param>
        /// <param name="ignoreCase">忽略大小写</param>
        /// <returns></returns>
        public virtual MemberInfo GetMember(Type type, String name, Boolean ignoreCase)
        {
            // 父类私有成员的获取需要递归，可见范围则不需要，有些类型的父类为空，比如接口
            while (type != null && type != typeof(Object))
            {
                var fs = type.GetMember(name, ignoreCase ? bfic : bf);
                if (fs != null && fs.Length > 0)
                {
                    // 得到多个的时候，优先返回精确匹配
                    if (ignoreCase && fs.Length > 1)
                    {
                        foreach (var fi in fs)
                        {
                            if (fi.Name == name) return fi;
                        }
                    }
                    return fs[0];
                }

                type = type.BaseType;
            }
            return null;
        }
        #endregion

        #region 反射获取 字段/属性
        private readonly ConcurrentDictionary<Type, IList<FieldInfo>> _cache1 = new ConcurrentDictionary<Type, IList<FieldInfo>>();
        private readonly ConcurrentDictionary<Type, IList<FieldInfo>> _cache2 = new ConcurrentDictionary<Type, IList<FieldInfo>>();
        /// <summary>获取字段</summary>
        /// <param name="type"></param>
        /// <param name="baseFirst"></param>
        /// <returns></returns>
        public virtual IList<FieldInfo> GetFields(Type type, Boolean baseFirst = true)
        {
            if (baseFirst)
                return _cache1.GetOrAdd(type, key => GetFields2(key, true));
            else
                return _cache2.GetOrAdd(type, key => GetFields2(key, false));
        }

        private IList<FieldInfo> GetFields2(Type type, Boolean baseFirst)
        {
            var list = new List<FieldInfo>();

            // Void*的基类就是null
            if (type == typeof(Object) || type.BaseType == null) return list;

            if (baseFirst) list.AddRange(GetFields(type.BaseType));

            var fis = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var fi in fis)
            {
                if (fi.GetCustomAttribute<NonSerializedAttribute>() != null) continue;

                list.Add(fi);
            }

            if (!baseFirst) list.AddRange(GetFields(type.BaseType));

            return list;
        }

        private readonly ConcurrentDictionary<Type, IList<PropertyInfo>> _cache3 = new ConcurrentDictionary<Type, IList<PropertyInfo>>();
        private readonly ConcurrentDictionary<Type, IList<PropertyInfo>> _cache4 = new ConcurrentDictionary<Type, IList<PropertyInfo>>();
        /// <summary>获取属性</summary>
        /// <param name="type"></param>
        /// <param name="baseFirst"></param>
        /// <returns></returns>
        public virtual IList<PropertyInfo> GetProperties(Type type, Boolean baseFirst = true)
        {
            if (baseFirst)
                return _cache3.GetOrAdd(type, key => GetProperties2(key, true));
            else
                return _cache4.GetOrAdd(type, key => GetProperties2(key, false));
        }

        private IList<PropertyInfo> GetProperties2(Type type, Boolean baseFirst)
        {
            var list = new List<PropertyInfo>();

            // Void*的基类就是null
            if (type == typeof(Object) || type.BaseType == null) return list;

            // 本身type.GetProperties就可以得到父类属性，只是不能保证父类属性在子类属性之前
            if (baseFirst) list.AddRange(GetProperties(type.BaseType));

            // 父类子类可能因为继承而有重名的属性，此时以子类优先，否则反射父类属性会出错
            var set = new HashSet<String>(list.Select(e => e.Name));

            //var pis = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var pis = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (var pi in pis)
            {
                if (pi.GetIndexParameters().Length > 0) continue;
                if (pi.GetCustomAttribute<XmlIgnoreAttribute>() != null) continue;
                //if (pi.GetCustomAttribute<ScriptIgnoreAttribute>() != null) continue;
                if (pi.GetCustomAttribute<IgnoreDataMemberAttribute>() != null) continue;

                if (!set.Contains(pi.Name))
                {
                    list.Add(pi);
                    set.Add(pi.Name);
                }
            }

            // 获取用于序列化的属性列表时，加上非公有的数据成员
            pis = type.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (var pi in pis)
            {
                if (pi.GetIndexParameters().Length > 0) continue;
                if (pi.GetCustomAttribute<XmlElementAttribute>() == null && pi.GetCustomAttribute<DataMemberAttribute>() == null) continue;

                if (!set.Contains(pi.Name))
                {
                    list.Add(pi);
                    set.Add(pi.Name);
                }
            }

            if (!baseFirst) list.AddRange(GetProperties(type.BaseType).Where(e => !set.Contains(e.Name)));

            return list;
        }
        #endregion

        #region 反射调用
        /// <summary>反射创建指定类型的实例</summary>
        /// <param name="type">类型</param>
        /// <param name="parameters">参数数组</param>
        /// <returns></returns>
        public virtual Object CreateInstance(Type type, params Object[] parameters)
        {
            try
            {
                if (parameters == null || parameters.Length == 0)
                {
                    // 基元类型
                    switch (type.GetTypeCode())
                    {
                        case TypeCode.Empty:
                        case TypeCode.DBNull: return null;
                        case TypeCode.Boolean: return false;
                        case TypeCode.Char: return '\0';
                        case TypeCode.SByte: return (SByte)0;
                        case TypeCode.Byte: return (Byte)0;
                        case TypeCode.Int16: return (Int16)0;
                        case TypeCode.UInt16: return (UInt16)0;
                        case TypeCode.Int32: return 0;
                        case TypeCode.UInt32: return 0U;
                        case TypeCode.Int64: return 0L;
                        case TypeCode.UInt64: return 0UL;
                        case TypeCode.Single: return 0F;
                        case TypeCode.Double: return 0D;
                        case TypeCode.Decimal: return 0M;
                        case TypeCode.DateTime: return DateTime.MinValue;
                        case TypeCode.String: return String.Empty;
                        default: return Activator.CreateInstance(type, true);
                    }
                }
                else
                    return Activator.CreateInstance(type, parameters);
            }
            catch (Exception ex)
            {
                //throw new Exception("创建对象失败 type={0} parameters={1}".F(type.FullName, parameters.Join()), ex);
                throw new Exception($"创建对象失败 type={type.FullName} parameters={parameters.Join()} {ex.GetTrue()?.Message}", ex);
            }
        }

        /// <summary>反射调用指定对象的方法</summary>
        /// <param name="target">要调用其方法的对象，如果要调用静态方法，则target是类型</param>
        /// <param name="method">方法</param>
        /// <param name="parameters">方法参数</param>
        /// <returns></returns>
        public virtual Object Invoke(Object target, MethodBase method, params Object[] parameters) => method.Invoke(target, parameters);

        /// <summary>反射调用指定对象的方法</summary>
        /// <param name="target">要调用其方法的对象，如果要调用静态方法，则target是类型</param>
        /// <param name="method">方法</param>
        /// <param name="parameters">方法参数字典</param>
        /// <returns></returns>
        public virtual Object InvokeWithParams(Object target, MethodBase method, IDictionary parameters)
        {
            // 该方法没有参数，无视外部传入参数
            var pis = method.GetParameters();
            if (pis == null || pis.Length == 0) return Invoke(target, method, null);

            var ps = new Object[pis.Length];
            for (var i = 0; i < pis.Length; i++)
            {
                Object v = null;
                if (parameters != null && parameters.Contains(pis[i].Name)) v = parameters[pis[i].Name];
                ps[i] = v.ChangeType(pis[i].ParameterType);
            }

            return method.Invoke(target, ps);
        }

        /// <summary>获取目标对象的属性值</summary>
        /// <param name="target">目标对象</param>
        /// <param name="property">属性</param>
        /// <returns></returns>
        public virtual Object GetValue(Object target, PropertyInfo property) => property.GetValue(target, null);

        /// <summary>获取目标对象的字段值</summary>
        /// <param name="target">目标对象</param>
        /// <param name="field">字段</param>
        /// <returns></returns>
        public virtual Object GetValue(Object target, FieldInfo field) => field.GetValue(target);

        /// <summary>设置目标对象的属性值</summary>
        /// <param name="target">目标对象</param>
        /// <param name="property">属性</param>
        /// <param name="value">数值</param>
        public virtual void SetValue(Object target, PropertyInfo property, Object value) => property.SetValue(target, value.ChangeType(property.PropertyType), null);

        /// <summary>设置目标对象的字段值</summary>
        /// <param name="target">目标对象</param>
        /// <param name="field">字段</param>
        /// <param name="value">数值</param>
        public virtual void SetValue(Object target, FieldInfo field, Object value) => field.SetValue(target, value.ChangeType(field.FieldType));
        #endregion

        #region 对象拷贝
        /// <summary>从源对象拷贝数据到目标对象</summary>
        /// <param name="target">目标对象</param>
        /// <param name="source">源对象</param>
        /// <param name="deep">递归深度拷贝，直接拷贝成员值而不是引用</param>
        /// <param name="excludes">要忽略的成员</param>
        public virtual void Copy(Object target, Object source, Boolean deep = false, params String[] excludes)
        {
            if (target == null || source == null || target == source) return;

            var targetType = target.GetType();
            // 基础类型无法拷贝
            if (targetType.GetTypeCode() != TypeCode.Object) throw new XException("基础类型 {0} 无法拷贝", targetType.FullName);

            // 不是深度拷贝时，直接复制引用
            if (!deep)
            {
                var sourceType = source.GetType();

                //// 借助 IModel 优化取值赋值，有 IExtend 扩展属性的实体类过于复杂而不支持，例如IEntity就有脏数据问题
                if (target is IModel dst && !(target is  IExtend))
                {
                    var pis = sourceType.GetProperties(true);
                    foreach (var pi in targetType.GetProperties(true))
                    {
                        if (excludes != null && excludes.Contains(pi.Name)) continue;

                        var pi2 = pis.FirstOrDefault(e => e.Name == pi.Name);
                        if (pi2 != null && pi2.CanRead)
                            dst[pi.Name] = source is IModel src ? src[pi2.Name] : GetValue(source, pi2);
                    }
                }
                else
                {
                    var pis = sourceType.GetProperties(true);
                    foreach (var pi in targetType.GetProperties(true))
                    {
                        if (!pi.CanWrite) continue;
                        if (excludes != null && excludes.Contains(pi.Name)) continue;
                        //if (pi.GetIndexParameters().Length > 0) continue;
                        //if (pi.GetCustomAttribute<IgnoreDataMemberAttribute>(false) != null) continue;
                        //if (pi.GetCustomAttribute<XmlIgnoreAttribute>() != null) continue;

                        var pi2 = pis.FirstOrDefault(e => e.Name == pi.Name);
                        if (pi2 != null && pi2.CanRead)
                            SetValue(target, pi, source is IModel src ? src[pi2.Name] : GetValue(source, pi2));
                    }
                }
                return;
            }

            // 来源对象转为字典
            var dic = new Dictionary<String, Object>();
            foreach (var pi in source.GetType().GetProperties(true))
            {
                if (!pi.CanRead) continue;
                if (excludes != null && excludes.Contains(pi.Name)) continue;
                //if (pi.GetIndexParameters().Length > 0) continue;
                //if (pi.GetCustomAttribute<XmlIgnoreAttribute>() != null) continue;

                dic[pi.Name] = GetValue(source, pi);
            }

            Copy(target, dic, deep);
        }

        /// <summary>从源字典拷贝数据到目标对象</summary>
        /// <param name="target">目标对象</param>
        /// <param name="source">源字典</param>
        /// <param name="deep">递归深度拷贝，直接拷贝成员值而不是引用</param>
        public virtual void Copy(Object target, IDictionary<String, Object> source, Boolean deep = false)
        {
            if (target == null || source == null || source.Count == 0 || target == source) return;

            foreach (var pi in target.GetType().GetProperties(true))
            {
                if (!pi.CanWrite) continue;
                //if (pi.GetIndexParameters().Length > 0) continue;
                //if (pi.GetCustomAttribute<XmlIgnoreAttribute>() != null) continue;

                if (source.TryGetValue(pi.Name, out var obj))
                {
                    // 基础类型直接拷贝，不考虑深拷贝
                    if (deep && pi.PropertyType.GetTypeCode() == TypeCode.Object)
                    {
                        var v = GetValue(target, pi);

                        // 如果目标对象该成员为空，需要创建再拷贝
                        if (v == null)
                        {
                            v = pi.PropertyType.CreateInstance();
                            SetValue(target, pi, v);
                        }
                        Copy(v, obj, deep);
                    }
                    else
                        SetValue(target, pi, obj);
                }
            }
        }
        #endregion

        #region 类型辅助
        /// <summary>获取一个类型的元素类型</summary>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public virtual Type GetElementType(Type type)
        {
            if (type.HasElementType) return type.GetElementType();

            if (type.As<IEnumerable>())
            {
                // 如果实现了IEnumerable<>接口，那么取泛型参数
                foreach (var item in type.GetInterfaces())
                {
                    if (item.IsGenericType && item.GetGenericTypeDefinition() == typeof(IEnumerable<>)) return item.GetGenericArguments()[0];
                }
                //// 通过索引器猜测元素类型
                //var pi = type.GetProperty("Item", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                //if (pi != null) return pi.PropertyType;
            }

            return null;
        }

        /// <summary>类型转换</summary>
        /// <param name="value">数值</param>
        /// <param name="conversionType"></param>
        /// <returns></returns>
        public virtual Object ChangeType(Object value, Type conversionType)
        {
            Type vtype = null;
            if (value != null) vtype = value.GetType();
            if (vtype == conversionType) return value;

            // 可空类型
            var utype = Nullable.GetUnderlyingType(conversionType);
            if (utype != null)
            {
                if (value == null) return null;

                // 时间日期可空处理
                if (value is DateTime dt && dt == DateTime.MinValue) return null;

                conversionType = utype;
            }

            //conversionType = Nullable.GetUnderlyingType(conversionType) ?? conversionType;
            if (conversionType.IsEnum)
            {
                if (vtype == typeof(String))
                    return Enum.Parse(conversionType, (String)value, true);
                else
                    return Enum.ToObject(conversionType, value);
            }

            // 字符串转为货币类型，处理一下
            if (vtype == typeof(String))
            {
                var str = (String)value;
                if (Type.GetTypeCode(conversionType) == TypeCode.Decimal)
                {
                    value = str.TrimStart(new Char[] { '$', '￥' });
                }
                else if (conversionType.As<Type>())
                {
                    return GetType((String)value, false);
                }

                // 字符串转为简单整型，如果长度比较小，满足32位整型要求，则先转为32位再改变类型
                var code = Type.GetTypeCode(conversionType);
                if (code >= TypeCode.Int16 && code <= TypeCode.UInt64 && str.Length <= 10) return Convert.ChangeType(value.ToLong(), conversionType);
            }

            if (value != null)
            {
                // 尝试基础类型转换
                switch (Type.GetTypeCode(conversionType))
                {
                    case TypeCode.Boolean:
                        return value.ToBoolean();
                    case TypeCode.DateTime:
                        return value.ToDateTime();
                    case TypeCode.Double:
                        return value.ToDouble();
                    case TypeCode.Int16:
                        return (Int16)value.ToInt();
                    case TypeCode.Int32:
                        return value.ToInt();
                    case TypeCode.UInt16:
                        return (UInt16)value.ToInt();
                    case TypeCode.UInt32:
                        return (UInt32)value.ToInt();
                    default:
                        break;
                }

                // 支持DateTimeOffset转换
                if (conversionType == typeof(DateTimeOffset)) return value.ToDateTimeOffset();

                if (value is IConvertible) value = Convert.ChangeType(value, conversionType);
            }
            else
            {
                // 如果原始值是null，要转为值类型，则new一个空白的返回
                if (conversionType.IsValueType) value = CreateInstance(conversionType);
            }

            if (conversionType.IsAssignableFrom(vtype)) return value;

            return value;
        }

        /// <summary>获取类型的友好名称</summary>
        /// <param name="type">指定类型</param>
        /// <param name="isfull">是否全名，包含命名空间</param>
        /// <returns></returns>
        public virtual String GetName(Type type, Boolean isfull) => isfull ? type.FullName : type.Name;
        #endregion

        #region 插件
        //private readonly ConcurrentDictionary<Type, ConcurrentDictionary<Type, Boolean>> _as_cache = new ConcurrentDictionary<Type, ConcurrentDictionary<Type, Boolean>>();
        /// <summary>是否子类</summary>
        /// <param name="type"></param>
        /// <param name="baseType"></param>
        /// <returns></returns>
        public Boolean As(Type type, Type baseType)
        {
            if (type == null) return false;
            if (type == baseType) return true;

            // 如果基类是泛型定义，补充完整，例如IList<>
            if (baseType.IsGenericTypeDefinition
                && type.IsGenericType && !type.IsGenericTypeDefinition
                && baseType is TypeInfo inf && inf.GenericTypeParameters.Length == type.GenericTypeArguments.Length)
                baseType = baseType.MakeGenericType(type.GenericTypeArguments);

            if (type == baseType) return true;

            if (baseType.IsAssignableFrom(type)) return true;

            //// 绝大部分子类判断可通过IsAssignableFrom完成，除非其中一方ReflectionOnly
            //if (type.Assembly.ReflectionOnly == baseType.Assembly.ReflectionOnly) return false;

            // 缓存
            //var key = $"{type.FullName}_{baseType.FullName}";
            //if (!_as_cache.TryGetValue(type, out var dic))
            //{
            //    dic = new ConcurrentDictionary<Type, Boolean>();
            //    _as_cache.TryAdd(type, dic);
            //}

            //if (dic.TryGetValue(baseType, out var rs)) return rs;
            var rs = false;

            //// 接口
            //if (baseType.IsInterface)
            //{
            //    if (type.GetInterface(baseType.FullName) != null)
            //        rs = true;
            //    else if (type.GetInterfaces().Any(e => e.IsGenericType && baseType.IsGenericTypeDefinition ? e.GetGenericTypeDefinition() == baseType : e == baseType))
            //        rs = true;
            //}

            //// 判断是否子类时，支持只反射加载的程序集
            //if (!rs && type.Assembly.ReflectionOnly)
            //{
            //    // 反射加载时，需要特殊处理接口
            //    //if (baseType.IsInterface && type.GetInterface(baseType.Name) != null) return true;
            //    while (!rs && type != typeof(Object))
            //    {
            //        if (type.FullName == baseType.FullName &&
            //            type.AssemblyQualifiedName == baseType.AssemblyQualifiedName)
            //            rs = true;
            //        type = type.BaseType;
            //    }
            //}

            //dic.TryAdd(baseType, rs);

            return rs;
        }

        /// <summary>在指定程序集中查找指定基类的子类</summary>
        /// <param name="asm">指定程序集</param>
        /// <param name="baseType">基类或接口，为空时返回所有类型</param>
        /// <returns></returns>
        public virtual IEnumerable<Type> GetSubclasses(Assembly asm, Type baseType)
        {
            if (asm == null) throw new ArgumentNullException(nameof(asm));
            if (baseType == null) throw new ArgumentNullException(nameof(baseType));

            return AssemblyX.Create(asm).FindPlugins(baseType);
        }

        /// <summary>在所有程序集中查找指定基类或接口的子类实现</summary>
        /// <param name="baseType">基类或接口</param>
        /// <returns></returns>
        public virtual IEnumerable<Type> GetAllSubclasses(Type baseType)
        {
            // 不支持isLoadAssembly
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in GetSubclasses(asm, baseType))
                {
                    yield return type;
                }
            }
        }

        ///// <summary>在所有程序集中查找指定基类或接口的子类实现</summary>
        ///// <param name="baseType">基类或接口</param>
        ///// <param name="isLoadAssembly">是否加载为加载程序集</param>
        ///// <returns></returns>
        //public virtual IEnumerable<Type> GetAllSubclasses(Type baseType, Boolean isLoadAssembly)
        //{
        //    //// 不支持isLoadAssembly
        //    //foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        //    //{
        //    //    foreach (var type in GetSubclasses(asm, baseType))
        //    //    {
        //    //        yield return type;
        //    //    }
        //    //}
        //    return AssemblyX.FindAllPlugins(baseType, isLoadAssembly);
        //}
        #endregion
    }
    #endregion
}
