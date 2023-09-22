using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Sinya.Tera.Shared.Schema
{
    public class ProtocolAndOpCode
    {
        [JsonProperty("map")]
        public Dictionary<string, Dictionary<string, int>> Maps { get; set; } = new();

        [JsonProperty("protocol")]
        public Dictionary<string, string> Protocol { get; set; } = new();
    }
    
}
