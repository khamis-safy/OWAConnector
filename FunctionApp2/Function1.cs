using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace FunctionApp2
{
    public class Function1
    {
        [FunctionName("Function1")]
        public void Run([ServiceBusTrigger("sb-dev-smpp-outgoing-queue", Connection = "Endpoint=sb://dev-servicebus-wpp.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=MPdJsbUcnafJOFMM3F1ez6xdQS/tY4LtCb48vZ7OA8w=")]string myQueueItem, ILogger log)
        {
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {myQueueItem}");
        }
    }
}
