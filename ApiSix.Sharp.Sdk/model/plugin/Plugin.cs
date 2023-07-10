using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace ApiSix.Sharp.model
{
    //[JsonConverter(typeof(ClientJsonConverter2<Plugin>))]
    public class Plugin : BaseModel
    {
    }

    class PluginJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            //如果遇到了Plugin，我们才进行转换
            if (objectType.FullName == typeof(Plugin).FullName)
            {
                return true;
            }
            return false;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            //再判断一次，如果是Plugin接口，那么我们指定具体实现类型
            if (objectType.FullName == typeof(Plugin).FullName)
            {
                //var jobj = serializer.Deserialize<JObject>(reader);
                // if (jobj != null)
                //{
                var Path = reader.Path;//.Substring(reader.Path.LastIndexOf(".") + 1);
                //traffic
                if (Path.EndsWith("limit-count", StringComparison.OrdinalIgnoreCase))
                    return serializer.Deserialize<LimitCount>(reader); //jobj.ToEntity<LimitCount>();
                else if (Path.EndsWith("limit-conn", StringComparison.OrdinalIgnoreCase))
                    return serializer.Deserialize<LimitConn>(reader);
                else if (Path.EndsWith("Limit-Req", StringComparison.OrdinalIgnoreCase))
                    return serializer.Deserialize<LimitReq>(reader); //jobj.ToEntity<LimitReq>();
                else if (Path.EndsWith("api-breaker", StringComparison.OrdinalIgnoreCase))
                    return serializer.Deserialize<apiBreaker>(reader);

                //auth
                else if (Path.EndsWith("Basic-Auth", StringComparison.OrdinalIgnoreCase))
                    return serializer.Deserialize<BasicAuth>(reader); //jobj.ToEntity<BasicAuth>();
                else if (Path.EndsWith("Key-Auth", StringComparison.OrdinalIgnoreCase))
                    return serializer.Deserialize<KeyAuth>(reader);
                else if (Path.EndsWith("jwt-auth", StringComparison.OrdinalIgnoreCase))
                    return serializer.Deserialize<jwtAuth>(reader);

                //tracers
                else if (Path.EndsWith("Zipkin", StringComparison.OrdinalIgnoreCase))
                    return serializer.Deserialize<ZipkinPlugin>(reader); //jobj.ToEntity<Zipkin>();
                else if (Path.EndsWith("skywalking", StringComparison.OrdinalIgnoreCase))
                    return serializer.Deserialize<SkywalkingPlugin>(reader);

                //general
                else if (Path.EndsWith("Redirect", StringComparison.OrdinalIgnoreCase))
                    return serializer.Deserialize<Redirect>(reader); //jobj.ToEntity<Redirect>();

                //security
                else if (Path.EndsWith("Cors", StringComparison.OrdinalIgnoreCase))
                    return serializer.Deserialize<httpCors>(reader); //jobj.ToEntity<httpCors>();
                else if (Path.EndsWith("ip-restriction", StringComparison.OrdinalIgnoreCase))
                    return serializer.Deserialize<IpRestriction>(reader); //jobj.ToEntity<IpRestriction>();
                else if (Path.EndsWith("uri-blocker", StringComparison.OrdinalIgnoreCase))
                    return serializer.Deserialize<uriBlocker>(reader);

                //transform
                else if (Path.EndsWith("grpc-transcode", StringComparison.OrdinalIgnoreCase))
                    return serializer.Deserialize<grpcTranscode>(reader);
                else if (Path.EndsWith("Proxy-Rewrite", StringComparison.OrdinalIgnoreCase))
                    return serializer.Deserialize<ProxyRewrite>(reader);
                else if (Path.EndsWith("response-rewrite", StringComparison.OrdinalIgnoreCase))
                    return serializer.Deserialize<ResponseRewrite>(reader); 
                //}
                //return serializer.Deserialize<Plugin>(reader);
            }
            return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            //按正常流程序列化
            serializer.Serialize(writer, value);
        }
    }

    //class ClientJsonConverter2<TConcrete> : JsonConverter
    //{
    //    public override bool CanConvert(Type objectType)
    //    {
    //        //我们能转换任何东西
    //        return true;
    //    }

    //    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    //    {//显性指定要转换的实现类
    //        var jobj = serializer.Deserialize<JObject>(reader);
    //        if (jobj != null)
    //        {
    //            var Path = reader.Path;//.Substring(reader.Path.LastIndexOf(".") + 1);
    //            if (Path.EndsWith("limit-count", StringComparison.OrdinalIgnoreCase))
    //                return jobj.ToEntity<LimitCount>();
    //            else if (Path.EndsWith("Basic-Auth", StringComparison.OrdinalIgnoreCase))
    //                return jobj.ToEntity<BasicAuth>();
    //            else if (Path.EndsWith("Ip-Restriction", StringComparison.OrdinalIgnoreCase))
    //                return jobj.ToEntity<IpRestriction>();
    //            else if (Path.EndsWith("Limit-Req", StringComparison.OrdinalIgnoreCase))
    //                return jobj.ToEntity<LimitReq>();
    //            else if (Path.EndsWith("Proxy-Rewrite", StringComparison.OrdinalIgnoreCase))
    //                return jobj.ToEntity<ProxyRewrite>();
    //            else if (Path.EndsWith("Zipkin", StringComparison.OrdinalIgnoreCase))
    //                return jobj.ToEntity<Zipkin>();
    //            else if (Path.EndsWith("Redirect", StringComparison.OrdinalIgnoreCase))
    //                return jobj.ToEntity<Redirect>();
    //            else if (Path.EndsWith("Cors", StringComparison.OrdinalIgnoreCase))
    //                return jobj.ToEntity<httpCors>();
    //        }
    //        return null;
    //    }

    //    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    //    {
    //        //默认序列化流程
    //        serializer.Serialize(writer, value);
    //    }

    //}

}
