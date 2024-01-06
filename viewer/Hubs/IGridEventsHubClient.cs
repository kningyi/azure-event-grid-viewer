using System.Threading.Tasks;
using viewer.Models;

namespace viewer.Hubs
{
    public interface IGridEventsHubClient
    {
        Task GridUpdate(GridUpdateModel data); 
        Task Identification(IdentityModel identity);
    }
}
