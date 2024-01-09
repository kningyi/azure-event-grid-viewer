using System;

namespace viewer.Models
{
    public class ScanResultDto
    {
        public string Id { get; set; }
        public string ETag { get; set; }
        public string Url { get; set; }
        public DateTime? FinishedTime { get; set; }
        public bool Passed { get; set; }
    }
}
