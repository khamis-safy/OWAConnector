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
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Extensions;

namespace WhatsappConnector
{

    public class FunctionHeartBeat
    {
        private readonly Ishared shared;
        private readonly ILogger<FunctionHeartBeat> logger;

        public FunctionHeartBeat(Ishared shared, ILogger<FunctionHeartBeat> logger)
        {
            this.shared = shared;
            this.logger = logger;
        }

        [FunctionName("HeartBeat")]
        public async Task<ActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req)
        {
            try
            {
                var query = new parameters
                {
                    access_token = req.Query["token"],
                    account_id = req.Query["accountId"],
                    number=req.Query["number"]
                };

                var phoneListRes = await StaticShared.Client.GetAsync($"https://graph.facebook.com/v16.0/{query.account_id}/phone_numbers?access_token={query.access_token}");
                var phoneList = JsonConvert.DeserializeObject<PhoneNumbers>(phoneListRes.Content.ReadAsStringAsync().Result);
                string numberId = "";
                foreach (var item in phoneList.data)
                {
                    if (NormalizeNumber(item.display_phone_number) == NormalizeNumber(query.number))
                    {
                        numberId = item.id;
                        break;
                    }
                }
                if(numberId.IsNullOrWhiteSpace()) {
                    return new BadRequestObjectResult(new { Msg ="phone number is not exists into this account"});
                }
                return new OkObjectResult(new { Msg = "HeartBeat success" });

            }
            catch (Exception e)
            {
                return new BadRequestObjectResult(new { Msg = e.Message });
            }

        }
        private string NormalizeNumber(string number)
        {
            return new string(number.Where(char.IsDigit).ToArray());
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
            public string number { get; set; }
        }
        private class templateReturned
        {
            public string template_Name { get; set; }
            public string template_description { get; set; }
            public string status { get; set; }
        }
    }
}