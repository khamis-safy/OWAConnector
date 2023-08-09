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
using System.Net.Http.Headers;

namespace WhatsappConnector
{

    public class FunctionDeleteTemplate
    {
        private readonly Ishared shared;
        private readonly ILogger<FunctionDeleteTemplate> logger;

        public FunctionDeleteTemplate(Ishared shared, ILogger<FunctionDeleteTemplate> logger)
        {
            this.shared = shared;
            this.logger = logger;
        }

        [FunctionName("DeleteTemplate")]
        public async Task<ActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = null)] HttpRequest req)
        {
            try
            {
                var query = new parameters
                {
                    access_token = req.Query["token"],
                    account_id = req.Query["accountId"],
                    template_name=req.Query["templateName"]
                };

                string request = string.Format("https://graph.facebook.com/v16.0/"+query.account_id+"/message_templates?&name="+query.template_name);
                StaticShared.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", query.access_token);

                var theToken = await StaticShared.Client.DeleteAsync(request);
                if (!theToken.IsSuccessStatusCode)
                {
                    return new BadRequestObjectResult(new { Msg = theToken.Content.ReadAsStringAsync().Result });
                }
                return new OkObjectResult(new { Msg = "the template: "+query.template_name+", has been deleted successfully" });

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
            public List<List<string>> body_text { get; set; }
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
            public string template_name { get; set; }
        }
        private class templateReturned
        {
            public string template_Name { get; set; }
            public string template_description { get; set; }
            public string status { get; set; }
        }
    }
}