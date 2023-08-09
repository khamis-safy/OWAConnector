using System;
using System.Net.Http.Headers;
using System.Net.Http;
using JamaaTech.Smpp.Net.Client;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using JamaaTech.Smpp.Net.Lib.Protocol;
using JamaaTech.Smpp.Net.Lib;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using static System.Net.Mime.MediaTypeNames;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace SendSMS
{
    public class SendSMS
    {
        public static List<SMPPClientList> clientlist = new List<SMPPClientList>();
        static string baseurl = "https://dev-commswift-api.azurewebsites.net/";
        static string createconnectionendpoint = "api/PowerAutomate/CreateSMPPInstance";
        static string closeconnectionendpoint = "api/PowerAutomate/DeleteSMPPInstance";
        static string updateconnectionendpoint = "api/PowerAutomate/UpdateSMPPInstance";
        static string getconnectionendpoint = "api/PowerAutomate/GetSMPPInstances";

        [FunctionName("SendSMS")]
        public void Run([ServiceBusTrigger("%IncommingQueueName%",Connection = "AzureWebJobsStorage")]string myQueueItem, ILogger log)
        {
            try
            {
                //myQueueItem = myQueueItem.Substring(1, myQueueItem.Length - 2);
                log.LogInformation($"Comming Body...!: {myQueueItem}");
                var MsgBody = JsonConvert.DeserializeObject<SMPPIncomingMsg>(myQueueItem);
                //process this msg to smpp code 

                SendMessage(MsgBody,log);
            }
            catch (Exception ex)
            {
                log.LogError(ex, ex.Message);
            }
        }

        private void SendMessage(SMPPIncomingMsg incmsg, ILogger log)
        {

            try
            {
                SmppClient client = null;
                if (!clientlist.Any(d => d.client.Name == incmsg.ClientId))
                {
                    log.LogError("Client Is not exists, Make Sure that the clientId is correct, Or try to create a new one");
                }
                else
                {
                    var data = clientlist.First(d => d.client.Name == incmsg.ClientId);
                    if (data.client.ConnectionState != SmppConnectionState.Connected)
                    {
                        data.client.ForceConnect(5000);
                    }
                    client = data.client;
                }


                // send message now
                var msg = new TextMessage();

                msg.DestinationAddress = incmsg.DestinationAddress;
                msg.SourceAddress = incmsg.SourceAddress;
                msg.Text = incmsg.Text;
                msg.RegisterDeliveryNotification = true; //I want delivery notification for this message

                // if (client.ConnectionState != SmppConnectionState.Connected) client.ForceConnect(5000);

                client.BeginSendMessage(msg, SendMessageCompleteCallback, client);
                //Thread.Sleep(1000);
                // client.Shutdown();
            }
            catch (Exception e)
            {
                log.LogError( e.Message );
            }

        }
        private void SendMessageCompleteCallback(IAsyncResult result)
        {
            SmppClient client = (SmppClient)result.AsyncState;
            client.EndSendMessage(result);
        }


    }
    public class SMPPIncomingMsg
    {
        public string ClientId { get; set; }
        public string DestinationAddress { get; set; }
        public string SourceAddress { get; set; }
        public string Text { get; set; }
    }

    public class SMPPClientList
    {
        public SmppClient client { get; set; }
        public string SourceAddress { get; set; }
    }
}
