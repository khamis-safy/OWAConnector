using System;
using System.Net.Http.Headers;
using System.Net.Http;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using static System.Net.Mime.MediaTypeNames;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using WhatsappConnector.HelperClasses;
using SMPPClientConnection.HelperClasses;
using System.Threading;
using Newtonsoft.Json.Linq;
using System.Text;
using WhatsappConnector;
using System.Collections;
using System.IO;
using Hangfire.MemoryStorage.Database;
using System.Text.RegularExpressions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Extensions;

namespace SendSMS
{
    public class SendSMS
    {
        private readonly Ishared shared;
        private readonly ILogger<SendSMS> log;
        private static object _mutex = new object();


        public SendSMS(Ishared shared, ILogger<SendSMS> log)
        {
            this.shared = shared;
            this.log = log;
        }
        [Singleton]
        [FunctionName("SendMSG")]
        public void Run([ServiceBusTrigger("%IncommingTopicName%", "%IncommingQueueName%",Connection = "AzureWebJobsStorage")]string myQueueItem)
        {
            lock (_mutex)
            {
                var MsgBody = new WhatsappIncomingMsg();
            try
            {
                //myQueueItem = myQueueItem.Substring(1, myQueueItem.Length - 2);
                 log.LogInformation($"Comming Body...!: {myQueueItem}");
                MsgBody = JsonConvert.DeserializeObject<WhatsappIncomingMsg>(myQueueItem);
                    //process this msg to smpp code 
                    if (!MsgBody.language.IsNullOrWhiteSpace()) 
                    {
                        SendTemplateMsg(MsgBody);
                    }else
                        SendMessage(MsgBody);
            }
            catch (Exception ex)
            {
                log.LogError(ex, ex.Message);

                var data = new SMPPCallBack
                {
                    messageRef = MsgBody.messageRef,
                    ackStatus = "4",

                };
               shared.CallBackWebhook(data);
            }
                Thread.Sleep(200);

            }

        }

