using System;
using System.Linq;
using System.Threading;
using System.Transactions;
using Hangfire.SqlServer;
using Hangfire.Storage;
using Microsoft.ServiceBus.Messaging;

namespace Hangfire.Azure.ServiceBusQueue
{
    internal class ServiceBusQueueJobQueue : IPersistentJobQueue
    {
        private static readonly TimeSpan SyncReceiveTimeout = TimeSpan.FromSeconds(5);

        private readonly ServiceBusManager _manager;

        public ServiceBusQueueJobQueue(ServiceBusManager manager)
        {
            if (manager == null) throw new ArgumentNullException("manager");

            _manager = manager;
        }

        public IFetchedJob Dequeue(string[] queues, CancellationToken cancellationToken)
        {
            BrokeredMessage message = null;
            var queueIndex = 0;

            var clients = queues
                .Select(queue => _manager.GetClient(queue))
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
            Transaction.Current.TransactionCompleted += (s, e) =>
            {
                // Because we are within a TransactionScope at this point the below
                // call would not work (Local transactions are not supported with other resource managers/DTC
                // exception is thrown) within being done after the transaction has completed
                var client = _manager.GetClient(queue);

                using (var message = new BrokeredMessage(jobId))
                {
                    client.Send(message);
                }
            };
        }
    }
}
