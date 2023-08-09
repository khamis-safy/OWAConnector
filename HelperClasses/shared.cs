using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SMPPClientConnection.HelperClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace WhatsappConnector.HelperClasses
{
    public class shared : Ishared
    {

      
        public async Task CallBackWebhook(SMPPCallBack contente)
        {
            
            try
            {
              await  StaticShared.callbackSender.SendMessagesAsync(new List<ServiceBusMessage> { new ServiceBusMessage(JsonConvert.SerializeObject(contente).ToString()) {
                    Subject = "statusupdatelistener",
                    CorrelationId = "statusupdate"

                } });
            }
            finally
            {
            }

        }


        public async Task incommingMsgWebhook(IncommingMsgCallBack contente)
        {
           
            try
            {
                await StaticShared.receivemessagesSender.SendMessagesAsync(new List<ServiceBusMessage> { new ServiceBusMessage(JsonConvert.SerializeObject(contente).ToString()) {
                            CorrelationId= "incommingmsg",
                            Subject= "incommingmsglistener"

                    } });
            }
            finally
            {
            }

        }


        public async Task incommingMsgWebhook(string contente)
        {
            try
            {
                await StaticShared.receivemessagesSender.SendMessagesAsync(new List<ServiceBusMessage> { new ServiceBusMessage(JsonConvert.SerializeObject(contente)) 
                {
                            CorrelationId= "incommingmsg",
                            Subject= "incommingmsglistener"

                } });
            }
            finally
            {
            }
        }

        public void AddReceivedMessage(ReceivedMessage message)
        {
            StaticShared.messageslist.Add(message);
        }
        public void DeleteReceivedMessage(IList<ReceivedMessage> message)
        {
            foreach (var item in message)
            {
            StaticShared.messageslist.Remove(item);

            }
        }

        public IList<ReceivedMessage> ListReceivedMessage()
        {
            return StaticShared.messageslist;
        }
    }
}
