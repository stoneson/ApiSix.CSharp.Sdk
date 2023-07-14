using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiSix.CSharp.model
{
    public class BaseBody : BaseModel
    {
        /// <summary>
        /// id
        /// </summary>
        [JsonProperty("id")]
        public String id { get; set; }

        /// <summary>
        /// epoch 时间戳，单位为秒，如果不指定则自动创建
        /// </summary>
        [JsonProperty("create_time")]
        public long? createTime { get; set; }
        /// <summary>
        /// epoch 时间戳，单位为秒，如果不指定则自动创建
        /// </summary>
        [JsonProperty("update_time")]
        public long? updateTime { get; set; }
    }
}
