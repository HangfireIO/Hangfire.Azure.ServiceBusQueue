using System;
using Azure.Messaging.ServiceBus.Administration;
using Hangfire.SqlServer;
using Hangfire.States;

namespace Hangfire.Azure.ServiceBusQueue
{
    public static class ServiceBusQueueSqlServerStorageExtensions
    {
        public static SqlServerStorage UseServiceBusQueues(
            this SqlServerStorage storage,
            string connectionString)
        {
            return UseServiceBusQueues(storage, new ServiceBusQueueOptions
            {
                ConnectionString = connectionString,
                Queues           = new[] { EnqueuedState.DefaultQueue }
            });
        }

        public static SqlServerStorage UseServiceBusQueues(
            this SqlServerStorage storage,
            string connectionString,
            params string[] queues)
        {
            return UseServiceBusQueues(storage, new ServiceBusQueueOptions
            {
                ConnectionString = connectionString,
                Queues           = queues
            });
        }

        public static SqlServerStorage UseServiceBusQueues(
            this SqlServerStorage storage,
            string connectionString,
            Action<CreateQueueOptions> configureAction,
            params string[] queues)
        {
            return UseServiceBusQueues(storage, new ServiceBusQueueOptions
            {
                ConnectionString = connectionString,
                Configure        = configureAction,
                Queues           = queues
            });
        }

        public static SqlServerStorage UseServiceBusQueues(
            this SqlServerStorage storage,
            ServiceBusQueueOptions options)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            if (options == null) throw new ArgumentNullException(nameof(options));

            var provider = new ServiceBusQueueJobQueueProvider(options);

            storage.QueueProviders.Add(provider, options.Queues);

            return storage;
        }
    }
}
