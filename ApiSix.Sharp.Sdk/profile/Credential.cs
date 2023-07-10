using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiSix.Sharp
{
    public interface Credential
    {
        string Token { get; }
    }
    public class DefaultCredential : Credential
    {
        public DefaultCredential(String token)
        {
            this.Token = token;
        }
        public string Token { get; }
    }
   
}
