﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiSix.CSharp
{
    public class ApisixSDKExcetion : Exception
    {
        public int errorCode { get; } = 600;

        public ApisixSDKExcetion(String message) : this(message, 600)
        {
        }
        public ApisixSDKExcetion(Exception ex) : base(ex?.Message, ex)
        {
        }
        public ApisixSDKExcetion(String message, int errorCode) : base(message)
        {
            this.errorCode = errorCode;
        }

        public int getErrorCode()
        {
            return errorCode;
        }
        public override string ToString()
        {
            return "[ApisixSDKExcetion]"
                     + "message:"
                     + this.Message
                     + " errorCode:"
                     + this.getErrorCode();
        }
    }

    /// <summary>X组件异常</summary>
    [Serializable]
    public class XException : Exception
    {
        #region 构造
        /// <summary>初始化</summary>
        public XException() { }

        /// <summary>初始化</summary>
        /// <param name="message"></param>
        public XException(String message) : base(message) { }

        /// <summary>初始化</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public XException(String format, params Object[] args) : base(String.Format(format, args)) { }

        /// <summary>初始化</summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public XException(String message, Exception innerException) : base(message, innerException) { }

        /// <summary>初始化</summary>
        /// <param name="innerException"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public XException(Exception innerException, String format, params Object[] args) : base(String.Format(format, args), innerException) { }

        /// <summary>初始化</summary>
        /// <param name="innerException"></param>
        public XException(Exception innerException) : base((innerException?.Message), innerException) { }

        ///// <summary>初始化</summary>
        ///// <param name="info"></param>
        ///// <param name="context"></param>
        //protected XException(SerializationInfo info, StreamingContext context) : base(info, context) { }
        #endregion
    }
}
