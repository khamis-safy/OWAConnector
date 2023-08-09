using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using WhatsappConnector.HelperClasses;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using SMPPClientConnection.HelperClasses;
using System.Net.Http.Headers;
using Azure.Storage.Blobs.Models;
using System.Text;
using Azure;
using System.Net.Http;
using System.Threading;

namespace WhatsappConnector
{
    public class Webhook
    {
        private readonly Ishared shared;
        private readonly ILogger<Webhook> logger;
        private static object _mutex = new object();


        public Webhook(Ishared shared,ILogger<Webhook> logger)
        {
            this.shared = shared;
            this.logger = logger;
        }
        [FunctionName("webhook")]
        [Singleton]
        public  ActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get","post", Route = null)] HttpRequest req)
        {
            lock (_mutex)
            {

                try
                {
                    //  var ii = shared.ListClients();

                    if (req.Method == "GET")
                    {
                        var challenge = req.Query["hub.challenge"];
                        logger.LogInformation("####################");
                        logger.LogInformation($"authentication requested {challenge}");
                        logger.LogInformation("####################");

                        return new OkObjectResult(long.Parse(challenge.First()));

                        // The action is a POST.
                    }
                    // string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                    else
                    {
                        string requestBody = new StreamReader(req.Body).ReadToEndAsync().Result;
                        var barsed = JsonConvert.DeserializeObject<incommingWebhook>(requestBody);
                        logger.LogInformation("Signal Coming ....\n\n");
                        if (barsed != null)
                        {
                            if (barsed.entry.First().changes.First().value.messages != null)
                            {
                                // this is incomming msg
                                var msg = barsed.entry.First().changes.First().value;

                                if (msg.messages.First().type == "text")
                                {
                                    var incMsg = new IncommingMsgCallBack
                                    {
                                        msgtype = "text",
                                        fromnumber = msg.messages.First().from,
                                        textcontents = msg.messages.First().text.body,
                                        wppSessionId = barsed.entry.First().id,
                                        tonumber = msg.metadata.display_phone_number,
                                        wppMessageRef = $"false_{msg.messages.First().from}@c.us_{Guid.NewGuid()}",
                                        msgSource = "OWA",
                                        attachmenturl = ""
                                    };
                                    logger.LogInformation("incoming msg ....\t" + JsonConvert.SerializeObject(incMsg).ToString());

                                    shared.incommingMsgWebhook(incMsg);
                                }
                                else if (msg.messages.First().type == "image")
                                {


                                    string finalUrl =  getAtachment(barsed.entry.First().id, msg.messages.First().image.id, msg.metadata.display_phone_number).Result;




                                    //var phoneList = JsonConvert.DeserializeObject<PhoneNumbers>(phoneListRes.Content.ReadAsStringAsync().Result);

                                    var incMsg = new IncommingMsgCallBack
                                    {
                                        msgtype = "image",
                                        fromnumber = msg.messages.First().from,
                                        textcontents = msg.messages.First().image.caption==null?"": msg.messages.First().image.caption,
                                        wppSessionId = barsed.entry.First().id,
                                        wppMessageRef = $"false_{msg.messages.First().from}@c.us_{Guid.NewGuid()}",
                                        attachmenturl = finalUrl,
                                        tonumber = msg.metadata.display_phone_number,
                                        msgSource = "OWA"
                                    };
                                    logger.LogInformation("incoming msg ....\t" + JsonConvert.SerializeObject(incMsg).ToString());

                                    shared.incommingMsgWebhook(incMsg);
                                }
                                else if (msg.messages.First().type == "video")
                                {
                                    string finalUrl = getAtachment(barsed.entry.First().id, msg.messages.First().video.id, msg.metadata.display_phone_number).Result;

                                    var incMsg = new IncommingMsgCallBack
                                    {
                                        msgtype = "video",
                                        fromnumber = msg.messages.First().from,
                                        textcontents = msg.messages.First().video.caption == null ? "" : msg.messages.First().video.caption,
                                        wppSessionId = barsed.entry.First().id,
                                        attachmenturl = finalUrl,
                                        tonumber = msg.metadata.display_phone_number,
                                        wppMessageRef = $"false_{msg.messages.First().from}@c.us_{Guid.NewGuid()}",
                                        msgSource = "OWA"
                                    };
                                    logger.LogInformation("incoming msg ....\t" + JsonConvert.SerializeObject(incMsg).ToString());

                                    shared.incommingMsgWebhook(incMsg);
                                }
                                else if (msg.messages.First().type == "document")
                                {
                                    string finalUrl = getAtachment(barsed.entry.First().id, msg.messages.First().document.id, msg.metadata.display_phone_number).Result;

                                    var incMsg = new IncommingMsgCallBack
                                    {
                                        msgtype = "document",
                                        fromnumber = msg.messages.First().from,
                                        textcontents = msg.messages.First().document.caption == null ? "" : msg.messages.First().document.caption,
                                        wppSessionId = barsed.entry.First().id,
                                        attachmenturl = finalUrl,
                                        tonumber = msg.metadata.display_phone_number,
                                        wppMessageRef = $"false_{msg.messages.First().from}@c.us_{Guid.NewGuid()}",
                                        msgSource = "OWA"
                                    };
                                    logger.LogInformation("incoming msg ....\t" + JsonConvert.SerializeObject(incMsg).ToString());

                                    shared.incommingMsgWebhook(incMsg);
                                }
                                else if (msg.messages.First().type == "audio")
                                {
                                    string finalUrl = getAtachment(barsed.entry.First().id, msg.messages.First().audio.id, msg.metadata.display_phone_number).Result;

                                    var incMsg = new IncommingMsgCallBack
                                    {
                                        msgtype = "audio",
                                        fromnumber = msg.messages.First().from,
                                        textcontents = "",
                                        wppSessionId = barsed.entry.First().id,
                                        attachmenturl = finalUrl,
                                        tonumber = msg.metadata.display_phone_number,
                                        wppMessageRef = $"false_{msg.messages.First().from}@c.us_{Guid.NewGuid()}",
                                        msgSource = "OWA"
                                    };
                                    logger.LogInformation("incoming msg ....\t" + JsonConvert.SerializeObject(incMsg).ToString());

                                    shared.incommingMsgWebhook(incMsg);
                                }
                                else if (msg.messages.First().type == "sticker")
                                {
                                    string finalUrl = getAtachment(barsed.entry.First().id, msg.messages.First().sticker.id, msg.metadata.display_phone_number).Result;

                                    var incMsg = new IncommingMsgCallBack
                                    {
                                        msgtype = "sticker",
                                        fromnumber = msg.messages.First().from,
                                        textcontents = "",
                                        wppSessionId = barsed.entry.First().id,
                                        attachmenturl = finalUrl,
                                        tonumber = msg.metadata.display_phone_number,
                                        wppMessageRef = $"false_{msg.messages.First().from}@c.us_{Guid.NewGuid()}",
                                        msgSource = "OWA"
                                    };
                                    logger.LogInformation("incoming msg ....\t" + JsonConvert.SerializeObject(incMsg).ToString());

                                    shared.incommingMsgWebhook(incMsg);
                                }

                            }
                            else if (barsed.entry.First().changes.First().value.statuses != null)
                            {
                                var st = barsed.entry.First().changes.First().value.statuses;
                                var msgref = StaticShared.TrackedMsg.First(d => d.wamid == st.First().id);
                                // this is status msg
                                var status = new SMPPCallBack
                                {
                                    ackStatus = st.First().status == "delivered" ? "2" : st.First().status == "sent" ? "1" : st.First().status == "read" ? "3" : "4",
                                    messageRef = msgref.messageRef
                                };
                                if (status.ackStatus == "3" || status.ackStatus == "4")
                                    StaticShared.TrackedMsg.Remove(msgref);
                                logger.LogInformation("status msg ....\t" + status.messageRef + ", " + status.ackStatus);

                                shared.CallBackWebhook(status);
                            }
                        }
                    Thread.Sleep(200);

                        return new OkObjectResult(null);

                    }

                    // var data = JsonConvert.DeserializeObject<SMPPCreateConnection>(requestBody);
                }
                catch (Exception e)
                {
                    logger.LogError(e.Message, e);

                    return new BadRequestObjectResult(new { Msg = e.Message });
                }

            }
        }
        public string Uploadfile(byte[] file,string mime)
        {
            try
            {

                // Image image = ConvertBase64ToImage(file, mime);

                var ext= MimeTypes.MimeTypeMap.GetExtension(mime);

                string filename = Guid.NewGuid().ToString() + ext;
                var blobClient = StaticShared.containerClient.GetBlobClient(filename);
                var options = new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders { ContentType = mime }
                };
                using (var stream = new MemoryStream(file)) {
                    blobClient.Upload(stream, options);
                }
                var url = blobClient.Uri.AbsoluteUri;
                    
                return url;
            }
            catch
            {
                return String.Empty;
            }
        }
        public async Task<string> getAtachment(string accountId, string imageId, string display_phone_number)
        {
            logger.LogInformation("####################");
            logger.LogInformation($"Getting the token for accId {accountId}, image Id {imageId} and phone number {display_phone_number}");
            logger.LogInformation("####################");

            //Get the client token
            var theToken = await StaticShared.ClientForIncoming.GetAsync($"{Environment.GetEnvironmentVariable("baseurl")}{Environment.GetEnvironmentVariable("GetToken")}?accountId={accountId}&waNumber={display_phone_number}");
            var tokenContent = await theToken.Content.ReadAsStringAsync();


            logger.LogInformation("####################");
            logger.LogInformation($"the token is {tokenContent}");
            logger.LogInformation("####################");

            //Get the attachment URL
            StaticShared.ClientForGettingMedia.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenContent);
            StaticShared.ClientForGettingMedia.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; AcmeInc/1.0)");
            var mediaobj = await StaticShared.ClientForGettingMedia.GetAsync($"https://graph.facebook.com/v16.0/{imageId}/");
            var attachmentData = JsonConvert.DeserializeObject<GetAttachmentURL>(await mediaobj.Content.ReadAsStringAsync());
            logger.LogInformation("####################");
            logger.LogInformation($"attachment Data is {JsonConvert.SerializeObject(attachmentData)}");
            logger.LogInformation("####################");

            //Download the attachment
            byte[] contentBytes;

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenContent);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; AcmeInc/1.0)");

                contentBytes = await client.GetByteArrayAsync(attachmentData.url);
                
            }
            //using (var webClient = new WebClient())
            //{
            //    webClient.Headers.Add("Authorization", "Bearer "+tokenContent);
            //    contentBytes = webClient.DownloadData(attachmentData.url);
            //}
            //logger.LogInformation("####################");
            //logger.LogInformation($"Content Byte Data is {contentBytes}");
            //logger.LogInformation("####################");

            logger.LogInformation("####################");
            logger.LogInformation($"attachment Data is {Convert.ToBase64String(contentBytes)}");
            logger.LogInformation("####################");

            string finalUrl = Uploadfile(contentBytes, attachmentData.mime_type);
          
            logger.LogInformation("####################");
            logger.LogInformation($"attachment Url is {finalUrl}");
            logger.LogInformation("####################");
            return finalUrl;
        }

      
    }
    
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class Button
    {
        public string payload { get; set; }
        public string text { get; set; }
    }
    public class Text
    {
        public string body { get; set; }
    }

    public class Change
    {
        public Value value { get; set; }
        public string field { get; set; }
    }

    public class Contact
    {
        public Profile profile { get; set; }
        public string wa_id { get; set; }
    }

    public class Context
    {
        public string from { get; set; }
        public string id { get; set; }
    }

    public class Conversation
    {
        public string id { get; set; }
        public Origin origin { get; set; }
    }

    public class Entry
    {
        public string id { get; set; }
        public List<Change> changes { get; set; }
    }

    public class Message
    {
        public Context? context { get; set; }
        public string from { get; set; }
        public string id { get; set; }
        public string timestamp { get; set; }
        public string type { get; set; }
        public Button? button { get; set; }
        public Text? text { get; set; }
        public Media? image { get; set; }
        public Media? video { get; set; }
        public Media? document { get; set; }
        public Media2? audio { get; set; }
        public Media2? sticker { get; set; }
    }
    public class Media
    {
        public string id { get; set; }
        public string? caption { get; set; }

    }
    public class Media2
    {
        public string id { get; set; }

    }
    public class Metadata
    {
        public string display_phone_number { get; set; }
        public string phone_number_id { get; set; }
    }

    public class Origin
    {
        public string type { get; set; }
    }

    public class Pricing
    {
        public bool billable { get; set; }
        public string pricing_model { get; set; }
        public string category { get; set; }
    }

    public class Profile
    {
        public string name { get; set; }
    }

    public class incommingWebhook
    {
        public string @object { get; set; }
        public List<Entry> entry { get; set; }
    }

    public class Status
    {
        public string id { get; set; }
        public string status { get; set; }
        public string timestamp { get; set; }
        public string recipient_id { get; set; }
        public Conversation conversation { get; set; }
        public Pricing pricing { get; set; }
    }

    public class Value
    {
        public string messaging_product { get; set; }
        public Metadata metadata { get; set; }
        public List<Status>? statuses { get; set; }
        public List<Contact>? contacts { get; set; }
        public List<Message>? messages { get; set; }
    }
    public class GetAttachmentURL
    {
        public string messaging_product { get; set; }
        public string url { get; set; }
        public string mime_type { get; set; }
        public string sha256 { get; set; }
        public string file_size { get; set; }
        public string id { get; set; }
    }

}
