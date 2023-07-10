using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiSix.Sharp
{
    public interface Profile
    {
         Credential Credential { get; }

         String Version { get; }

         String Endpoint { get; }

         HttpProfile HttpProfile { get; }
    }
    public class DefaultProfile : Profile
    {
        private DefaultProfile(String endpoint, String version, Credential credential)
        {
            this.Credential = credential;
            this.Endpoint = endpoint;
            this.Version = version;
            this.HttpProfile = new HttpProfile();
        }

        public static DefaultProfile getProfile(String endpoint, String version, Credential credential)
        {
            var profile = new DefaultProfile(endpoint, version, credential);
            return profile;
        }
        public static DefaultProfile getProfile(String endpoint, String version, string apiKey)
        {
            var profile = new DefaultProfile(endpoint, version, new DefaultCredential(apiKey));
            return profile;
        }

        public Credential Credential { get; }

        public String Version { get; }

        public String Endpoint { get; }

        public HttpProfile HttpProfile { get; }
    }
}