        private async void SendMessage(WhatsappIncomingMsg incmsg)
        {
            // var clientlist = shared.ListClients();

            //  SmppClient client = null;
            try
            {
                StaticShared.Client.DefaultRequestHeaders.Authorization =
                 new AuthenticationHeaderValue("Bearer", incmsg.sessionId);
                SendTxtMsg textclass;
                if (incmsg.attachmenturl == "")
                    incmsg.msgtype = "text";
                var cutted = incmsg.attachmenturl.Split('.').Last();
                if (cutted.Equals("png", StringComparison.OrdinalIgnoreCase) || cutted.Equals("jpg", StringComparison.OrdinalIgnoreCase)|| cutted.Equals("jpeg", StringComparison.OrdinalIgnoreCase))
                { 
                    incmsg.msgtype = "image";
                }
                else if (cutted.Equals("mp4", StringComparison.OrdinalIgnoreCase))
                    incmsg.msgtype = "video";
                else if (cutted.Equals("doc", StringComparison.OrdinalIgnoreCase) || cutted.Equals("docx", StringComparison.OrdinalIgnoreCase) ||
                    cutted.Equals("pdf", StringComparison.OrdinalIgnoreCase) || cutted.Equals("XLS", StringComparison.OrdinalIgnoreCase) ||
                    cutted.Equals("XLSX", StringComparison.OrdinalIgnoreCase) || cutted.Equals("PPT", StringComparison.OrdinalIgnoreCase) ||
                    cutted.Equals("PPTX", StringComparison.OrdinalIgnoreCase) || cutted.Equals("txt", StringComparison.OrdinalIgnoreCase))
                    incmsg.msgtype = "document";
                else if (cutted.Equals("wav", StringComparison.OrdinalIgnoreCase) || cutted.Equals("mp3", StringComparison.OrdinalIgnoreCase) ||
                    cutted.Equals("m4a", StringComparison.OrdinalIgnoreCase))
                    incmsg.msgtype = "audio";
                else if (cutted.Equals("WASTICKERS", StringComparison.OrdinalIgnoreCase))
                    incmsg.msgtype = "sticker";



                var phoneListRes = await StaticShared.Client.GetAsync($"https://graph.facebook.com/v16.0/{incmsg.deviceid}/phone_numbers?access_token={incmsg.sessionId}");
                if (!phoneListRes.IsSuccessStatusCode)
                {
                    log.LogError("Error: " + phoneListRes.Content.ReadAsStringAsync().Result);
                }
                var phoneList = JsonConvert.DeserializeObject<PhoneNumbers>(phoneListRes.Content.ReadAsStringAsync().Result);
                string numberId = "";
                foreach (var item in phoneList.data)
                {
                    if (NormalizeNumber(item.display_phone_number) == NormalizeNumber(incmsg.fromNumber))
                    {
                        numberId = item.id;
                        break;
                    }
                }


                switch (incmsg.msgtype)
                {
                    case "text":
                         textclass = new SendTxtMsg()
                        {
                            messaging_product = "whatsapp",
                            recipient_type = "individual",
                            text = new Text()
                            {
                                body = incmsg.textcontents,
                                preview_url = true
                            },
                            to = incmsg.toNumber,
                            type = "text"

                        };
                        break;
                    case "image":
                        textclass = new SendTxtMsg()
                        {
                            messaging_product = "whatsapp",
                            recipient_type = "individual",
                            image = new Media()
                            {
                                caption = incmsg.textcontents,
                                link = incmsg.attachmenturl
                            },
                            to = incmsg.toNumber,
                            type = "image"

                        };
                        break;
                    case "video":
                        textclass = new SendTxtMsg()
                        {
                            messaging_product = "whatsapp",
                            recipient_type = "individual",
                            video = new Media()
                            {
                                caption = incmsg.textcontents,
                                link = incmsg.attachmenturl
                            },
                            to = incmsg.toNumber,
                            type = incmsg.msgtype

                        };
                        break;
                    case "document":
                        textclass = new SendTxtMsg()
                        {
                            messaging_product = "whatsapp",
                            recipient_type = "individual",
                            document = new Media()
                            {
                                caption = incmsg.textcontents,
                                link = incmsg.attachmenturl
                            },
                            to = incmsg.toNumber,
                            type = incmsg.msgtype

                        };
                        break;
                    case "audio":
                        textclass = new SendTxtMsg()
                        {
                            messaging_product = "whatsapp",
                            recipient_type = "individual",
                            audio = new Media2()
                            {
                                link = incmsg.attachmenturl
                            },
                            to = incmsg.toNumber,
                            type = incmsg.msgtype

                        };
                        break;
                    case "sticker":
                        textclass = new SendTxtMsg()
                        {
                            messaging_product = "whatsapp",
                            recipient_type = "individual",
                            sticker = new Media2()
                            {
                                link = incmsg.attachmenturl
                            },
                            to = incmsg.toNumber,
                            type = incmsg.msgtype

                        };
                        break;
                    default:
                         textclass = new SendTxtMsg()
                        {
                            messaging_product = "whatsapp",
                            recipient_type = "individual",
                            text = new Text()
                            {
                                body = incmsg.textcontents,
                                preview_url = true
                            },
                            to = incmsg.toNumber,
                            type = "text"

                        };
                        break;
                }



                var content = new StringContent(JsonConvert.SerializeObject(textclass), Encoding.UTF8, "application/json");


                // var res = await httpClient.postrequest($"https://graph.facebook.com/v13.0/{msg.FromNumberId}/messages", content, msg.Token);
                var msgres =await StaticShared.Client.PostAsync($"https://graph.facebook.com/v16.0/{numberId}/messages", content);
                var sds = msgres.Content.ReadAsStringAsync().Result;
                if (msgres.IsSuccessStatusCode)
                {
                    var msgresContent = JsonConvert.DeserializeObject<MsgResponse>(msgres.Content.ReadAsStringAsync().Result);
                    var msgId = msgresContent.messages.First().id;
                    incmsg.wamid = msgId;
                    StaticShared.TrackedMsg.Add(incmsg);
                }
                else
                {
                    // log.LogError("Error: on send whatsapp message api, on object: " + JsonConvert.SerializeObject(incmsg).ToString());
                    var reason = msgres.Content.ReadAsStringAsync().Result;
                    var data = new SMPPCallBack
                    {
                        messageRef = incmsg.messageRef,
                        ackStatus = "4",
                    };
                    shared.CallBackWebhook(data);
                }
            }
            catch (Exception ex)
            {

                log.LogError("Error: " + ex.Message + ", on object: "+JsonConvert.SerializeObject(incmsg).ToString());

                var data = new SMPPCallBack
                {
                    messageRef = incmsg.messageRef,
                    ackStatus = "4",
                };
                shared.CallBackWebhook(data);
            }

           
            
                //Thread.Sleep(1000);
                // client.Shutdown();
        }

