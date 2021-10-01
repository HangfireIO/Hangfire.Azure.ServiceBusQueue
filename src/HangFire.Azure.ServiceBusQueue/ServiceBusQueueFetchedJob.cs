using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Hangfire.Logging;
using Hangfire.Storage;

namespace Hangfire.Azure.ServiceBusQueue
{
    internal class ServiceBusQueueFetchedJob : IFetchedJob
    {
        private readonly ILog _logger = LogProvider.GetCurrentClassLogger();
        private readonly ServiceBusReceiver _client;
        private readonly TimeSpan? _lockRenewalDelay;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ServiceBusReceivedMessage _message;

        private bool _completed;
        private bool _disposed;

        public ServiceBusQueueFetchedJob(ServiceBusReceiver client, ServiceBusReceivedMessage message, TimeSpan? lockRenewalDelay)
        {
            _message = message ?? throw new ArgumentNullException(nameof(message));
            _client = client;
            _lockRenewalDelay = lockRenewalDelay;
            _cancellationTokenSource = new CancellationTokenSource();

            JobId    = message.Body.ToString();

            KeepAlive();
        }

        public string JobId { get; }
        public ServiceBusReceivedMessage Message => _message;

        public void Requeue()
        {
            _cancellationTokenSource.Cancel();

            try
            {
                AsyncHelper.RunSync(() => _client.AbandonMessageAsync(_message));
                _completed = true;
            }
            catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessageNotFound)
            {
                _logger.Warn($"Message with token '{_message.LockToken}' not found in service bus.");
            }
            catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessageLockLost)
            {
            }
        }

        public void RemoveFromQueue()
        {
            _cancellationTokenSource.Cancel();

            try
            {
                AsyncHelper.RunSync(() => _client.CompleteMessageAsync(_message));
                _completed = true;
            }
            catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessageNotFound)
            {
                _logger.Warn($"Message with token '{_message.LockToken}' not found in service bus.");
            }
            catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessageLockLost)
            {
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();

            try
            {
                if (!_completed && !_disposed)
                {
                    AsyncHelper.RunSync(() => _client.AbandonMessageAsync(_message));
                }
            }
            catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessageNotFound
                                                 || ex.Reason == ServiceBusFailureReason.MessageLockLost)
            {
            }

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
                    var toWait = _lockRenewalDelay ??
                                 _message.LockedUntil - DateTime.UtcNow - TimeSpan.FromSeconds(1);

                    await Task.Delay(toWait, _cancellationTokenSource.Token).ConfigureAwait(false);

                    // Double check we have not been cancelled to avoid renewing a lock
                    // unnecessarily
                    if (_cancellationTokenSource.Token.IsCancellationRequested) continue;

                    try
                    {
                        await _client.RenewMessageLockAsync(_message).ConfigureAwait(false);
                    }
                    catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessageNotFound
                                                         || ex.Reason == ServiceBusFailureReason.MessageLockLost)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.DebugException(
                            $"An exception was thrown while trying to renew a lock for job '{JobId}'.", ex);
                    }
                }
            }, _cancellationTokenSource.Token);
        }
    }
}
