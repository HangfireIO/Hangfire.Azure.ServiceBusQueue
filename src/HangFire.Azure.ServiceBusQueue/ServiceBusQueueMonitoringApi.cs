using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire.SqlServer;

namespace Hangfire.Azure.ServiceBusQueue
{
    internal class ServiceBusQueueMonitoringApi : IPersistentJobQueueMonitoringApi
    {
        private readonly ServiceBusManager _manager;
        private readonly string[] _queues;

        public ServiceBusQueueMonitoringApi(ServiceBusManager manager, string[] queues)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _queues  = queues ?? throw new ArgumentNullException(nameof(queues));
        }

        public IEnumerable<string> GetQueues()
        {
            return _queues;
        }

        public IEnumerable<long> GetEnqueuedJobIds(string queue, int from, int perPage)
        {
            return AsyncHelper.RunSync(() => GetEnqueuedJobIdsAsync(queue, from, perPage));
        }

        private async Task<IEnumerable<long>> GetEnqueuedJobIdsAsync(string queue, int from, int perPage)
        {
            var receiver = await _manager.GetReceiverAsync(queue).ConfigureAwait(false);

            var jobIds = new List<long>();

            // Hangfire api require a 0 based index for @from, but PeekMessageAsync is 1 based
            var messages = await receiver.PeekMessagesAsync(perPage, from + 1).ConfigureAwait(false);

            foreach (var msg in messages)
            {
                if (long.TryParse(msg.Body.ToString(), out var longJobId))
                {
                    jobIds.Add(longJobId);
                }
            }

            return jobIds;
        }

        public IEnumerable<long> GetFetchedJobIds(string queue, int from, int perPage)
        {
            return Enumerable.Empty<long>();
        }

        public EnqueuedAndFetchedCountDto GetEnqueuedAndFetchedCount(string queue)
        {
            return AsyncHelper.RunSync(() => GetEnqueuedAndFetchedCountAsync(queue));
        }

        private async Task<EnqueuedAndFetchedCountDto> GetEnqueuedAndFetchedCountAsync(string queue)
        {
            var queueRuntimeInfo = await _manager.GetQueueRuntimeInfoAsync(queue).ConfigureAwait(false);

            return new EnqueuedAndFetchedCountDto
            {
                EnqueuedCount = (int)queueRuntimeInfo.Value.ActiveMessageCount,
                FetchedCount  = null
            };
        }
    }
}
