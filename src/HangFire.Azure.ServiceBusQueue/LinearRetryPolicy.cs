using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hangfire.Azure.ServiceBusQueue
{
    public class LinearRetryPolicy : IRetryPolicy
    {
        private readonly List<TimeSpan> _retryDelays = new List<TimeSpan>();

        public LinearRetryPolicy(int retryCount, TimeSpan retryDelay)
        {
            if (retryCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(retryCount));

            if (retryDelay <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(retryCount));

            for (var i = 0; i < retryCount; i++)
            {
                _retryDelays.Add(retryDelay);
            }
        }

        public LinearRetryPolicy(IEnumerable<TimeSpan> retryDelay)
        {
            if (retryDelay == null)
                throw new ArgumentNullException(nameof(retryDelay));

            var timeSpans = retryDelay.ToList();
            if (!timeSpans.Any() || timeSpans.Any(x=>x <= TimeSpan.Zero))
                throw new ArgumentOutOfRangeException(nameof(retryDelay));

            _retryDelays = timeSpans.ToList();
        }

        public async Task Execute(Func<Task> action)
        {
            var exceptions = new List<Exception>();

            var attempt = 0;
            foreach (var retryDelay in _retryDelays)
            {
                attempt++;
                try
                {
                    if (attempt > 1)
                        await Task.Delay(retryDelay).ConfigureAwait(false);

                    await action().ConfigureAwait(false);

                    return;
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
            throw new AggregateException(exceptions);
        }
    }
}
