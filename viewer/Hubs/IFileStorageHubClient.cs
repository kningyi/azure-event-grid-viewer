using System.Threading.Tasks;
using viewer.Models;

namespace viewer.Hubs
{
    public interface IFileStorageHubClient
    {
        Task GridUpdate(GridUpdateModel data); 
        Task Subscription(HubSubscriptionDto identity);
    }
}
