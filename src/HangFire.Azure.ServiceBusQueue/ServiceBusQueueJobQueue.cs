using System;
using System.Linq;
using System.Threading;
using System.Transactions;
using Hangfire.SqlServer;
using Hangfire.Storage;
using System.Threading.Tasks;
using System.Data.Common;
using Azure.Messaging.ServiceBus;
using Hangfire.Logging;

namespace Hangfire.Azure.ServiceBusQueue
{
    internal class ServiceBusQueueJobQueue : IPersistentJobQueue
    {
        private readonly ServiceBusManager _manager;
        private readonly ServiceBusQueueOptions _options;
        private readonly ILog _logger = LogProvider.GetCurrentClassLogger();

        public ServiceBusQueueJobQueue(ServiceBusManager manager, ServiceBusQueueOptions options)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public IFetchedJob Dequeue(string[] queues, CancellationToken cancellationToken)
        {
            if (queues == null)
                throw new ArgumentNullException(nameof(queues));

            if (queues.Length == 0)
                throw new ArgumentException("Queue array must not be empty.", nameof(queues));

            return Task.Run(async () =>
            {
                var queueIndex = 0;
                var receivers = await Task.WhenAll(queues.Select(queue => _manager.GetReceiverAsync(queue))).ConfigureAwait(false);

                do
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var busReceiver = receivers[queueIndex];

                    try
                    {
                        var message = await busReceiver.ReceiveMessageAsync(_manager.Options.LoopReceiveTimeout, cancellationToken)
                                                       .ConfigureAwait(false);

                        if (message != null)
                        {
                            _logger.Info(
                                $"{DateTime.Now} - Dequeue one message from queue {busReceiver.EntityPath} with body {message.Body}");
                            return new ServiceBusQueueFetchedJob(busReceiver, message, _options.LockRenewalDelay);
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        _logger.Warn($"a cancellation has been requested for the queue {busReceiver.EntityPath}");
                    }
                    catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.ServiceTimeout)
                    {
                        _logger.Warn("Servicebus timeout dequeuing one message from queue {busReceiver.EntityPath}");
                    }
                    catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
                    {
                        var errorMessage =
                            $"Queue {busReceiver.EntityPath} could not be found. Either create the queue manually, " +
                            "or grant the Manage permission and set ServiceBusQueueOptions.CheckAndCreateQueues to true";

                        throw new UnauthorizedAccessException(errorMessage, ex);
                    }

                    queueIndex = (queueIndex + 1) % queues.Length;

                    await Task.Delay(_options.QueuePollInterval).ConfigureAwait(false);

                } while (true);
            }).GetAwaiter().GetResult();
        }

        public void Enqueue(DbConnection connection, DbTransaction transaction, string queue, string jobId)
        {
            // Because we are within a TransactionScope at this point the below
            // call would not work (Local transactions are not supported with other resource managers/DTC
            // exception is thrown) without suppression
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                AsyncHelper.RunSync(() => DoEnqueueAsync(queue, jobId));
            }
        }

        private async Task DoEnqueueAsync(string queue, string jobId)
        {
            var sender = await _manager.GetSenderAsync(queue).ConfigureAwait(false);

            var message = new ServiceBusMessage(jobId) { MessageId = jobId };
            await _manager.Options.RetryPolicy.Execute(() => sender.SendMessageAsync(message)).ConfigureAwait(false);
            _logger.Info($"{DateTime.Now} - Enqueue one message to queue {sender.EntityPath} with body {jobId}");
        }
    }
}
