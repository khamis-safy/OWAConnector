using Azure.Core;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using SMPPClientConnection.HelperClasses;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace WhatsappConnector.HelperClasses
{
    public static class StaticShared
    {

        public static HttpClient Client = new HttpClient();
        public static HttpClient ClientForDownload = new HttpClient();
        public static HttpClient ClientForIncoming = new HttpClient();

        public static HttpClient ClientForGettingMedia = new HttpClient();
        public static List<WhatsappIncomingMsg> TrackedMsg=new List<WhatsappIncomingMsg>();
        //public static List<SMPPClientList> clientlist = new List<SMPPClientList>();
        public static List<ReceivedMessage> messageslist = new List<ReceivedMessage>();
        public static ServiceBusSender callbackSender = new ServiceBusClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"), new ServiceBusClientOptions()
        {
            TransportType = ServiceBusTransportType.AmqpWebSockets
        }).CreateSender(Environment.GetEnvironmentVariable("servicebus-cs-topic"));

        public static ServiceBusSender receivemessagesSender = new ServiceBusClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"), new ServiceBusClientOptions()
        {
            TransportType = ServiceBusTransportType.AmqpWebSockets
        }).CreateSender(Environment.GetEnvironmentVariable("servicebus-cs-topic"));



        // Create a BlobServiceClient using the connection string
       public static BlobServiceClient blobServiceClient = new BlobServiceClient(Environment.GetEnvironmentVariable("BlobConnectionString"));

        // Get a reference to the container
        public static BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(Environment.GetEnvironmentVariable("BlobContainerName"));


    }
}
