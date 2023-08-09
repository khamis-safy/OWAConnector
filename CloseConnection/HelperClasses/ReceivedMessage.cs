using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMPPClientConnection.HelperClasses
{
    public class ReceivedMessage
    {
        public string ClientId { get; set; }
        public string sourceAddress { get; set; }
        public string destinationAddress { get; set; }
        public int msgCount { get; set; }
        public int msgSequence { get; set; }
        public string Text { get;  set; }
    }
}
