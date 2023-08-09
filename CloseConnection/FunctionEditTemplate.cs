using WhatsappConnector.HelperClasses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Linq;
using System.Net.Http.Headers;
using static System.Net.Mime.MediaTypeNames;
using static Azure.Core.HttpHeader;
using System.Text.RegularExpressions;
using System.Collections;
using MimeTypes;
using static WhatsappConnector.FunctionCreateTemplate;
using System.Net;

namespace WhatsappConnector
{
   
    public class FunctionEditTemplate
    {
        private readonly Ishared shared;
        private readonly ILogger<FunctionEditTemplate> logger;

        public FunctionEditTemplate(Ishared shared, ILogger<FunctionEditTemplate> logger)
        {
            this.shared = shared;
            this.logger = logger;
        }

        [FunctionName("EditTemplate")]
        public async Task<ActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = null)] HttpRequest req)
        {
            try
            {
                var query = new parameters
                {
                    template_name = req.Query["templateName"],
                };


                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var data = JsonConvert.DeserializeObject<WhatsappCreateTemplate>(requestBody);
                var id = await GetTemplateId(data.accountId, data.token, query.template_name);

                var extracted = new List<List<string>>();
                var extractedone = new List<string>();

                var extractData = Regex.Matches(data.bodyText, @"\{(.*?)\}");
                int c = 1;
                foreach (Match item in extractData)
                {

                    extractedone.Add(item.Value.Replace("}", "").Replace("{", ""));
                    data.bodyText = data.bodyText.Replace(item.Value, "{{" + c + "}}");
                    c++;
                }
                extracted.Add(extractedone);

                StaticShared.Client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", data.token);

                var template = new Template
                {
                    category = data.category,
                    language = data.languageCode,
                    name = data.templateName,
                    components = new List<Component>()
                };
                Component compon;
                if (extractedone.Count() > 0)
                {
                    compon = new Component
                    {
                        type = "BODY",
                        text = data.bodyText,
                        example = new Example { body_text = extracted }
                    };
                }
                else
                {
                    compon = new Component
                    {
                        type = "BODY",
                        text = data.bodyText,
                    };
                }
                template.components.Add(compon);
                if (data.isButtonReply == true)
                {
                    var compo = new Component
                    {
                        type = "BUTTONS",
                        buttons = new List<Button>()

                    };
                    foreach (var item in data.buttons)
                    {
                        compo.buttons.Add(new Button
                        {
                            text = item.text,
                            type = "QUICK_REPLY"
                        });
                    }
                    template.components.Add(compo);
                }
                else if (data.isButtonURL)
                {
                    var compo = new Component
                    {
                        type = "BUTTONS",
                        buttons = new List<Button>()

                    };
                    compo.buttons.Add(new Button
                    {
                        text = data.buttons.First().text,
                        type = "URL",
                        url = data.buttons.First().url
                    });

                    template.components.Add(compo);
                }

                if (data.isFooter == true)
                {
                    var compo = new Component
                    {
                        type = "FOOTER",
                        text = data.footerText
                    };
                  
                    template.components.Add(compo);
                }

                if (data.isHeaderText == true || data.isHeaderImage == true)
                {
                    var compo = new Component
                    {
                        type = "HEADER"
                        
                    };
                    if (data.isHeaderText)
                    {
                        compo.text = data.headerText;
                        compo.format = "TEXT";

                    }
                    else
                    {
                        //Get file length and mime type
                        //var response = (HttpWebResponse)(await request.GetResponseAsync());
                        byte[] Contentresult;
                        byte[] buffer = new byte[10000];

                        var request = HttpWebRequest.CreateHttp(data.headerImageUrl);
                        request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:40.0) Gecko/20100101 Firefox/40.1";

                        using (WebResponse response = request.GetResponse())
                        {
                            using (Stream responseStream = response.GetResponseStream())
                            {
                                using (MemoryStream memoryStream = new MemoryStream())
                                {
                                    int count = 0;
                                    do
                                    {
                                        count = responseStream.Read(buffer, 0, buffer.Length);
                                        memoryStream.Write(buffer, 0, count);

                                    } while (count != 0);

                                    Contentresult = memoryStream.ToArray();

                                }
                            }
                        }
                        var length = Contentresult.Length;
                        var mime = MimeTypeMap.GetMimeType(data.headerImageUrl.Split('.').Last());


                        //create session upload on whatsapp
                        StaticShared.Client.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue("Bearer", data.token);

                        var sessionRes = await StaticShared.Client.PostAsync($"https://graph.facebook.com/v17.0/{data.applicationId}/uploads?file_length={length}&file_type={mime}&access_token={data.token}", null);
                        if (sessionRes.IsSuccessStatusCode == false)
                            return new BadRequestObjectResult(new { Msg = await sessionRes.Content.ReadAsStringAsync() });
                        var Sres = JsonConvert.DeserializeObject<sessionResponse>(sessionRes.Content.ReadAsStringAsync().Result);
                        sessionFinalResponse imagestr;
                        using (var httpClient = new HttpClient())
                        {
                            using (var myrequest = new HttpRequestMessage(new HttpMethod("POST"), $"https://graph.facebook.com/v17.0/{Sres.id}"))
                            {
                                myrequest.Headers.TryAddWithoutValidation("Authorization", "OAuth " + data.token);
                                myrequest.Headers.TryAddWithoutValidation("file_offset", "0");

                                myrequest.Content = new ByteArrayContent(Contentresult);
                                myrequest.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded");

                                var response = await httpClient.SendAsync(myrequest);
                                if (!response.IsSuccessStatusCode)
                                    return null;

                                var responseContent = response.Content.ReadAsStringAsync().Result;
                                imagestr = System.Text.Json.JsonSerializer.Deserialize<sessionFinalResponse>(responseContent);
                            }
                        }




                        if (mime.Contains("image", StringComparison.OrdinalIgnoreCase))
                            compo.format = "IMAGE";
                        else if (mime.Equals("video/mp4", StringComparison.OrdinalIgnoreCase))
                            compo.format = "Video".ToUpper();
                        else if (mime.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
                            compo.format = "document".ToUpper();
                        compo.example = new Example { header_handle = new List<string> { imagestr.h } };
                    }
                    

                    template.components.Add(compo);
                }

                var js = JsonConvert.SerializeObject(template, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                var content = new StringContent(js, Encoding.UTF8, "application/json");


                var res=  await StaticShared.Client.PostAsync($"https://graph.facebook.com/v17.0/{id}",content);

                if(res.IsSuccessStatusCode== true)
                     return new OkObjectResult(await res.Content.ReadAsStringAsync());
                else return new BadRequestObjectResult(new { Msg = await res.Content.ReadAsStringAsync() });



            }
            catch (Exception e)
            {
               // logger.LogError("Connection Cannot stablishing at: " + DateTime.UtcNow + ", Client: " + client.Name);

                return new BadRequestObjectResult(new { Msg = e.Message });
            }

            //if (shared.ListClients().Count > 0)
            //{
            //    foreach (var item in shared.ListClients())
            //    {
            //        if (item.client.ConnectionState == SmppConnectionState.Connected)
            //        {

            //            StaticShared.Client.PutAsync(Environment.GetEnvironmentVariable("baseurl") + Environment.GetEnvironmentVariable("updateconnectionendpoint"), new StringContent(JsonConvert.SerializeObject(new ClientDbProperties
            //            {
            //                instanceId = item.client.Name,
            //                lastUpdate = DateTime.UtcNow,
            //                status = "Connected"
            //            }).ToString(), Encoding.UTF8, "application/json"));

            //    }
            //}
            //}




        }
        public async Task<string> GetTemplateId(string account_id, string access_token, string template_name)
        {
            string request = string.Format("https://graph.facebook.com/v16.0/" + account_id + "/message_templates?&access_token=" + access_token);
            var theToken = await StaticShared.Client.GetAsync(request);
            var tokenContent = await theToken.Content.ReadAsStringAsync();

            var data = JsonConvert.DeserializeObject<Templates2>(tokenContent);

            string template_Id="";
            
            foreach (var item in data.data)
            {
                if (item.name == template_name)
                {
                    template_Id = item.id;
                    break;
                }
            }
            return template_Id;
            
        }





    

        public class Cursors
        {
            public string before { get; set; }
            public string after { get; set; }
        }

        public class Datum2
        {
            public string name { get; set; }
            public string previous_category { get; set; }
            public List<Component2> components { get; set; }
            public string language { get; set; }
            public string status { get; set; }
            public string category { get; set; }
            public string id { get; set; }
        }

        private class Template
        {
            public string name { get; set; }
            public string language { get; set; }
            public string category { get; set; }
            public List<Component> components { get; set; }
        }

        public class Paging
        {
            public Cursors cursors { get; set; }
        }

        private class Templates2
        {
            public List<Datum> data { get; set; }
            public Paging paging { get; set; }
        }
        private class Datum
        {
            public string name { get; set; }
            public string previous_category { get; set; }
            public List<Component2> components { get; set; }
            public string language { get; set; }
            public string status { get; set; }
            public string category { get; set; }
            public string id { get; set; }
        }
        private class parameters
        {
            public string template_name { get; set; }
        }
        private class templateReturned
        {
            public string template_Name { get; set; }
            public string status { get; set; }
            public string template_content { get; set; }
            public string category { get; set; }
            public string language { get; set; }
        }

        public class Button
        {
            public string type { get; set; }
            public string text { get; set; }
            public string? url { get; set; }
        }

        public class Component2
        {
            public string type { get; set; }
            public string text { get; set; }
            public Example2? example { get; set; }
            public string format { get; set; }
            public List<Button> buttons { get; set; }
        }

        public class Example
        {
            public List<string>? header_handle { get; set; }
            public List<List<string>>? body_text { get; set; }
        }
        public class Example2
        {
            public List<List<string>> body_text { get; set; }
        }

        public class Component
        {
            public string type { get; set; }
            public string text { get; set; }
            public Example? example { get; set; }
            public string format { get; set; }
            public List<Button> buttons { get; set; }
        }
        public class Templates
        {
            public string name { get; set; }
            public string language { get; set; }
            public string category { get; set; }
            public List<Component2> components { get; set; }
        }
    }
     class WhatsappEditTemplate
    {
        //required data
        public string token { get; set; }
        public string accountId { get; set; }
        public string category { get;  set; }
        public string languageCode { get;  set; }
        public string templateName { get;  set; }
        public string bodyText { get; set; }

        public bool isHeaderImage { get; set; }
        public bool isButton { get;  set; }
        public bool isFooter { get;  set; }
        public bool isHeaderText { get;  set; }


        public List<buttonsForEdit>? buttons { get;  set; }
        public string? footerText { get;  set; }
        public string? headerText { get;  set; }
        public string? headerImageUrl { get;  set; }

        public List<string>? exampleBody { get; set; }
    }
     class buttonsForEdit
    {
        public string text { get; set; }
        public string? url { get; set; }

    }



}
