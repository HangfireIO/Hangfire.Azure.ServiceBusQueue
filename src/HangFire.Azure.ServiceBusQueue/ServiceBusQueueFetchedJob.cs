using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Logging;
using Hangfire.Storage;
using Microsoft.ServiceBus.Messaging;

namespace Hangfire.Azure.ServiceBusQueue
{
    internal class ServiceBusQueueFetchedJob : IFetchedJob
    {
        private readonly ILog _logger = LogProvider.GetLogger(typeof(ServiceBusQueueFetchedJob));
        private readonly BrokeredMessage _message;
        private readonly TimeSpan? _lockRenewalDelay;
        private readonly CancellationTokenSource _cancellationTokenSource;

        private bool _completed;
        private bool _disposed;

        public ServiceBusQueueFetchedJob(BrokeredMessage message, TimeSpan? lockRenewalDelay)
        {
            if (message == null) throw new ArgumentNullException("message");

            _message = message;
            _lockRenewalDelay = lockRenewalDelay;
            _cancellationTokenSource = new CancellationTokenSource();

            JobId = _message.GetBody<string>();

            KeepAlive();
        }

        public string JobId { get; private set; }

        public BrokeredMessage Message { get { return this._message; } }

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
                    // Previously we were waiting until a second before the lock is due
                    // to expire to give us plenty of time to wake up and communicate
                    // with queue to renew the lock of this message before expiration.
                    // However since clocks may be non-synchronized well, for long-running
                    // background jobs it's better to have more renewal attempts than a
                    // lock that's expired too early.
                    var toWait = _lockRenewalDelay.HasValue
                        ? _lockRenewalDelay.Value
                        : _message.LockedUntilUtc - DateTime.UtcNow - TimeSpan.FromSeconds(1);
                    
                    await Task.Delay(toWait, _cancellationTokenSource.Token);

                    // Double check we have not been cancelled to avoid renewing a lock
                    // unnecessarily
                    if (!_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        try
                        {
                            _message.RenewLock();
                        }
                        catch (Exception ex)
                        {
                            _logger.DebugException(
                                $"An exception was thrown while trying to renew a lock for job '{JobId}'.",
                                ex);
                        }
                    }
                }
            }, _cancellationTokenSource.Token);
        }
    }
}
