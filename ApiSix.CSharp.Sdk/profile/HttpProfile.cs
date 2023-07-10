using System;

namespace ApiSix.CSharp
{
    public class HttpProfile
    {
        public const String REQ_HTTPS = "https://";
        public const String REQ_HTTP = "http://";
        public const String REQ_POST = "POST";
        public const String REQ_GET = "GET";
        public const String REQ_PUT = "PUT";
        public const String REQ_DELETE = "DELETE";
        public const String REQ_PATCH = "PATCH";

        public String Endpoint { get; set; }

        public String Protocol { get; set; }

        public int ReadTimeou { get; set; }

        public int WriteTimeout { get; set; }

        public int ConnTimeout { get; set; }


        public HttpProfile()
        {
            this.Endpoint = null;
            this.Protocol = HttpProfile.REQ_HTTP;
            this.ReadTimeou = 10;
            this.WriteTimeout = 10;
            this.ConnTimeout = 30;
        }

    }
}
