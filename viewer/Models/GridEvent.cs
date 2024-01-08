using Newtonsoft.Json;
using System;

namespace viewer.Models
{
    public class GridEvent<T> : GridEventSnippet, IEvent<T> where T: class
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("topic")]
        public string Source { get; set; }

        [JsonProperty("subject")]
        public string Subject {get; set; }

        [JsonProperty("eventTime")]
        public DateTime Time { get; set; }

        [JsonProperty("data")]
        public T Data { get; set; }
    }
}