        private async void SendTemplateMsg(WhatsappIncomingMsg incmsg)
        {
            try
            {
                StaticShared.Client.DefaultRequestHeaders.Authorization =
                 new AuthenticationHeaderValue("Bearer", incmsg.sessionId);
                SendTxtMsg textclass;



                var phoneListRes = await StaticShared.Client.GetAsync($"https://graph.facebook.com/v16.0/{incmsg.deviceid}/phone_numbers?access_token={incmsg.sessionId}");
                if (!phoneListRes.IsSuccessStatusCode)
                {
                    log.LogError("Error: " + phoneListRes.Content.ReadAsStringAsync().Result);
                }
                var phoneList = JsonConvert.DeserializeObject<PhoneNumbers>(phoneListRes.Content.ReadAsStringAsync().Result);
                string numberId = "";
                foreach (var item in phoneList.data)
                {
                    if (NormalizeNumber(item.display_phone_number) == NormalizeNumber(incmsg.fromNumber))
                    {
                        numberId = item.id;
                        break;
                    }
                }
                var extracted = new List<Parameter>();

                var extractData = Regex.Matches(incmsg.textcontents, @"\{(.*?)\}");
                int c = 1;
                foreach (Match item in extractData)
                {
                    extracted.Add(new Parameter { text = item.Value.Replace("}", "").Replace("{", ""),type= "text" });
                    c++;
                }
                textclass = new SendTxtMsg()
                {
                    messaging_product = "whatsapp",
                    recipient_type = "individual",
                    type = "template",
                    to = incmsg.toNumber,
                   
                    template = new Template
                    {
                        name = incmsg.templateName,
                        language = new Language
                        {
                            code = incmsg.language
                        }
                        ,components=new List<Component>
                        {
                            new Component
                            {
                                type="body",
                                parameters=extracted
                            }
                        }
                    }
                };

              



                var content = new StringContent(JsonConvert.SerializeObject(textclass), Encoding.UTF8, "application/json");


                // var res = await httpClient.postrequest($"https://graph.facebook.com/v13.0/{msg.FromNumberId}/messages", content, msg.Token);
                var msgres = await StaticShared.Client.PostAsync($"https://graph.facebook.com/v16.0/{numberId}/messages", content);
                var sds = msgres.Content.ReadAsStringAsync().Result;
                if (msgres.IsSuccessStatusCode)
                {
                    var msgresContent = JsonConvert.DeserializeObject<MsgResponse>(msgres.Content.ReadAsStringAsync().Result);
                    var msgId = msgresContent.messages.First().id;
                    incmsg.wamid = msgId;
                    StaticShared.TrackedMsg.Add(incmsg);
                }
                else
                {
                    // log.LogError("Error: on send whatsapp message api, on object: " + JsonConvert.SerializeObject(incmsg).ToString());
                    var reason = msgres.Content.ReadAsStringAsync().Result;
                    var data = new SMPPCallBack
                    {
                        messageRef = incmsg.messageRef,
                        ackStatus = "4",
                    };
                    shared.CallBackWebhook(data);
                }
            }
            catch (Exception ex)
            {

                log.LogError("Error: " + ex.Message + ", on object: " + JsonConvert.SerializeObject(incmsg).ToString());

                var data = new SMPPCallBack
                {
                    messageRef = incmsg.messageRef,
                    ackStatus = "4",
                };
                shared.CallBackWebhook(data);
            }
        }
        private string NormalizeNumber(string number)
        {
            return new string(number.Where(char.IsDigit).ToArray());
        }


    }
      
    }

public class Datum
{
    public string verified_name { get; set; }
    public string code_verification_status { get; set; }
    public string display_phone_number { get; set; }
    public string quality_rating { get; set; }
    public string id { get; set; }
}

public class PhoneNumbers
{
    public List<Datum> data { get; set; }
}
public class WhatsappIncomingMsg
{
    public string msgtype { get; set; }
    public string textcontents { get; set; }
    public long deviceid { get; set; }
    public string attachmenturl { get; set; }
    public string toNumber { get; set; }
    public string messageRef { get; set; }
    public string sessionId { get; set; }
    public int delay { get; set; }
    public string DeviceType { get; set; }
    public object? msgDatetime { get; set; }
    public string fromNumber { get; set; }
    public string wamid { get; set; }
    public string language { get; set; }
    public string templateName { get; set; }
}
public class Contact
{
    public string input { get; set; }
    public string wa_id { get; set; }
}

