using System.Threading.Tasks;

namespace viewer.Hubs
{
    public interface IGridEventsHubClient
    {
        Task GridUpdate(params string[] data);
    }
}
