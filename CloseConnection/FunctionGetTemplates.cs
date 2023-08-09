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
using System.Threading.Tasks;
using System.IO;
using Microsoft.Identity.Client;
using System.Text.RegularExpressions;

namespace WhatsappConnector
{

    public class FunctionGetTemplate
    {
        private readonly Ishared shared;
        private readonly ILogger<FunctionGetTemplate> logger;

        public FunctionGetTemplate(Ishared shared, ILogger<FunctionGetTemplate> logger)
        {
            this.shared = shared;
            this.logger = logger;
        }

        [FunctionName("GetTemplates")]
        public async Task<ActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req)
        {
            try
            {
                var query = new parameters
                {
                    access_token = req.Query["token"],
                    account_id = req.Query["accountId"],
                    isApproved=bool.Parse(req.Query["isApproved"])
                };

                string request = string.Format("https://graph.facebook.com/v16.0/"+query.account_id+"/message_templates?&access_token="+query.access_token);
                var theToken = await StaticShared.Client.GetAsync(request);
                var tokenContent = await theToken.Content.ReadAsStringAsync();

                var data = JsonConvert.DeserializeObject<Templates>(tokenContent);

                List<templateReturned> templates = new List<templateReturned>();
             
                
                List<WhatsappCreateTemplate> templatesList = new List<WhatsappCreateTemplate>();

                if (!query.isApproved)
                {
                    foreach (var item in data.data)
                    {
                        var template = new WhatsappCreateTemplate()
                        {
                            category = item.category,
                            languageCode = item.language,
                            templateName = item.name,
                            status= item.status,
                        };

                        foreach (var component in item.components)
                        {
                            if (component.type == "HEADER")
                            {
                                if (component.format == "TEXT")
                                {
                                    template.isHeaderText = true;
                                    template.headerText = component.text;
                                }
                                else if(component.format=="IMAGE" || component.format.Equals("video",StringComparison.OrdinalIgnoreCase) || component.format.Equals("document", StringComparison.OrdinalIgnoreCase))
                                {
                                    template.headerImageUrl = component.example.header_handle.First();
                                    //do logic to get the image
                                    template.isHeaderImage= true;
                                }

                            }
                            else if (component.type == "FOOTER")
                            {
                                template.isFooter = true;
                                template.footerText = component.text;
                            }
                            else if (component.type == "BUTTONS")
                            {
                                template.buttons = new List<Button>();
                                foreach (var button in component.buttons)
                                {
                                    if (button.type == "QUICK_REPLY")
                                    {
                                        template.isButtonReply = true;
                                        template.buttons.Add(button);
                                    }
                                    else if (button.type == "URL")
                                    {
                                        template.isButtonURL = true;
                                        template.buttons.Add(button);

                                    }
                                }
                            }
                            else if (component.type == "BODY")
                            {
                                string body = component.text;
                                var extractData = Regex.Matches(body, @"\{{(.*?)\}}");
                                foreach (Match item2 in extractData)
                                {
                                    body = body.Replace(item2.Value, "{}");
                                }
                                template.bodyText = body;
                            }

                        }
                        templatesList.Add(template);
                    }

                    return new OkObjectResult(templatesList);

                }


                string template_Name;
                string template_description;
               
                foreach (var item in data.data)
                {
                    if (item.status.Equals("APPROVED"))
                    {
                        template_Name = item.name;
                        var body = item.components.Where(x => x.type == "BODY").First().text;
                        var extractData = Regex.Matches(body, @"\{{(.*?)\}}");
                        int c = 1;
                        foreach (Match item2 in extractData)
                        {
                            body = body.Replace(item2.Value, "{}");
                            c++;
                        }
                        template_description = body;
                        
                        templates.Add(new templateReturned { template_content = template_description, template_Name = template_Name,status = item.status, language=item.language, category=item.category });
                    }
                }

                
                return new OkObjectResult(templates);

            }
            catch (Exception e)
            {
                return new BadRequestObjectResult(new { Msg = e.Message });
            }

        }
        public class Button
        {
            public string type { get; set; }
            public string text { get; set; }
            public string? url { get; set; }
        }

        public class Component
        {
            public string type { get; set; }
            public string format { get; set; }
            public string text { get; set; }
            public Example example { get; set; }
            public List<Button> buttons { get; set; }
        }

        public class Cursors
        {
            public string before { get; set; }
            public string after { get; set; }
        }

        public class Datum
        {
            public string name { get; set; }
            public string previous_category { get; set; }
            public List<Component> components { get; set; }
            public string language { get; set; }
            public string status { get; set; }
            public string category { get; set; }
            public string id { get; set; }
        }

        public class Example
        {
            public List<string>? header_handle { get; set; }
            public List<List<string>>? body_text { get; set; }
        }


        public class Paging
        {
            public Cursors cursors { get; set; }
        }

        public class Templates
        {
            public List<Datum> data { get; set; }
            public Paging paging { get; set; }
        }

        private class parameters
        {
            public string access_token { get; set; }
            public string account_id { get; set; }
            public bool isApproved { get; set; }
        }
        private class templateReturned
        {
            public string template_Name { get; set; }
            public string status { get; set; }
            public string template_content { get; set; }
            public string category { get; set; }
            public string language { get; set; }
        }

        class WhatsappCreateTemplate
        {
            //required data
            public string status { get; set; }
            public string category { get; set; }
            public string languageCode { get; set; }
            public string templateName { get; set; }
            public string bodyText { get; set; }


            public bool isHeaderImage { get; set; }=false;
            public bool isButtonURL { get; set; } = false;
            public bool isButtonReply { get; set; } = false;
            public bool isFooter { get; set; } = false;
            public bool isHeaderText { get; set; } = false;


            public List<Button>? buttons { get; set; }
            public string? footerText { get; set; }
            public string? headerText { get; set; }
            public string? headerImageUrl { get; set; }

        }
    }
}