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

        public IEnumerable<long> GetEnqueuedJobIds(string queue, int @from, int perPage)
        {
            var client = _manager.GetClient(queue);
            var jobIds = new List<long>();

            // We have to overfetch to retrieve enough messages for paging.
            // e.g. @from = 10 and page size = 20 we need 30 messages from the start
            var messages = client.PeekBatch(0, @from + perPage).ToArray();
            
            // We could use LINQ here but to avoid creating lots of garbage lists
            // through .Skip / .ToList etc. use a simple loop.
            for (var i = 0; i < messages.Length; i++)
            {
                var msg = messages[i];

                // Only include the job id once we have skipped past the @from
                // number
                if (i >= @from)
                {
                    jobIds.Add(long.Parse(msg.GetBody<string>()));
                }

                msg.Dispose();
            }

            return jobIds;
        }

        public IEnumerable<long> GetFetchedJobIds(string queue, int @from, int perPage)
        {
            return Enumerable.Empty<long>();
        }

        public EnqueuedAndFetchedCountDto GetEnqueuedAndFetchedCount(string queue)
        {
            var queueDescriptor = _manager.GetDescription(queue);

            return new EnqueuedAndFetchedCountDto
            {
                EnqueuedCount = (int) queueDescriptor.MessageCountDetails.ActiveMessageCount,
                FetchedCount = null
            };
        }
    }
}
