using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMPPClientConnection.HelperClasses
{
    public class SMPPCallBack
    {
        public string msgtype { get; set; }
        public string ackStatus { get; set; }
        public string wppSesionId { get; set; }
        public string attachmenturl { get; set; }
        public string toNumber { get; set; }
        public string wppMessageRef { get; set; }
        public string messageRef { get; set; }
        public string textcontents { get; set; }
        public bool isMobile { get; set; }
    }
}
