using System;
using System.Threading;

namespace Hangfire.Azure.ServiceBusQueue
{
    public class LinearRetryPolicy : IRetryPolicy
    {
        public LinearRetryPolicy(int retryCount, TimeSpan retryDelay)
        {
            this.RetryDelay = retryDelay;
            this.RetryCount = retryCount;
        }

        public TimeSpan RetryDelay { get; private set; }

        public int RetryCount { get; private set; }

        public void Execute(Action action)
        {
            for (var i = 0; i < this.RetryCount; i++)
            {
                try
                {
                    action();

                    return;
                }
                catch (TimeoutException)
                {
                    if (i == this.RetryCount - 1)
                    {
                        throw;
                    }

                    Thread.Sleep(this.RetryDelay);
                }
            }
        }
    }
}