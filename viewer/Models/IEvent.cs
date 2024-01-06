using System;

namespace viewer.Models
{
    public interface IEvent<T> where T : class
    {
        string Id { get; set; }
        string Type { get; set; }
        string Source { get; set; }
        string Subject { get; set; }
        DateTime Time { get; set; }
        T Data { get; set; }
    }
}
