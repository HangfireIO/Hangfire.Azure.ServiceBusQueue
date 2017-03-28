using System.Configuration;
using Hangfire.Azure.ServiceBusQueue;

namespace HangFire.Azure.ServiceBusQueue.Tests
{
    public class TestServiceBusQueueOptions : ServiceBusQueueOptions
    {
        public TestServiceBusQueueOptions()
        {
            CheckAndCreateQueues = true;
            ConnectionString = ConfigurationManager.AppSettings["BusConnectionString"];
            QueuePrefix = "hf-sb-tests-";
            Queues = new[] {"test1", "test2", "test3"};
        }
    }
}