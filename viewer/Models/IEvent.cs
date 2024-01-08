using System;

namespace viewer.Models
{
    public interface IEvent<T> : IEventSnippet where T : class
    {
        string Id { get; set; }
        string Source { get; set; }
        string Subject { get; set; }
        DateTime Time { get; set; }
        T Data { get; set; }
    }
}
