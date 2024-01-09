using Newtonsoft.Json;
using System;

namespace viewer.Models
{
    public class ScanResult
    {
        [JsonProperty("correlationId")]
        public string CorrelationId { get; set; }

        [JsonProperty("blobUri")]
        public string Url { get; set; }

        [JsonProperty("eTag")]
        public string ETag { get; set; }

        [JsonProperty("scanFinishedTimeUtc")]
        public DateTime? FinishedTime { get; set; }

        [JsonProperty("scanResultType")]
        public string Result { get; set; }
    }
}
