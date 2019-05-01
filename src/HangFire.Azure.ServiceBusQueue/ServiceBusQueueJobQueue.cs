using System;
using System.Linq;
using System.Threading;
using System.Transactions;
using Hangfire.SqlServer;
using Hangfire.Storage;
using Microsoft.ServiceBus.Messaging;
using System.Data;

namespace Hangfire.Azure.ServiceBusQueue
{
    internal class ServiceBusQueueJobQueue : IPersistentJobQueue
    {
        private static readonly TimeSpan MinSyncReceiveTimeout = TimeSpan.FromTicks(1);
        private static readonly TimeSpan SyncReceiveTimeout = TimeSpan.FromSeconds(5);

        private readonly ServiceBusManager _manager;
        private readonly ServiceBusQueueOptions _options;

        public ServiceBusQueueJobQueue(ServiceBusManager manager, ServiceBusQueueOptions options)
        {
            if (manager == null) throw new ArgumentNullException("manager");
            if (options == null) throw new ArgumentNullException("options");

            _manager = manager;
            _options = options;
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

                    message = queueIndex == queues.Length - 1
                        ? client.Receive(SyncReceiveTimeout)
                        : client.Receive(MinSyncReceiveTimeout);
                }
                catch (TimeoutException)
                {
                }
                catch (MessagingEntityNotFoundException ex)
                {
                    var errorMessage = string.Format(
                        "Queue {0} could not be found. Either create the queue manually, " +
                        "or grant the Manage permission and set ServiceBusQueueOptions.CheckAndCreateQueues to true", 

                        clients[queueIndex].Path);

                    throw new UnauthorizedAccessException(errorMessage, ex);
                }

                queueIndex = (queueIndex + 1) % queues.Length;
            } while (message == null);

            return new ServiceBusQueueFetchedJob(message, _options.LockRenewalDelay);
        }

        public void Enqueue(IDbConnection connection, string queue, string jobId)
        {
            // Because we are within a TransactionScope at this point the below
            // call would not work (Local transactions are not supported with other resource managers/DTC
            // exception is thrown) without suppression
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var client = _manager.GetClient(queue);

                using (var message = new BrokeredMessage(jobId))
                {
                    client.Send(message);
                }
            }
        }
    }
}
