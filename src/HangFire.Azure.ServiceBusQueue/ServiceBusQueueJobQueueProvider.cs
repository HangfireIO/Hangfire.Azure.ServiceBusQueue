using System;
using Hangfire.SqlServer;

namespace Hangfire.Azure.ServiceBusQueue
{
    using Hangfire.Logging;

    internal class ServiceBusQueueJobQueueProvider : IPersistentJobQueueProvider
    {
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();

        private readonly ServiceBusQueueJobQueue _jobQueue;
        private readonly ServiceBusQueueMonitoringApi _monitoringApi;

        public ServiceBusQueueJobQueueProvider(ServiceBusQueueOptions options)
        {
            if (options == null) throw new ArgumentNullException("options");

            options.Validate();

            Logger.Info("Using the following options for Azure service bus:");
            Logger.InfoFormat("    Queue prefix: {0}", options.QueuePrefix);
            Logger.InfoFormat("    Queues: [{0}]", string.Join(", ", options.Queues));

            var manager = new ServiceBusManager(options);

            _jobQueue = new ServiceBusQueueJobQueue(manager);
            _monitoringApi = new ServiceBusQueueMonitoringApi(manager, options.Queues);
        }

        public IPersistentJobQueue GetJobQueue()
        {
            return _jobQueue;
        }

        public IPersistentJobQueueMonitoringApi GetJobQueueMonitoringApi()
        {
            return _monitoringApi;
        }
    }
}
