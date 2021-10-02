using System;
using System.Collections.Generic;
using Hangfire.Logging;
using System.Threading.Tasks;
using Azure;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

namespace Hangfire.Azure.ServiceBusQueue
{
    internal class ServiceBusManager
    {
        private readonly ILog _logger = LogProvider.GetCurrentClassLogger();

        // Stores the pre-created QueueClients (note the key is the unprefixed queue name)
        private readonly Dictionary<string, ServiceBusSender> _senders;
        private readonly Dictionary<string, ServiceBusReceiver> _receivers;

        private readonly ServiceBusAdministrationClient _managementAdminClient;
        private readonly ServiceBusClient _managementClient;

        public ServiceBusManager(ServiceBusQueueOptions options)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));

            _senders               = new Dictionary<string, ServiceBusSender>(options.Queues.Length);
            _receivers             = new Dictionary<string, ServiceBusReceiver>(options.Queues.Length);
            _managementClient      = new ServiceBusClient(options.ConnectionString);
            _managementAdminClient = new ServiceBusAdministrationClient(options.ConnectionString);

            if (options.CheckAndCreateQueues)
            {
                AsyncHelper.RunSync(CreateQueueClients);
            }
        }

        ~ServiceBusManager()
        {
            foreach (var sender in _senders)
            {
                AsyncHelper.RunSync(() => sender.Value.CloseAsync());
            }
            foreach (var receiver in _receivers)
            {
                AsyncHelper.RunSync(() => receiver.Value.CloseAsync());
            }
            _managementClient.DisposeAsync();
        }

        public ServiceBusQueueOptions Options { get; }

        public async Task<ServiceBusSender> GetSenderAsync(string queue)
        {
            if (_senders.Count != Options.Queues.Length)
            {
                await CreateQueueClients().ConfigureAwait(false);
            }
            return _senders[queue];
        }

        public async Task<ServiceBusReceiver> GetReceiverAsync(string queue)
        {
            if (_receivers.Count != Options.Queues.Length)
            {
                await CreateQueueClients().ConfigureAwait(false);
            }

            return _receivers[queue];
        }

        public async Task<Response<QueueRuntimeProperties>> GetQueueRuntimeInfoAsync(string queue)
        {
            return await _managementAdminClient.GetQueueRuntimePropertiesAsync(Options.GetQueueName(queue));
        }

        private async Task CreateQueueClients()
        {
            foreach (var queue in Options.Queues)
            {
                var prefixedQueue = Options.GetQueueName(queue);

                await CreateQueueIfNotExistsAsync(prefixedQueue).ConfigureAwait(false);

                _logger.TraceFormat("Creating new Senders and Receivers for queue {0}", prefixedQueue);

                if (!_senders.ContainsKey(queue))
                    _senders.Add(queue, _managementClient.CreateSender(prefixedQueue));

                if (!_receivers.ContainsKey(queue))
                    _receivers.Add(queue, _managementClient.CreateReceiver(prefixedQueue));
            }
        }

        private async Task CreateQueueIfNotExistsAsync(string prefixedQueue)
        {
            if (Options.CheckAndCreateQueues == false)
            {
                _logger.InfoFormat("Not checking for the existence of the queue {0}", prefixedQueue);
                return;
            }
            try
            {
                _logger.InfoFormat("Checking if queue {0} exists", prefixedQueue);

                if (await _managementAdminClient.QueueExistsAsync(prefixedQueue).ConfigureAwait(false))
                {
                    return;
                }

                _logger.InfoFormat("Creating new queue {0}", prefixedQueue);

                var queueOptions = new QueueDescription(prefixedQueue);
                if (Options.RequiresDuplicateDetection != null)
                {
                    queueOptions.RequiresDuplicateDetection = Options.RequiresDuplicateDetection.Value;
                }

                Options.Configure?.Invoke(queueOptions);

                await _managementAdminClient.CreateQueueAsync(queueOptions).ConfigureAwait(false);
            }
            catch (UnauthorizedAccessException ex)
            {
                var errorMessage =
                    $"Queue '{prefixedQueue}' could not be checked / created, likely due to missing the 'Manage' permission. " +
                    "You must either grant the 'Manage' permission, or set ServiceBusQueueOptions.CheckAndCreateQueues to false";

                throw new UnauthorizedAccessException(errorMessage, ex);
            }
        }
    }
}
