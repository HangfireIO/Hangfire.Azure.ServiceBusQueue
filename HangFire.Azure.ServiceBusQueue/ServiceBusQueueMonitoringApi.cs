using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire.SqlServer;

namespace Hangfire.Azure.ServiceBusQueue
{
    internal class ServiceBusQueueMonitoringApi : IPersistentJobQueueMonitoringApi
    {
        private readonly ServiceBusManager _manager;
        private readonly string[] _queues;

        public ServiceBusQueueMonitoringApi(ServiceBusManager manager, string[] queues)
        {
            if (manager == null) throw new ArgumentNullException("manager");
            if (queues == null) throw new ArgumentNullException("queues");

            _manager = manager;
            _queues = queues;
        }

        public IEnumerable<string> GetQueues()
        {
            return _queues;
        }

        public IEnumerable<int> GetEnqueuedJobIds(string queue, int @from, int perPage)
        {
            var client = _manager.GetClient(queue);
            var messages = client.PeekBatch(perPage).ToArray();

            var jobIds = messages.Select(x => int.Parse(x.GetBody<string>())).ToList();

            foreach (var message in messages)
            {
                message.Dispose();
            }

            return jobIds;
        }

        public IEnumerable<int> GetFetchedJobIds(string queue, int @from, int perPage)
        {
            return Enumerable.Empty<int>();
        }

        public EnqueuedAndFetchedCountDto GetEnqueuedAndFetchedCount(string queue)
        {
            var queueDescriptor = _manager.GetDescription(queue);

            return new EnqueuedAndFetchedCountDto
            {
                EnqueuedCount = (int) queueDescriptor.MessageCount,
                FetchedCount = null
            };
        }
    }
}
