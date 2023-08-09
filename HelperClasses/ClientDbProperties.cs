using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhatsappConnector.HelperClasses
{
    public class ClientDbProperties
    {
        public string instanceId { get; set; }
        public string clientInfo { get; set; }
        public string status { get; set; }
        public DateTime lastUpdate { get; set; }
    }
}
