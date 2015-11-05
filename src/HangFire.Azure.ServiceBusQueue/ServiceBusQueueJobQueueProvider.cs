using System;
using Hangfire.SqlServer;

namespace Hangfire.Azure.ServiceBusQueue
{
    internal class ServiceBusQueueJobQueueProvider : IPersistentJobQueueProvider
    {
        private readonly ServiceBusQueueJobQueue _jobQueue;
        private readonly ServiceBusQueueMonitoringApi _monitoringApi;

        public ServiceBusQueueJobQueueProvider(ServiceBusQueueOptions options)
        {
            if (options == null) throw new ArgumentNullException("options");

            options.Validate();

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
