using System;
using System.Linq;
using System.Threading;
using HangFire.SqlServer;
using HangFire.Storage;
using Microsoft.ServiceBus.Messaging;

namespace HangFire.Azure.ServiceBusQueue
{
    internal class ServiceBusQueueJobQueue : IPersistentJobQueue
    {
        private static readonly TimeSpan SyncReceiveTimeout = TimeSpan.FromSeconds(5);
        private readonly string _connectionString;

        public ServiceBusQueueJobQueue(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IFetchedJob Dequeue(string[] queues, CancellationToken cancellationToken)
        {
            BrokeredMessage message = null;
            var queueIndex = 0;

            var clients = queues
                .Select(queue => QueueClient.CreateFromConnectionString(_connectionString, queue))
                .ToArray();

            do
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var client = clients[queueIndex];

                    message = queueIndex == 0
                        ? client.Receive(SyncReceiveTimeout)
                        : client.Receive(new TimeSpan(1));
                }
                catch (TimeoutException)
                {
                }

                queueIndex = (queueIndex + 1) % queues.Length;
            } while (message == null);

            return new ServiceBusQueueFetchedJob(message);
        }

        public void Enqueue(string queue, string jobId)
        {
            var client = QueueClient.CreateFromConnectionString(_connectionString, queue);

            using (var message = new BrokeredMessage(jobId))
            {
                client.Send(message);   
            }
        }
    }
}
