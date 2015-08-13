using System;
using Hangfire.Storage;
using Microsoft.ServiceBus.Messaging;

namespace Hangfire.Azure.ServiceBusQueue
{
    internal class ServiceBusQueueFetchedJob : IFetchedJob
    {
        private readonly BrokeredMessage _message;
        private bool _completed;
        private bool _disposed;

        public ServiceBusQueueFetchedJob(BrokeredMessage message)
        {
            if (message == null) throw new ArgumentNullException("message");

            _message = message;

            JobId = _message.GetBody<string>();
        }

        public string JobId { get; private set; }

        public void Requeue()
        {
            _message.Abandon();
            _completed = true;
        }

        public void RemoveFromQueue()
        {
            _message.Complete();
            _completed = true;
        }

        public void Dispose()
        {
            if (!_completed && !_disposed)
            {
                _message.Abandon();
            }

            _message.Dispose();
            _disposed = true;
        }
    }
}
