using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiSix.Sharp
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
}
