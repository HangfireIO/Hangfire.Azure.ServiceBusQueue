using System;
using System.Collections.Generic;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;

namespace Hangfire.Azure.ServiceBusQueue
{
    internal class ServiceBusManager
    {
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
                var prefixed = options.GetQueueName(queue);

                if (!namespaceManager.QueueExists(prefixed))
                {
                    var description = new QueueDescription(prefixed);

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
