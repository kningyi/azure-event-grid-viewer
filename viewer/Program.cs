using Microsoft.AspNetCore.Builder;
using viewer.Hubs;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddControllers();
//builder.Services.AddSignalR();

builder.Services.AddSignalR(options => {
        options.EnableDetailedErrors = true; 
    }
);

builder.Services.AddScoped<AbstractGridEventsHub, GridEventsHub>();
builder.Services.AddScoped<IGridEventHubService, GridEventHubService>();

var app = builder.Build();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCookiePolicy();

app.UseEndpoints(endpoints =>
{
    endpoints.MapHub<AbstractGridEventsHub>("/hubs/gridevents");
    endpoints.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");
});

app.Run();

