using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace viewer.Hubs
{
    public interface IGridEventHubService
    {
        Task Broadcast<T>(string type, string subject = null, T content = null, HttpRequest request = null) where T : class;
        Task<bool> Process(string jsonContent, HttpRequest request);
    }
}
