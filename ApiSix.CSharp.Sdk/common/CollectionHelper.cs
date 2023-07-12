using System.Collections.Concurrent;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using Newtonsoft.Json.Linq;
using System.Dynamic;
using Microsoft.VisualBasic;
using Newtonsoft.Json;

namespace ApiSix.CSharp
{
    /// <summary>集合扩展</summary>
    public static class CollectionHelper
    {
        /// <summary>集合转为数组，加锁确保安全</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static T[] ToArray<T>(this ICollection<T> collection)
        {
            if (collection == null) return null;

            lock (collection)
            {
                var count = collection.Count;
                if (count == 0) return new T[0];

                var arr = new T[count];
                collection.CopyTo(arr, 0);

                return arr;
            }
        }

        /// <summary>集合转为数组</summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="collection"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static IList<TKey> ToKeyArray<TKey, TValue>(this IDictionary<TKey, TValue> collection, Int32 index = 0)
        {
            if (collection == null) return null;

            if (collection is ConcurrentDictionary<TKey, TValue> cdiv) return cdiv.Keys as IList<TKey>;

            if (collection.Count == 0) return new TKey[0];
            lock (collection)
            {
                var arr = new TKey[collection.Count - index];
                collection.Keys.CopyTo(arr, index);
                return arr;
            }
        }

        /// <summary>集合转为数组</summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="collection"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static IList<TValue> ToValueArray<TKey, TValue>(this IDictionary<TKey, TValue> collection, Int32 index = 0)
        {
            if (collection == null) return null;

            if (collection is ConcurrentDictionary<TKey, TValue> cdiv) return cdiv.Values as IList<TValue>;

            if (collection.Count == 0) return new TValue[0];
            lock (collection)
            {
                var arr = new TValue[collection.Count - index];
                collection.Values.CopyTo(arr, index);
                return arr;
            }
        }

        /// <summary>目标匿名参数对象转为名值字典</summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static IDictionary<String, Object> ToDictionary(this Object source, IDictionary<string, object> dic = null)
        {
            //!! 即使传入为空，也返回字典，而不是null，避免业务层需要大量判空
            //if (target == null) return null;
            if (dic == null)
                dic = new NullableDictionary<String, Object>(StringComparer.OrdinalIgnoreCase);

            if (source != null)
            {
                // 修正字符串字典的支持问题
                if (source is IDictionary<String, Object> dicr)//Dictionary
                {
                    foreach (var item in dicr)
                    {
                        dic[item.Key] = item.Value;
                    }
                }
                else if (source is IDictionary dic2)
                {
                    foreach (DictionaryEntry item in dic2)
                    {
                        dic[item.Key + ""] = item.Value;
                    }
                }
                else if (source is Newtonsoft.Json.Linq.JObject _jObject)//JSON
                {
                    foreach (var item in _jObject)
                    {
                        //if (item.Value is Newtonsoft.Json.Linq.JValue)
                        //{
                        //    var val = item.Value as Newtonsoft.Json.Linq.JValue;
                        //    dic[item.Key] = val != null ? val.Value : null;
                        //}
                        //else { dic[item.Key] = item.Value; }
                        var val = item.Value;
                        if (val != null)
                        {
                            if (val is Newtonsoft.Json.Linq.JObject _sjObject)
                            {
                                dic[item.Key] = ToObject(_sjObject);
                            }
                            else if (val is Newtonsoft.Json.Linq.JArray _ajObject)
                            {
                                dic[item.Key] = ToArray(_ajObject);
                            }
                            else if (val is Newtonsoft.Json.Linq.JValue valo)
                            {
                                dic[item.Key] = valo != null ? valo.Value : null;
                            }
                            else { dic[item.Key] = val; }
                            continue;
                        }
                        dic[item.Key] = null;
                    }
                }
                else if (source is System.Dynamic.ExpandoObject _eObject)//ExpandoObject
                {
                    foreach (var item in _eObject)
                    {
                        dic[item.Key] = item.Value;
                    }
                }
                else if (source is System.Data.DataTable _dtObject)
                {
                    var list = new List<IDictionary<String, Object>>();
                    foreach (System.Data.DataRow row in _dtObject.Rows)
                    {
                        var dicdr = new Dictionary<String, Object>();
                        for (int j = 0; j < row.Table.Columns.Count; j++)
                        {
                            dic[row.Table.Columns[j].ColumnName] = row[j];
                        }
                        list.Add(dicdr);
                    }
                    dic[_dtObject.TableName] = list;
                }
                else if (source is System.Data.DataRow _drObject)
                {
                    for (int j = 0; j < _drObject.Table.Columns.Count; j++)
                    {
                        dic[_drObject.Table.Columns[j].ColumnName] = _drObject[j];
                    }
                }
                else if (source is System.Data.DataRowView _drvObject)
                {
                    for (int j = 0; j < _drvObject.Row.Table.Columns.Count; j++)
                    {
                        dic[_drvObject.Row.Table.Columns[j].ColumnName] = _drvObject.Row[j];
                    }
                }
                else//实体
                {
                    // 如果不是类类型或匿名类型，则返回空字典
                    var type = source.GetType();
                    if (!(type.IsClass || type.IsAnonymous())) return dic;

                    // 获取所有属性
                    var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    // 如果实例公开属性为空，则返回空字典
                    if (properties.Length == 0) return dic;
                    // 遍历公开属性
                    foreach (var property in properties)
                    {
                        var value = property.GetValue(source, null);
                        dic[property.Name] = value;
                    }
                    var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                    // 如果实例公开属性为空，则返回空字典
                    if (fields.Length == 0) return dic;
                    // 遍历公开属性
                    foreach (var field in fields)
                    {
                        var value = field.GetValue(source);
                        dic[field.Name] = value;
                    }
                }
            }

            return dic;
        }

