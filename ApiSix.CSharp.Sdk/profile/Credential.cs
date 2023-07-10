using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiSix.CSharp
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