public class Message
{
    public string id { get; set; }
}

public class MsgResponse
{
    public string messaging_product { get; set; }
    public List<Contact> contacts { get; set; }
    public List<Message> messages { get; set; }
}
public class Component
{
    public string type { get; set; }
    public List<Parameter> parameters { get; set; }
}

public class Language
{
    public string code { get; set; }
}

public class Parameter
{
    public string type { get; set; }
    public string text { get; set; }
}

public class TemplateMsg
{
    public string messaging_product { get; set; }
    public string to { get; set; }
    public string type { get; set; }
    public Template template { get; set; }
}

public class Template
{
    public string name { get; set; }
    public Language language { get; set; }
    public List<Component> components { get; set; }
}
public class WebhookPayload
{
    public string EventType { get; set; }
    public string Data { get; set; }
}
public class initialWebhook
{
    public string mode { get; set; }
    public long challenge { get; set; }
    public string verify_token { get; set; }
}
public class msgData
{
    public string Token { get; set; }
    public string toNumber { get; set; }
    public string FromNumberId { get; set; }
    public string? Message { get; set; }
    public string? Name { get; set; }
}
public class Text
{
    public bool preview_url { get; set; }
    public string body { get; set; }

}
public class Media
{
    public string link { get; set; }
    public string? caption { get; set; }

}
public class Media2
{
    public string link { get; set; }

}

public class SendTxtMsg
{
    public string messaging_product { get; set; }
    public string recipient_type { get; set; }
    public string to { get; set; }
    public string type { get; set; }
    public Text? text { get; set; }
    public Media? image { get; set; }
    public Media? video { get; set; }
    public Media? document { get; set; }
    public Media2? audio { get; set; }
    public Media2? sticker { get; set; }
    public Template? template { get; set; }

}

public class Body
{
    public string text { get; set; }

}
public class Reply
{
    public string id { get; set; }
    public string title { get; set; }

}
public class Buttons
{
    public string type { get; set; }
    public Reply reply { get; set; }

}
public class Action
{
    public IList<Buttons> buttons { get; set; }

}
public class Interactive
{
    public string type { get; set; }
    public Body body { get; set; }
    public Action action { get; set; }

}
public class actionButtons
{
    public string messaging_product { get; set; }
    public string recipient_type { get; set; }
    public string to { get; set; }
    public string type { get; set; }
    public Interactive interactive { get; set; }

}
public class Location
{
    public int longitude { get; set; }
    public int latitude { get; set; }
    public string name { get; set; }
    public string address { get; set; }

}
public class SendLoc
{
    public string messaging_product { get; set; }
    public string recipient_type { get; set; }
    public string to { get; set; }
    public string type { get; set; }
    public Location location { get; set; }

}
public class Header
{
    public string type { get; set; }
    public string text { get; set; }

}
public class ListBody
{
    public string text { get; set; }

}
public class Footer
{
    public string text { get; set; }

}
public class Rows
{
    public string id { get; set; }
    public string title { get; set; }
    public string description { get; set; }

}
public class Sections
{
    public string title { get; set; }
    public IList<Rows> rows { get; set; }

}
public class ListAction
{
    public string button { get; set; }
    public IList<Sections> sections { get; set; }

}
public class ListInteractive
{
    public string type { get; set; }
    public Header header { get; set; }
    public ListBody body { get; set; }
    public Footer footer { get; set; }
    public ListAction action { get; set; }

}
public class SendList
{
    public string messaging_product { get; set; }
    public string recipient_type { get; set; }
    public string to { get; set; }
    public string type { get; set; }
    public ListInteractive interactive { get; set; }

}

public class Name
{
    public string formatted_name { get; set; }
    public string first_name { get; set; }
    public string last_name { get; set; }

}
public class Phones
{
    public string phone { get; set; }
    public string type { get; set; }
    public string wa_id { get; set; }

}
public class Contacts
{
    public Name name { get; set; }
    public IList<Phones> phones { get; set; }

}
public class SendContact
{
    public string messaging_product { get; set; }
    public string recipient_type { get; set; }
    public string to { get; set; }
    public string type { get; set; }
    public IList<Contacts> contacts { get; set; }

}



