using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using JamaaTech.Smpp.Net.Client;
using JamaaTech.Smpp.Net.Lib.Protocol;
using JamaaTech.Smpp.Net.Lib;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using Common.Logging;
using System.Web.Http;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text.Json.Nodes;
using System.Text;
using System.Linq;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Hosting;
using static System.Net.Mime.MediaTypeNames;

namespace CreateConnection
{
    public static class CreateConnection
    {
        public static List<SMPPClientList> clientlist = new List<SMPPClientList>();
        static string baseurl = "https://dev-commswift-api.azurewebsites.net/";
        static string createconnectionendpoint = "api/PowerAutomate/CreateSMPPInstance";
        static string closeconnectionendpoint = "api/PowerAutomate/DeleteSMPPInstance";
        static string updateconnectionendpoint = "api/PowerAutomate/UpdateSMPPInstance";
        static string getconnectionendpoint = "api/PowerAutomate/GetSMPPInstances";


        static CreateConnection()
        {

        }



        [FunctionName("CreateConnection")]
       // [OpenApiOperation(operationId: "Run", tags: new[] { "name" })]
        //[OpenApiParameter(name: "name", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **Name** parameter")]
        //[OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        public static async Task<ActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req, ILogger log)
        {
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var data = JsonConvert.DeserializeObject<SMPPCreateConnection>(requestBody);
                var client = new SmppClient();
                SmppConnectionProperties properties = new SmppConnectionProperties();

                properties = client.Properties;
                properties.SystemID = data.SystemID;
                properties.Password = data.Password;
                properties.Port = data.Port; //IP port to use
                properties.Host = data.Host;
                properties.SystemType = "Transceiver";
                properties.DefaultServiceType = ServiceType.DEFAULT;
                properties.DefaultEncoding = DataCoding.UCS2;
                client.Name = data.Name;
                if (client.ConnectionState != SmppConnectionState.Connected)
                    client.Start();

                if (client.ConnectionState != SmppConnectionState.Connected) client.ForceConnect(5000);
                client.AutoReconnectDelay = 3000;
                client.ConnectionStateChanged += client_ConnectionStateChanged;
                client.MessageDelivered += client_MessageDelivered;
                client.MessageReceived += client_MessageReceived;

                if (client.Name == null)
                    client.Name = Guid.NewGuid().ToString();

                clientlist.Add(new SMPPClientList { client = client });

                var jn = JsonConvert.SerializeObject(clientlist);

                var dbclient = new ClientDbProperties
                {
                    instanceId = client.Name,
                    clientInfo = jn.ToString(),
                    lastUpdate = DateTime.UtcNow,
                    status = "Connected"
                };



                HttpClient Client = new HttpClient();

                var content = new StringContent(JsonConvert.SerializeObject(dbclient).ToString(), Encoding.UTF8, "application/json");
                Client.DefaultRequestHeaders.Add("B5DCA7A2-E086-4F55-A30C-8AE0C823D522", "88A265D7-C9DB-42DD-BF42-3020EF430A53");
                var result = Client.PostAsync(baseurl + createconnectionendpoint, content).Result;



                return new OkObjectResult(client.Name);
            }
            catch (Exception e)
            {
                return new BadRequestObjectResult(new { Msg = e.Message });
            }

        }
       [FunctionName("CloseConnection")]
        // [OpenApiOperation(operationId: "Run", tags: new[] { "name" })]
        //[OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        //[OpenApiParameter(name: "name", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **Name** parameter")]
        //[OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        public static IActionResult Run(
           [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = null)] HttpRequest req)
        {

            string ClientId = req.Query["clientId"];

            //dynamic data = JsonConvert.DeserializeObject(requestBody);
            try
            {
                if (!clientlist.Any(d => d.client.Name == ClientId))
                {
                    return new BadRequestObjectResult(new { Msg = "Client Is not exists, Make Sure that the clientId is correct" });
                }

                var data = clientlist.First(d => d.client.Name == ClientId);
                data.client.Shutdown();
                clientlist.Remove(data);

                HttpClient Client = new HttpClient();
                Client.DefaultRequestHeaders.Add("B5DCA7A2-E086-4F55-A30C-8AE0C823D522", "88A265D7-C9DB-42DD-BF42-3020EF430A53");
                var result = Client.DeleteAsync(baseurl + createconnectionendpoint + "?InstanceId=" + ClientId).Result;

                if (result.IsSuccessStatusCode)
                    return new OkObjectResult(new { Msg = "Success" });
                else return new BadRequestObjectResult(new { Msg = "Error from DeleteSMPPInstance Endpoint" });
            }
            catch (Exception e)
            {
                return new BadRequestObjectResult(new { Msg = e.Message });
            }
        }
        [FunctionName("SendSMS")]
        public static void Run([ServiceBusTrigger("%IncommingQueueName%", Connection = "AzureWebJobsStorage")] string myQueueItem, ILogger log)
        {
            try
            {
                //myQueueItem = myQueueItem.Substring(1, myQueueItem.Length - 2);
                log.LogInformation($"Comming Body...!: {myQueueItem}");
                var MsgBody = JsonConvert.DeserializeObject<SMPPIncomingMsg>(myQueueItem);
                //process this msg to smpp code 

                SendMessage(MsgBody, log);
            }
            catch (Exception ex)
            {
                log.LogError(ex, ex.Message);
            }
        }

















        private static void SendMessage(SMPPIncomingMsg incmsg, ILogger log)
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
                log.LogError(e.Message);
            }

        }
        public static void startupfunction(string SystemID,string Password,int Port,string Host,string Name)
        {
            var client = new SmppClient();
            SmppConnectionProperties properties = new SmppConnectionProperties();

            properties = client.Properties;
            properties.SystemID = SystemID;
            properties.Password =Password;
            properties.Port = Port; //IP port to use
            properties.Host = Host;
            properties.SystemType = "Transceiver";
            properties.DefaultServiceType = ServiceType.DEFAULT;
            properties.DefaultEncoding = DataCoding.UCS2;
            client.Name = Name;
            if (client.ConnectionState != SmppConnectionState.Connected)
                client.Start();

            if (client.ConnectionState != SmppConnectionState.Connected) client.ForceConnect(5000);
            client.AutoReconnectDelay = 3000;
            client.ConnectionStateChanged += client_ConnectionStateChanged;
            client.MessageDelivered += client_MessageDelivered;
            client.MessageReceived += client_MessageReceived;

            if (client.Name == null)
                client.Name = Guid.NewGuid().ToString();

            clientlist.Add(new SMPPClientList { client = client });
        }
       static void client_MessageDelivered(object sender, MessageEventArgs e)
        {
            TextMessage msg = e.ShortMessage as TextMessage;
            //parse msg.Text for more details
            Console.WriteLine("###################################");
            Console.WriteLine(msg.Text + " Message Delivered at: " + DateTime.UtcNow);
            Console.WriteLine("###################################\n\n\n");
        }
      static  private void client_MessageReceived(object sender, MessageEventArgs e)
        {
            TextMessage msg = e.ShortMessage as TextMessage;
            Console.WriteLine("###################################");
            Console.WriteLine(msg.Text + " Message Recieved at: " + DateTime.UtcNow);
            Console.WriteLine("###################################\n\n\n");
        }
       static private void client_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            switch (e.CurrentState)
            {
                case SmppConnectionState.Closed:
                    Console.WriteLine("###################################");
                    Console.WriteLine("Connection is lost at: " + DateTime.UtcNow);
                    Console.WriteLine("###################################\n\n\n");
                    //e.ReconnectInteval = 60000; //Try to reconnect after 1 min
                    break;
                case SmppConnectionState.Connected:
                    Console.WriteLine("###################################");
                    Console.WriteLine("Connection Stablished at: " + DateTime.UtcNow);
                    Console.WriteLine("###################################\n\n\n");
                    break;
                case SmppConnectionState.Connecting:
                    Console.WriteLine("###################################");
                    Console.WriteLine("Connecting .... at: " + DateTime.UtcNow);
                    Console.WriteLine("###################################\n\n\n");
                    break;
            }
        }

        private static void SendMessageCompleteCallback(IAsyncResult result)
        {
            SmppClient client = (SmppClient)result.AsyncState;
            client.EndSendMessage(result);
        }

    }
    public class SMPPCreateConnection
    {
        public string SystemID { get; set; }
        public string Password { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string? Name { get; set; }
    }

    public class SMPPClientList
    {
        public SmppClient client { get; set; }
    }
    public class SMPPIncomingMsg
    {
        public string ClientId { get; set; }
        public string DestinationAddress { get; set; }
        public string SourceAddress { get; set; }
        public string Text { get; set; }
    }

}


