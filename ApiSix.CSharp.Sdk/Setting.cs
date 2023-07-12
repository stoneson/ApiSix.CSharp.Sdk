using System;

namespace ApiSix.CSharp
{
    /// <summary>配置</summary>
    [Config("ApiSix.json","json")]
    public class Setting : Config<Setting>
    {
        #region 属性
        /// <summary>
        /// apiSix admin key
        /// </summary>
        public string ApiKey { get; set; } = "edd1c9f034335f136f87ad84b625c8f1";
        /// <summary>
        /// apiSix 版本
        /// </summary>
        public String Version { get; set; } = "3.3.0";
        /// <summary>
        /// apiSix admin url
        /// </summary>
        public String Endpoint { get; set; } = "127.0.0.1:9180";
      
        #endregion
    }
}
