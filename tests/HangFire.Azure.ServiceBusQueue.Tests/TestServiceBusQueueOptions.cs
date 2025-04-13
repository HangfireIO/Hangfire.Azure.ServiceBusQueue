using System;
using System.IO;
using Hangfire.Azure.ServiceBusQueue;
using Microsoft.Extensions.Configuration;
using Azure.Identity;

namespace HangFire.Azure.ServiceBusQueue.Tests
{
    public class TestServiceBusQueueOptions : ServiceBusQueueOptions
    {
        public TestServiceBusQueueOptions()
        {
            var configBuilder = new ConfigurationBuilder()
                                 .SetBasePath(Directory.GetCurrentDirectory())
                                 .AddJsonFile("appsettings.json").Build();

            CheckAndCreateQueues = true;
            ConnectionString     = configBuilder["BusConnectionString"];
            Credential           = configBuilder["UseDefaultAzureCredential"] == "true" ? new DefaultAzureCredential() : null;
            QueuePrefix          = "hf-sb-tests-";
            Queues               = new[] { "test1", "test2", "test3" };
            QueuePollInterval    = TimeSpan.Zero;
        }
    }
}
