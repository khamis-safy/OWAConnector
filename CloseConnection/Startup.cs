using Azure.Messaging.ServiceBus;
using WhatsappConnector;
using WhatsappConnector.HelperClasses;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;
using Newtonsoft.Json;
using SMPPClientConnection.HelperClasses;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;

[assembly: FunctionsStartup(typeof(Startup))]
namespace WhatsappConnector
{
    public class Startup : FunctionsStartup
    {
       // shared any = new shared();

      
        public override void Configure(IFunctionsHostBuilder builder)
        {
            StaticShared.ClientForIncoming.DefaultRequestHeaders.Add(Environment.GetEnvironmentVariable("SID"), Environment.GetEnvironmentVariable("TOKEN"));

            Environment.SetEnvironmentVariable("GetToken", "api/OWA/GetOWAToken");
            builder.Services.AddSingleton<Ishared, shared>();

            builder.Services.AddApplicationInsightsTelemetry(Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY"));

            //builder.Services.AddLogging();


        }






















    }
}