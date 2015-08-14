using System;
using Microsoft.ServiceBus.Messaging;

namespace Hangfire.Azure.ServiceBusQueue
{
    public class ServiceBusQueueOptions
    {
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