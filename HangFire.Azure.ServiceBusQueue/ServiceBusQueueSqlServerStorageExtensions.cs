using System;
using HangFire.SqlServer;
using HangFire.States;
using Microsoft.ServiceBus.Messaging;

namespace HangFire.Azure.ServiceBusQueue
{
    public static class ServiceBusQueueSqlServerStorageExtensions
    {
        public static SqlServerStorage UseServiceBusQueues(
            this SqlServerStorage storage,
            string connectionString)
        {
            return UseServiceBusQueues(storage, connectionString, new[] { EnqueuedState.DefaultQueue });
        }

        public static SqlServerStorage UseServiceBusQueues(
            this SqlServerStorage storage,
            string connectionString,
            params string[] queues)
        {
            return UseServiceBusQueues(storage, connectionString, null, queues);
        }

        public static SqlServerStorage UseServiceBusQueues(
            this SqlServerStorage storage,
            string connectionString,
            Action<QueueDescription> configureAction,
            params string[] queues)
        {
            if (storage == null) throw new ArgumentNullException("storage");
            if (connectionString == null) throw new ArgumentNullException("connectionString");

            var provider = new ServiceBusQueueJobQueueProvider(
                connectionString, configureAction, queues);

            storage.QueueProviders.Add(provider, queues);

            return storage;
        }
    }
}
