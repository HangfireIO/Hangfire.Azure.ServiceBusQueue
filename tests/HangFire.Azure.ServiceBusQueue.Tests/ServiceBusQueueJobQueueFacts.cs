using System;
using System.Linq;
using System.Threading;
using Hangfire.Azure.ServiceBusQueue;
using Hangfire.SqlServer;
using Microsoft.ServiceBus;
using NUnit.Framework;

namespace HangFire.Azure.ServiceBusQueue.Tests
{
    public class ServiceBusQueueJobQueueFacts
    {
        private ServiceBusQueueOptions options;
        private ServiceBusQueueJobQueueProvider provider;

        private IPersistentJobQueue queue;
        private IPersistentJobQueueMonitoringApi monitor;

        [SetUp]
        public void SetUpQueue()
        {
            SetUpQueue(TimeSpan.FromSeconds(5));
        }

        [TearDown]
        public void DeleteQueues()
        {
            var manager = NamespaceManager.CreateFromConnectionString(options.ConnectionString);

            foreach (var queueName in options.Queues)
            {
                manager.DeleteQueue(options.QueuePrefix + queueName);
            }
        }

        [Test]
        public void Dequeue_ThrowsOperationCanceled_WhenCancellationTokenIsSetAtTheBeginning()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act / Assert
            Assert.Throws<OperationCanceledException>(
                () => queue.Dequeue(options.Queues, cts.Token));
        }

        [Test]
        public void Dequeue_ShouldWaitIndefinitely_WhenThereAreNoJobs()
        {
            // Arrange
            var cts = new CancellationTokenSource(200);

            // Act / Assert
            Assert.Throws<OperationCanceledException>(
                () => queue.Dequeue(options.Queues, cts.Token));
        }

        [Test]
        public void Dequeue_ShouldFetchJobs_OnlyFromSpecifiedQueues()
        {
            // Arrange
            queue.Enqueue(null, options.Queues[0], "1");

            // Act / Assert
            Assert.Throws<OperationCanceledException>(
                () => queue.Dequeue(
                    options.Queues.Skip(1).ToArray(), // Exclude Queues[0]
                    CreateTimingOutCancellationToken()));
        }

        [Test]
        public void Dequeue_ShouldFetchJobs_FromMultipleQueues()
        {
            // Arrange
            queue.Enqueue(null, options.Queues[0], "1");
            queue.Enqueue(null, options.Queues[1], "2");

            // Act / Assert
            var job1 = queue.Dequeue(
                    options.Queues,
                    CreateTimingOutCancellationToken());

            var job2 = queue.Dequeue(
                    options.Queues,
                    CreateTimingOutCancellationToken());

            Assert.That(job1.JobId, Is.EqualTo("1"));
            Assert.That(job2.JobId, Is.EqualTo("2"));
        }

        [Test]
        public void Dequeue_ShouldFetchAJob_FromTheSpecifiedQueue()
        {
            // Arrange
            queue.Enqueue(null, options.Queues[0], "1");

            // Act / Assert
            var job = queue.Dequeue(options.Queues, CreateTimingOutCancellationToken());

            Assert.That(job.JobId, Is.EqualTo("1"));
        }

        [Test]
        public void Dequeue_Complete_Should_RemoveFromQueue()
        {
            // Arrange
            queue.Enqueue(null, options.Queues[0], "1");

            // Act
            var job = queue.Dequeue(options.Queues, CreateTimingOutCancellationToken());
            job.RemoveFromQueue();

            // Assert
            var counts = monitor.GetEnqueuedAndFetchedCount(options.Queues[0]);

            Assert.That(counts.EnqueuedCount, Is.EqualTo(0));
        }

        [Test]
        public void Dequeue_Requeue_ShouldAddBackToQueue()
        {
            // Arrange
            queue.Enqueue(null, options.Queues[0], "1");

            // Act
            var job = queue.Dequeue(options.Queues, CreateTimingOutCancellationToken());
            job.Requeue();

            // Assert
            var counts = monitor.GetEnqueuedAndFetchedCount(options.Queues[0]);

            Assert.That(counts.EnqueuedCount, Is.EqualTo(1));
        }

        [Test]
        [TestCase(5000, 10000)]
        [TestCase(5000, 1000)]
        [TestCase(5000, 2100)]
        [TestCase(500, 499)]
        [TestCase(50, 111)]
        // Ensure we keep the message alive during a long processing period.
        public void Dequeue_Complete_AfterLockTime_ShouldRemoveFromQueue(int lockTimeMs, int processingTimeMs)
        {
            // Arrange
            SetUpQueue(TimeSpan.FromMilliseconds(lockTimeMs));

            queue.Enqueue(null, options.Queues[0], "1");

            // Act
            var job = queue.Dequeue(options.Queues, CreateTimingOutCancellationToken());

            Thread.Sleep(TimeSpan.FromMilliseconds(processingTimeMs));
            job.RemoveFromQueue();

            // Assert
            var counts = monitor.GetEnqueuedAndFetchedCount(options.Queues[0]);

            Assert.That(counts.EnqueuedCount, Is.EqualTo(0));
        }

        private static CancellationToken CreateTimingOutCancellationToken()
        {
            var source = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            return source.Token;
        }

        private void SetUpQueue(TimeSpan lockDuration)
        {
            options = new TestServiceBusQueueOptions
            {
                Configure = d => d.LockDuration = lockDuration
            };

            provider = new ServiceBusQueueJobQueueProvider(options);

            queue = provider.GetJobQueue();
            monitor = provider.GetJobQueueMonitoringApi();
        }
    }
}