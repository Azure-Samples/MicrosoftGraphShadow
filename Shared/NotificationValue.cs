using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public class NotificationValue
    {
        public ChangeNotification[] value { get; set; } = new ChangeNotification[0];
    }
    public class ChangeNotification
    {
        public NotificationValue value { get; set; } = new NotificationValue();
        public string changeType { get; set; } = string.Empty;// for example "updated"
        public string clientState { get; set; } = string.Empty; //client secret
        public string resource { get; set; } = string.Empty; // for example"resource":"Users/bf8f6956-5bd3-49cc-9752-ed91d4d2da5f"

        public DateTime subscriptionExpirationDateTime { get; set; } // for example "2023-10-08T02:55:35.8245965-07:00"
        public string subscriptionId { get; set; } = string.Empty; // for example "41aaf9e1-f5a2-456a-ab5a-eed6e27eaaed"
        public string tenantId { get; set; } = string.Empty;
        ResourceData resourceData { get; set; } = new ResourceData();
    }
    public class ResourceData
    {
        public string odataType { get; set; }  = string.Empty;//"#Microsoft.Graph.User"
        public string odataId { get; set; } = string.Empty;    
        public string id { get; set; }=string.Empty;
        public string organizationId { get; set; } = string.Empty; 
    }
}
