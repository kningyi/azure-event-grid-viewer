using System.Security.Principal;

namespace viewer.Models
{
    public class IdentityModel
    {
        public string SessionId { get; set; }
        public string ConnectionId { get; set; }
        public IIdentity User { get; set; }
    }
}
