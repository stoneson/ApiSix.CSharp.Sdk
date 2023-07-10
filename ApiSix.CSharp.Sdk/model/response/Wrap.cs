using Newtonsoft.Json;

namespace ApiSix.Sharp.model
{
    public class Wrap<T>
    {
        [JsonProperty("node")]
        public T node { get; set; }
    }
}
