using System;
using System.Threading;
using Hangfire.Storage;
using Microsoft.ServiceBus.Messaging;
using System.Threading.Tasks;

namespace Hangfire.Azure.ServiceBusQueue
{
    internal class ServiceBusQueueFetchedJob : IFetchedJob
    {
        private readonly BrokeredMessage _message;
        private bool _completed;
        private bool _disposed;
        private readonly CancellationTokenSource _cancellationToken;
        public ServiceBusQueueFetchedJob(BrokeredMessage message)
        {
            if (message == null) throw new ArgumentNullException("message");

            _message = message;

            JobId = _message.GetBody<string>();

            _cancellationToken = new CancellationTokenSource();
        }

        public void CheckForValidLock()
        {
            Task.Factory.StartNew(() =>
            {
                while (!_cancellationToken.Token.IsCancellationRequested)
                {
                    if (DateTime.UtcNow > _message.LockedUntilUtc.AddSeconds(-10))
                    {
                        _message.RenewLock();
                    }

                    Task.Delay(500);
                }
            }, _cancellationToken.Token);
        }

        public string JobId { get; private set; }

        public void Requeue()
        {
            try
            {
                _message.Abandon();
            }
            finally
            {
                _completed = true;
                _cancellationToken.Cancel();
            }
            
        }

        public void RemoveFromQueue()
        {
            try
            {
                _message.Complete();
            }
            catch (MessageLockLostException) { }
            catch (Exception)
            {
                _message.Abandon();
                throw;
            }
            finally
            {
                _completed = true;
                _cancellationToken.Cancel();
            }
        }

        public void Dispose()
        {
            try
            {
                if (!_completed && !_disposed)
                {
                    _message.Abandon();
                }
            }
            finally
            {
                _message.Dispose();
                _disposed = true;
                _cancellationToken.Cancel();
            }

        }
    }
}
