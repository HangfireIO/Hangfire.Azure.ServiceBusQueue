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

        private readonly Dictionary<string, QueueClient> _clients;
        private readonly ServiceBusQueueOptions _options;
        private readonly NamespaceManager _namespaceManager;

        public ServiceBusManager(ServiceBusQueueOptions options)
        {
            if (options == null) throw new ArgumentNullException("options");

            _options = options;

            _clients = new Dictionary<string, QueueClient>();
            _namespaceManager = NamespaceManager.CreateFromConnectionString(options.ConnectionString);

            CreateQueuesIfNotExists(_namespaceManager, options);
        }

        public QueueClient GetClient(string queue)
        {
            var prefixedQueue = _options.GetQueueName(queue);

            QueueClient client;

            if (!_clients.TryGetValue(prefixedQueue, out client))
            {
                Logger.InfoFormat("Creating new QueueClient for queue {0}", prefixedQueue);

                client = QueueClient.CreateFromConnectionString(_options.ConnectionString, prefixedQueue, ReceiveMode.PeekLock);

                _clients[prefixedQueue] = client;
            }

            return client;
        }

        public QueueDescription GetDescription(string queue)
        {
            return _namespaceManager.GetQueue(_options.GetQueueName(queue));
        }

        private static void CreateQueuesIfNotExists(NamespaceManager namespaceManager, ServiceBusQueueOptions options)
        {
            foreach (var queue in options.Queues)
            {
                var prefixedQueue = options.GetQueueName(queue);

                Logger.InfoFormat("Checking if queue {0} exists", prefixedQueue);

                if (!namespaceManager.QueueExists(prefixedQueue))
                {
                    Logger.InfoFormat("Creating new queue {0}", prefixedQueue);

                    var description = new QueueDescription(prefixedQueue);

                    if (options.Configure != null)
                    {
                        options.Configure(description);
                    }

                    namespaceManager.CreateQueue(description);
                }
            }
        }
    }
}
