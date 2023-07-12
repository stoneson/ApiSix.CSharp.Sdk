using ApiSix.CSharp.model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace ApiSix.CSharp
{
    public static class JsonHelper
    {
        static Newtonsoft.Json.Converters.IsoDateTimeConverter timeFormat = new Newtonsoft.Json.Converters.IsoDateTimeConverter() { DateTimeFormat = "yyyy-MM-dd HH:mm:ss" };
        static JsonHelper()
        {
            Newtonsoft.Json.JsonSerializerSettings setting = new Newtonsoft.Json.JsonSerializerSettings();
            JsonConvert.DefaultSettings = new Func<JsonSerializerSettings>(() =>
            {
                //日期类型默认格式化处理
                setting.DateFormatHandling = Newtonsoft.Json.DateFormatHandling.MicrosoftDateFormat;
                setting.DateFormatString = "yyyy-MM-dd HH:mm:ss";
                //setting.TypeNameHandling = TypeNameHandling.Auto;
                //空值处理
                setting.NullValueHandling = NullValueHandling.Ignore;

                //高级用法九中的Bool类型转换 设置
                //setting.Converters.Add(new BoolConvert("是,否"));

                return setting;
            });
        }
        public static byte[] SerializeObject(this object value)
        {
            if (value == null) return null;

            return UTF8Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value, timeFormat));
        }

        public static object DeserializeObject(this byte[] value, Type type)
        {
            if (value.Length == 0)
            {
                return null;
            }
            return JsonConvert.DeserializeObject(UTF8Encoding.UTF8.GetString(value), type, timeFormat);
        }
        public static T ToObject<T>(this string value)
        {
            return DeserializeObjectByJson<T>(value);
        }
        public static object ToObject(this string value, Type type)
        {
            return DeserializeObjectByJson(value, type);
        }
        public static object DeserializeObjectByJson(this string value, Type type)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            ////使用另一个序列化器来进行反序列化
            //JsonSerializer serializer = new JsonSerializer();
            ////把我们自定义的JsonConverter放进序列化器(相当于告诉序列化器该怎么序列化)
            //serializer.Converters.Add(new PluginJsonConverter2());
            ////进行反序列化
            //JsonTextReader reader = new JsonTextReader(new StringReader(value));
            //var result = serializer.Deserialize(reader, type);
            return JsonConvert.DeserializeObject(value, type, timeFormat, new PluginJsonConverter());
        }
        public static T DeserializeObjectByJson<T>(this string value)
        {
            return (T)DeserializeObjectByJson(value, typeof(T));
        }
        public static string ToJson(this object value)
        {
            return SerializeObjectToJson(value);
        }
        public static string SerializeObjectToJson(this object value)
        {
            if (value == null) return "";
            return JsonConvert.SerializeObject(value, timeFormat);
        }
        public static T DeserializeObject<T>(this byte[] value)
        {
            if (value.Length == 0)
            {
                return default(T);
            }
            return (T)DeserializeObject(value, typeof(T));
        }

        public static string SerializeDictionaryToJsonString<TKey, TValue>(this Dictionary<TKey, TValue> dict)
        {
            if (dict.Count == 0)
                return "";

            string jsonStr = JsonConvert.SerializeObject(dict);
            return jsonStr;
        }
        /// <summary>Json类型对象转换实体类</summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T ConvertObject<T>(this Object obj)
        {
            if (obj == null) return default;
            if (obj is T t) return t;
            if (obj.GetType().As<T>()) return (T)obj;

            var objJs = obj.ToJson();
            return objJs.DeserializeObjectByJson<T>();
        }
        /// <summary>
        /// 将json字符串反序列化为字典类型
        /// </summary>
        /// <typeparam name="TKey">字典key</typeparam>
        /// <typeparam name="TValue">字典value</typeparam>
        /// <param name="jsonStr">json字符串</param>
        /// <returns>字典数据</returns>
        public static Dictionary<TKey, TValue> DeserializeStringToDictionary<TKey, TValue>(this string jsonStr)
        {
            if (string.IsNullOrWhiteSpace(jsonStr))
                return new Dictionary<TKey, TValue>();

            Dictionary<TKey, TValue> jsonDict = JsonConvert.DeserializeObject<Dictionary<TKey, TValue>>(jsonStr);

            return jsonDict;

        }
        public static PropertyInfo GetJsonProperty(this Type type, string name)
        {
            if (type == null || string.IsNullOrWhiteSpace(name)) return null;
            var property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property == null)
            {
                // 获取所有属性
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                // 如果实例公开属性为空，则返回空字典
                if (properties.Length == 0) return null;
                foreach (var prop in properties)
                {
                    var ja = prop.GetCustomAttribute<JsonPropertyAttribute>();
                    if (ja != null && ja.PropertyName.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        return prop;
                    }
                }
            }
            return property;
        }
        /// <summary>
        /// 将对象转字典集合
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static T ToEntity<T>(this JObject jobj) where T : class
        {
            if (jobj == null)
                return default(T);

            var type = typeof(T);
            if (!(type.IsClass || type.IsAnonymous())) return default(T);

            // 获取所有属性
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            // 如果实例公开属性为空，则返回空字典
            if (properties.Length == 0) return default(T);

            T entity = Activator.CreateInstance(type) as T;
            if (entity == null) return default(T);
            foreach (var item in jobj)
            {
                var val = item.Value;
                if (val != null)
                {
                    var property = type.GetJsonProperty(item.Key);
                    if (property == null) continue;
                    try
                    {
                        if (val is Newtonsoft.Json.Linq.JObject _sjObject)
                        {
                            var objval = _sjObject.ToObject(property.PropertyType);
                            property.SetValue(entity, objval, null);
                        }
                        else if (val is Newtonsoft.Json.Linq.JArray _ajObject)
                        {
                            var objval = _ajObject.ToObject(property.PropertyType);
                            property.SetValue(entity, objval, null);
                        }
                        else if (val is Newtonsoft.Json.Linq.JValue valo)
                        {
                            var objval = valo != null ? valo.Value : null;
                            property.SetValue(entity, Convert.ChangeType(objval, property.PropertyType), null);
                        }
                        else { property.SetValue(entity, Convert.ChangeType(val, property.PropertyType), null); }
                    }
                    catch { }
                }
            }
            return entity;
        }
        /// <summary>
        /// 判断是否是匿名类型
        /// </summary>
        /// <param name="obj">对象</param>
        /// <returns></returns>
        public static bool IsAnonymous(this object obj)
        {
            var type = obj.GetType();

            return Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false)
                   && type.IsGenericType && type.Name.Contains("AnonymousType")
                   && (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"))
                   && type.Attributes.HasFlag(TypeAttributes.NotPublic);
        }
    }
}
