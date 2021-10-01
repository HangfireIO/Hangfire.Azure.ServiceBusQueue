using System;
using Azure.Messaging.ServiceBus.Administration;

namespace Hangfire.Azure.ServiceBusQueue
{
    /// <summary>
    /// <inheritdoc cref="CreateQueueOptions"/>
    /// </summary>
    public class QueueDescription : CreateQueueOptions
    {
        public QueueDescription(string name) : base(name)
        {
        }

        public QueueDescription(QueueProperties queue) : base(queue)
        {
        }

        public string Path
        {
            get => Name;
            set => Name = value;
        }

        public bool EnableDeadLetteringOnMessageExpiration
        {
            get => DeadLetteringOnMessageExpiration;
            set => DeadLetteringOnMessageExpiration = value;
        }

        [Obsolete("This property is no longer in use in the latest azure SDK and has no effect on the queue creation")]
        public bool EnableExpress { get; set; }

    }
}
