using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace Shared
{
    public class UserEntity : BaseEntity
    {
        public string UserPrincipalName { get; set; } = String.Empty;
        public string GivenName { get; set; } = String.Empty;
        public string SurName { get; set; } = String.Empty;
        public string MobilePhone { get; set; } = String.Empty;
    }
}