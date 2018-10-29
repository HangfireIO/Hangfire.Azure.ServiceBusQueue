using System;
using System.Collections.Generic;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Hangfire.Logging;

namespace Hangfire.Azure.ServiceBusQueue
{
    internal class ServiceBusManager
    {
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();

        // Stores the pre-created QueueClients (note the key is the unprefixed queue name)
        private readonly Dictionary<string, QueueClient> _clients;

        private readonly ServiceBusQueueOptions _options;
        private readonly NamespaceManager _namespaceManager;
        private readonly MessagingFactory _messagingFactory;

        public ServiceBusManager(ServiceBusQueueOptions options)
        {
            if (options == null) throw new ArgumentNullException("options");

            _options = options;

            _clients = new Dictionary<string, QueueClient>(options.Queues.Length);
            _namespaceManager = NamespaceManager.CreateFromConnectionString(options.ConnectionString);
            _messagingFactory = MessagingFactory.CreateFromConnectionString(options.ConnectionString);

            // If we have this option set to true then we will create all clients up-front, otherwise
            // the creation will be delayed until the first client is retrieved
            if (options.CheckAndCreateQueues)
            {
                CreateQueueClients();
            }
        }

        public ServiceBusQueueOptions Options { get { return _options; } }

        public QueueClient GetClient(string queue)
        {
            if (_clients.Count != _options.Queues.Length)
            {
                CreateQueueClients();
            }

            return _clients[queue];
        }

        public QueueDescription GetDescription(string queue)
        {
            return _namespaceManager.GetQueue(_options.GetQueueName(queue));
        }

        private void CreateQueueClients()
        {
            foreach (var queue in _options.Queues)
            {
                var prefixedQueue = _options.GetQueueName(queue);

                CreateQueueIfNotExists(prefixedQueue, _namespaceManager, _options);

                Logger.TraceFormat("Creating new QueueClient for queue {0}", prefixedQueue);

                // Do not store as prefixed queue to avoid having to re-create name in GetClient method
                _clients[queue] = this._messagingFactory.CreateQueueClient(prefixedQueue, ReceiveMode.PeekLock);
            }
        }

        private static void CreateQueueIfNotExists(string prefixedQueue, NamespaceManager namespaceManager, ServiceBusQueueOptions options)
        {
            if (options.CheckAndCreateQueues == false)
            {
                Logger.InfoFormat("Not checking for the existence of the queue {0}", prefixedQueue);

                return;
            }

            try
            {
                Logger.InfoFormat("Checking if queue {0} exists", prefixedQueue);

                if (namespaceManager.QueueExists(prefixedQueue))
                {
                    return;
                }

                Logger.InfoFormat("Creating new queue {0}", prefixedQueue);

                var description = new QueueDescription(prefixedQueue);

                if (options.Configure != null)
                {
                    options.Configure(description);
                }

                namespaceManager.CreateQueue(description);
            }
            catch (UnauthorizedAccessException ex)
            {
                var errorMessage = string.Format(
                    "Queue '{0}' could not be checked / created, likely due to missing the 'Manage' permission. " +
                    "You must either grant the 'Manage' permission, or set ServiceBusQueueOptions.CheckAndCreateQueues to false",
                    prefixedQueue);

                throw new UnauthorizedAccessException(errorMessage, ex);
            }
        }
    }
}
