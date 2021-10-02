using System;
using Hangfire.Annotations;
using Hangfire.SqlServer;
using Hangfire.States;

namespace Hangfire.Azure.ServiceBusQueue
{
    public static class ServiceBusGlobalConfigurationExtensions
    {
        public static IGlobalConfiguration UseServiceBusQueues(
            [NotNull] this IGlobalConfiguration<SqlServerStorage> configuration,
            [NotNull] string connectionString)
        {
            return UseServiceBusQueues(configuration, new ServiceBusQueueOptions
            {
                ConnectionString = connectionString,
                Queues           = new[] { EnqueuedState.DefaultQueue }
            });
        }

        public static IGlobalConfiguration UseServiceBusQueues(
            [NotNull] this IGlobalConfiguration<SqlServerStorage> configuration,
            [NotNull] string connectionString,
            params string[] queues)
        {
            return UseServiceBusQueues(configuration, new ServiceBusQueueOptions
            {
                ConnectionString = connectionString,
                Queues           = queues
            });
        }

        public static IGlobalConfiguration UseServiceBusQueues(
            [NotNull] this IGlobalConfiguration<SqlServerStorage> configuration,
            [NotNull] string connectionString,
            Action<QueueDescription> configureAction,
            params string[] queues)
        {
            return UseServiceBusQueues(configuration, new ServiceBusQueueOptions
            {
                ConnectionString = connectionString,
                Configure        = configureAction,
                Queues           = queues
            });
        }

        public static IGlobalConfiguration UseServiceBusQueues(
            [NotNull] this IGlobalConfiguration<SqlServerStorage> configuration,
            [NotNull] ServiceBusQueueOptions options)
        {
            var sqlServerStorage = configuration.Entry;
            var provider         = new ServiceBusQueueJobQueueProvider(options);
            sqlServerStorage.QueueProviders.Add(provider, options.Queues);
            return configuration;
        }
    }
}
