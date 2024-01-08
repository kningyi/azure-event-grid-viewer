using Newtonsoft.Json;

namespace viewer.Models
{
    public class GridEventSnippet : IEventSnippet
    {
        [JsonProperty("eventType")]
        public string Type { get; set; }
    }
}
