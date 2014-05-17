using System;
using System.Collections.Generic;
using System.Linq;
using HangFire.SqlServer;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;

namespace HangFire.Azure.ServiceBusQueue
{
    internal class ServiceBusQueueMonitoringApi : IPersistentJobQueueMonitoringApi
    {
        private readonly string _connectionString;
        private readonly string[] _queues;

        public ServiceBusQueueMonitoringApi(string connectionString, string[] queues)
        {
            if (connectionString == null) throw new ArgumentNullException("connectionString");
            if (queues == null) throw new ArgumentNullException("queues");

            _connectionString = connectionString;
            _queues = queues;
        }

        public IEnumerable<string> GetQueues()
        {
            return _queues;
        }

        public IEnumerable<int> GetEnqueuedJobIds(string queue, int @from, int perPage)
        {
            var client = QueueClient.CreateFromConnectionString(_connectionString, queue);
            var messages = client.PeekBatch(perPage).ToArray();

            var result = messages.Select(x => int.Parse(x.GetBody<string>()));

            foreach (var message in messages)
            {
                message.Dispose();
            }

            return result;
        }

        public IEnumerable<int> GetFetchedJobIds(string queue, int @from, int perPage)
        {
            return Enumerable.Empty<int>();
        }

        public EnqueuedAndFetchedCountDto GetEnqueuedAndFetchedCount(string queue)
        {
            var namespaceManager = NamespaceManager.CreateFromConnectionString(_connectionString);
            var queueDescriptor = namespaceManager.GetQueue(queue);

            return new EnqueuedAndFetchedCountDto
            {
                EnqueuedCount = (int) queueDescriptor.MessageCount,
                FetchedCount = null
            };
        }
    }
}
