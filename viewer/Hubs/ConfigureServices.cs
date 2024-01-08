using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace viewer.Hubs
{
    public static class ConfigureServices
    {
        public static WebApplicationBuilder AddFileWatcher(this WebApplicationBuilder builder)
        {
            builder.Services.AddScoped<AbstractGridEventsHub, GridEventsHub>();
            builder.Services.AddScoped<IGridEventHubService, GridEventHubService>();

            return builder;
        }

        public static IEndpointRouteBuilder MapFileWatcherHub(this IEndpointRouteBuilder endpoints, string url)
        {
            endpoints.MapHub<AbstractGridEventsHub>(url);

            return endpoints;
        }
    }
}
