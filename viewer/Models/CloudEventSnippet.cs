using Newtonsoft.Json;

namespace viewer.Models
{
    public class CloudEventSnippet : IEventSnippet
    {
        [JsonProperty("type")]
        public string Type { get; set; }
    }
}
