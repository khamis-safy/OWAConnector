using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMPPClientConnection.HelperClasses
{
    public class IncommingMsgCallBack
    {
        public string msgtype { get; set; }
        public string textcontents { get; set; }
        public string attachmenturl { get; set; }
        public string fromnumber { get; set; }
        public string tonumber { get; set; }
        public string wppSessionId { get; set; }
        public string wppMessageRef { get; set; }
        public string msgSource { get; set; }
    }
}
