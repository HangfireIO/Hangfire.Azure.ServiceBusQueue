using System;

namespace Hangfire.Azure.ServiceBusQueue
{
    public interface IRetryPolicy
    {
        void Execute(Action action);
    }
}