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
                    var isLastQueue = queueIndex == queues.Length - 1;

                    message = isLastQueue
                        ? client.Receive(_manager.Options.LoopReceiveTimeout) // Last queue
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

            return new ServiceBusQueueFetchedJob(message);
        }

        public void Enqueue(IDbConnection connection, string queue, string jobId)
        {
            // Because we are within a TransactionScope at this point the below
            // call would not work (Local transactions are not supported with other resource managers/DTC
            // exception is thrown) without suppression
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var client = _manager.GetClient(queue);

                using (var message = new BrokeredMessage(jobId) { MessageId = jobId })
                {
                    _manager.Options.RetryPolicy.Execute(() => client.Send(message));
                }
            }
        }
    }
}