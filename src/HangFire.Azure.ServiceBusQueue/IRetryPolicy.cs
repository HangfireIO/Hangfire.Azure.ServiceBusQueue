using System;
using System.Threading.Tasks;

namespace Hangfire.Azure.ServiceBusQueue
{
    public interface IRetryPolicy
    {
        Task Execute(Func<Task> action);
    }
}