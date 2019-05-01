using System;
using Microsoft.ServiceBus.Messaging;

namespace Hangfire.Azure.ServiceBusQueue
{
    public class ServiceBusQueueOptions
    {
        public ServiceBusQueueOptions()
        {
            this.CheckAndCreateQueues = true;
        }

        /// <summary>
        /// Gets or sets the prefix that will be prepended to all queue names in
        /// the service bus (e.g. if the prefix is "a-prefix-" then the default queue
        /// will be named "a-prefix-default" in Azure)
        /// </summary>
        public string QueuePrefix { get; set; }

        /// <summary>
        /// Configures a queue on construction, for example setting maximum message
        /// size of default TTL.
        /// </summary>
        public Action<QueueDescription> Configure { get; set; }

        /// <summary>
        /// Gets or sets a value which indicates whether or not to automatically create and
        /// configure queues.
        /// </summary>
        /// <remarks>
        /// On initialisation if this property is <cc>true</cc> we will create and check all queues
        /// immediately, otherwise we delay the creation of the queue clients until they are first
        /// requested.
        /// </remarks>
        public bool CheckAndCreateQueues { get; set; }
        
        /// <summary>
        /// Gets or sets a delay between calls to the <see cref="BrokeredMessage.RenewLock"/> method
        /// to disallow workers to pick up the same background job several time while it's still
        /// processing on an active server.
        /// </summary>
        public TimeSpan? LockRenewalDelay { get; set; }

        public string ConnectionString { get; set; }

        public string[] Queues { get; set; }

        internal string GetQueueName(string name)
        {
            if (QueuePrefix != null)
            {
                return QueuePrefix + name;
            }

            return name;
        }

        internal void Validate()
        {
            if (ConnectionString == null)
                throw new InvalidOperationException("Must supply ConnectionString to ServiceBusQueueOptions");

            if (Queues == null)
                throw new InvalidOperationException("Must supply Queues to ServiceBusQueueOptions");

            if (Queues.Length == 0)
                throw new InvalidOperationException("Must supply at least one queue in Queues property of ServiceBusQueueOptions");
        }
    }
}