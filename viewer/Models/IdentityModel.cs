using System.Collections.Generic;
using System.Security.Principal;

namespace viewer.Models
{
    public class IdentityModel
    {
        public string UserId { get; set; }
        public string ConnectionId { get; set; }
        public IIdentity User { get; set; }
        public IDictionary<object, object?> Items { get; set; }
    }
}
