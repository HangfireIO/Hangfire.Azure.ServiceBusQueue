using System;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Storage;
using Microsoft.ServiceBus.Messaging;

namespace Hangfire.Azure.ServiceBusQueue
{
    internal class ServiceBusQueueFetchedJob : IFetchedJob
    {
        private readonly BrokeredMessage _message;
        private readonly CancellationTokenSource _cancellationTokenSource;

        private bool _completed;
        private bool _disposed;


        public ServiceBusQueueFetchedJob(BrokeredMessage message)
        {
            if (message == null) throw new ArgumentNullException("message");

            _message = message;
            _cancellationTokenSource = new CancellationTokenSource();

            JobId = _message.GetBody<string>();

            KeepAlive();
        }

        public string JobId { get; private set; }

        public BrokeredMessage Message => this._message;

        public void Requeue()
        {
            _cancellationTokenSource.Cancel();

            _message.Abandon();
            _completed = true;
        }

        public void RemoveFromQueue()
        {
            _cancellationTokenSource.Cancel();

            _message.Complete();
            _completed = true;
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();

            if (!_completed && !_disposed)
            {
                _message.Abandon();
            }

            _message.Dispose();
            _disposed = true;
        }

        private void KeepAlive()
        {
            Task.Run(async () =>
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    if (DateTime.UtcNow > _message.LockedUntilUtc.AddSeconds(-1))
                    {
                        _message.RenewLock();
                    }

                    await Task.Delay(500, _cancellationTokenSource.Token);
                }
            }, _cancellationTokenSource.Token);
        }
    }
}
