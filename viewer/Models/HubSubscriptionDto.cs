using System.Security.Principal;

namespace viewer.Models
{
    public class HubSubscriptionDto
    {
        public string ConnectionId { get; set; }
        public string FolderPath { get; set; }
        public string UserId { get; set; }
        public IIdentity User { get; set; }
    }
}
