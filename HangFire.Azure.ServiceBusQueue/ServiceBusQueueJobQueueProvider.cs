using System;
using System.Data;
using HangFire.SqlServer;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;

namespace HangFire.Azure.ServiceBusQueue
{
    internal class ServiceBusQueueJobQueueProvider : IPersistentJobQueueProvider
    {
        private readonly string _connectionString;
        private readonly Action<QueueDescription> _configureAction;
        private readonly string[] _queues;

        public ServiceBusQueueJobQueueProvider(
            string connectionString, 
            Action<QueueDescription> configureAction,
            string[] queues)
        {
            if (connectionString == null) throw new ArgumentNullException("connectionString");
            if (queues == null) throw new ArgumentNullException("queues");

            _connectionString = connectionString;
            _configureAction = configureAction;
            _queues = queues;

            CreateQueuesIfNotExists();
        }

        public IPersistentJobQueue GetJobQueue(IDbConnection connection)
        {
            return new ServiceBusQueueJobQueue(_connectionString);
        }

        public IPersistentJobQueueMonitoringApi GetJobQueueMonitoringApi(IDbConnection connection)
        {
            return new ServiceBusQueueMonitoringApi(_connectionString, _queues);
        }

        private void CreateQueuesIfNotExists()
        {
            foreach (var queue in _queues)
            {
                var namespaceManager =
                    NamespaceManager.CreateFromConnectionString(_connectionString);

                if (!namespaceManager.QueueExists(queue))
                {
                    var description = new QueueDescription(queue);

                    if (_configureAction != null)
                    {
                        _configureAction(description);
                    }

                    namespaceManager.CreateQueue(description);
                }
            }
        }
    }
}
