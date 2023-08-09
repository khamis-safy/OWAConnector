using Microsoft.AspNetCore.Mvc;
using SMPPClientConnection.HelperClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhatsappConnector.HelperClasses
{
    public interface Ishared
    {
     
        Task CallBackWebhook(SMPPCallBack contente);
        Task incommingMsgWebhook(IncommingMsgCallBack contente);
        Task incommingMsgWebhook(string contente);
        void AddReceivedMessage(ReceivedMessage message);
        IList<ReceivedMessage> ListReceivedMessage();
        void DeleteReceivedMessage(IList<ReceivedMessage> message);
    }
}
