
using Newtonsoft.Json;
using System;

namespace viewer.Models
{
    // Reference: https://github.com/cloudevents/spec/tree/v1.0-rc1 

    public class CloudEvent<T> : IEvent<T> where T : class
    {
        [JsonProperty("specversion")]
        public string SpecVersion { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("subject")]
        public string Subject { get; set; }

        [JsonProperty("time")]
        public DateTime Time { get; set; }

        [JsonProperty("data")]
        public T Data { get; set; }
    }
}