        /// <summary>
        /// Newtonsoft.Json.Linq.JObject对象转Object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static IDictionary<string, object> ToObject(this Newtonsoft.Json.Linq.JObject obj)
        {
            if (obj == null)
                return new NullableDictionary<String, Object>(StringComparer.OrdinalIgnoreCase);

            var dy = new NullableDictionary<String, Object>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in obj)
            {
                var val = item.Value;
                if (val != null)
                {
                    if (val is Newtonsoft.Json.Linq.JObject _sjObject)
                    {
                        dy[item.Key] = ToObject(_sjObject);
                    }
                    else if (val is Newtonsoft.Json.Linq.JArray _ajObject)
                    {
                        dy[item.Key] = ToArray(_ajObject);
                    }
                    else if (val is Newtonsoft.Json.Linq.JValue valo)
                    {
                        dy[item.Key] = valo != null ? valo.Value : null;
                    }
                    else { dy[item.Key] = val; }
                    continue;
                }
                dy[item.Key] = null;
            }
            return dy;
        }
        /// <summary>
        /// Newtonsoft.Json.Linq.JArray 对象转dynamic
        /// </summary>
        /// <param name="jarray"></param>
        /// <returns></returns>
        public static IList<object> ToArray(this Newtonsoft.Json.Linq.JArray jarray)
        {
            if (jarray == null)
                return new List<object>();

            var expando = new List<object>();
            foreach (var item in jarray)
            {
                if (item.HasValues)
                {
                    var val = item.Value<object>();
                    if (val != null)
                    {
                        if (val is Newtonsoft.Json.Linq.JObject _sjObject)
                        {
                            expando.Add(ToObject(_sjObject));
                        }
                        else if (val is Newtonsoft.Json.Linq.JArray _ajObject)
                        {
                            expando.Add(ToArray(_ajObject));
                        }
                        else if (val is Newtonsoft.Json.Linq.JValue valo)
                        {
                            expando.Add(valo != null ? valo.Value : null);
                        }
                        else
                        {
                            expando.Add(val);
                        }
                        continue;
                    }
                }
            }
            return expando;
        }

        /// <summary>合并字典参数</summary>
        /// <param name="dic">字典</param>
        /// <param name="target">目标对象</param>
        /// <param name="overwrite">是否覆盖同名参数</param>
        /// <param name="excludes">排除项</param>
        /// <returns></returns>
        public static IDictionary<String, Object> Merge(this IDictionary<String, Object> dic, Object target, Boolean overwrite = true, String[] excludes = null)
        {
            var exs = excludes != null ? new HashSet<String>(excludes, StringComparer.OrdinalIgnoreCase) : null;
            foreach (var item in target.ToDictionary())
            {
                if (exs == null || !exs.Contains(item.Key))
                {
                    if (overwrite || !dic.ContainsKey(item.Key)) dic[item.Key] = item.Value;
                }
            }

            return dic;
        }

        /// <summary>转为可空字典</summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="collection"></param>
        /// <param name="comparer"></param>
        /// <returns></returns>
        public static IDictionary<TKey, TValue> ToNullable<TKey, TValue>(this IDictionary<TKey, TValue> collection, IEqualityComparer<TKey> comparer = null)
        {
            if (collection == null) return null;

            if (collection is NullableDictionary<TKey, TValue> dic && (comparer == null || dic.Comparer == comparer)) return dic;

            return new NullableDictionary<TKey, TValue>(collection, comparer);
        }

        /// <summary>从队列里面获取指定个数元素</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">消费集合</param>
        /// <param name="count">元素个数</param>
        /// <returns></returns>
        public static IEnumerable<T> Take<T>(this Queue<T> collection, Int32 count)
        {
            if (collection == null) yield break;

            while (count-- > 0 && collection.Count > 0)
            {
                yield return collection.Dequeue();
            }
        }

        /// <summary>从消费集合里面获取指定个数元素</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">消费集合</param>
        /// <param name="count">元素个数</param>
        /// <returns></returns>
        public static IEnumerable<T> Take<T>(this IProducerConsumerCollection<T> collection, Int32 count)
        {
            if (collection == null) yield break;

            while (count-- > 0 && collection.TryTake(out var item))
            {
                yield return item;
            }
        }

        /// <summary>分析Json字符串得到字典</summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static IDictionary<String, Object> DecodeJson(this String json)
        {
            var jobj = json.ToObject<object>();
            return jobj.ToDictionary();
        }
    }
}
