using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Hangfire.Azure.ServiceBusQueue;
using Hangfire.SqlServer;
using NUnit.Framework;

namespace HangFire.Azure.ServiceBusQueue.Tests
{
    public class ServiceBusQueueMonitoringApiFacts
    {
        private ServiceBusQueueOptions options;
        private ServiceBusQueueJobQueueProvider provider;

        private IPersistentJobQueue queue;
        private IPersistentJobQueueMonitoringApi monitor;

        [SetUp]
        public void SetUpQueue()
        {
            options  = new TestServiceBusQueueOptions();
            provider = new ServiceBusQueueJobQueueProvider(options);
            queue    = provider.GetJobQueue();
            monitor  = provider.GetJobQueueMonitoringApi();
        }

        [TearDown]
        public async Task DeleteQueues()
        {
            var manager = options.Credential != null ? new ServiceBusAdministrationClient(options.ConnectionString, options.Credential) : new ServiceBusAdministrationClient(options.ConnectionString);

            foreach (var currentQueue in options.Queues)
            {
                await manager.DeleteQueueAsync(options.QueuePrefix + currentQueue);
            }
        }

        [Test]
        public void GetEnqueuedAndFetchedCount_WhenNoJobs_ShouldCountFromQueue()
        {
            // Arrange
            // Act
            var counts = monitor.GetEnqueuedAndFetchedCount(options.Queues[0]);

            // Assert
            Assert.That(counts.EnqueuedCount, Is.EqualTo(0));
            Assert.That(counts.FetchedCount, Is.Null);
        }

        [Test]
        public void GetEnqueuedAndFetchedCount_WhenJobs_ShouldCountFromQueue()
        {
            // Arrange
            queue.Enqueue(null, null, options.Queues[0], "1234");

            // Act
            var counts = monitor.GetEnqueuedAndFetchedCount(options.Queues[0]);

            // Assert
            Assert.That(counts.EnqueuedCount, Is.EqualTo(1));
            Assert.That(counts.FetchedCount, Is.Null);
        }

        [Test]
        public async Task GetEnqueuedAndFetchedCount_WhenDeadlettered_ShouldIgnore()
        {
            // Arrange
            queue.Enqueue(null, null, options.Queues[0], "1234");
            var job = (ServiceBusQueueFetchedJob)queue.Dequeue(options.Queues, default(CancellationToken));


            var fieldInfo = job.GetType().GetTypeInfo().GetField("_client", BindingFlags.NonPublic | BindingFlags.Instance);
            var client    = fieldInfo?.GetValue(job) as ServiceBusReceiver;

            await client.DeadLetterMessageAsync(job.Message);

            // Act
            var counts = monitor.GetEnqueuedAndFetchedCount(options.Queues[0]);

            // Assert
            Assert.That(counts.EnqueuedCount, Is.EqualTo(0));
            Assert.That(counts.FetchedCount, Is.Null);
        }

        [Test]
        public void GetEnqueuedJobIds_WhenJobs_ShouldWorkPage1()
        {
            // Arrange
            queue.Enqueue(null, null, options.Queues[0], "1");
            queue.Enqueue(null, null, options.Queues[0], "2");
            queue.Enqueue(null, null, options.Queues[0], "3");
            queue.Enqueue(null, null, options.Queues[0], "4");

            // Act
            var counts = monitor.GetEnqueuedJobIds(options.Queues[0], 0, 5);

            // Assert/
            Assert.That(counts, Is.EquivalentTo(new long[] { 1, 2, 3, 4 }));
        }

        [Test]
        public void GetEnqueuedJobIds_WhenJobs_ShouldWorkPage2()
        {
            // Arrange
            queue.Enqueue(null, null, options.Queues[0], "1");
            queue.Enqueue(null, null, options.Queues[0], "2");
            queue.Enqueue(null, null, options.Queues[0], "3");
            queue.Enqueue(null, null, options.Queues[0], "4");

            // Act
            var counts1 = monitor.GetEnqueuedJobIds(options.Queues[0], 0, 2);
            var counts2 = monitor.GetEnqueuedJobIds(options.Queues[0], 2, 2);

            // Assert/
            Assert.That(counts1, Is.EquivalentTo(new long[] { 1, 2 }));
            Assert.That(counts2, Is.EquivalentTo(new long[] { 3, 4 }));
        }

        [Test]
        public void GetEnqueuedJobIds_WhenJobs_ShouldWorkWithLargeValues()
        {
            // Arrange
            for (var i = 0; i < 100; i++)
            {
                queue.Enqueue(null, null, options.Queues[0], i.ToString());
            }

            // Act
            var counts = monitor.GetEnqueuedJobIds(options.Queues[0], 58, 2);

            // Assert/
            Assert.That(counts, Is.EquivalentTo(new long[] { 58, 59 }));
        }
    }
}
