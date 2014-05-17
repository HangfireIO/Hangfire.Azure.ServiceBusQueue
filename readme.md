HangFire.Azure.ServiceBusQueue
============================

<a href="https://ci.appveyor.com/project/odinserj/hangfire-azure-servicebusqueue"><img title="Build status" width="113" src="https://ci.appveyor.com/api/projects/status/7so5rr4a2qy5abw6?retina=true" /></a>

This library adds support for Service Bus Queues to be used together
with HangFire.SqlServer job storage implementation. All job data is saved
to SQL Azure database, but job queues use Service Bus Queue.

**Note:** This is pre-alpha version, that has not been ever tested. Please, use it with care :)

Installation
-------------

This library is available as a NuGet Package, so type the following
command into your Package Manager Console window:

```
PM> Install-Package HangFire.Azure.ServiceBusQueue
```

Usage
------

```csharp

string connectionString = 
    CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString");

// You can configure queues through this action
Action<QueueDescription> configureAction = qd =>
{
    qd.MaxSizeInMegabytes = 5120;
    qd.DefaultMessageTimeToLive = new TimeSpan(0, 1, 0);
};

JobStorage.Current = new SqlServerStorage("<connection string>")
    // Use the "default" queue with default options...
    .UseServiceBusQueues(connectionString) // "default" queue and default options
    // ... or specify either queues...
    .UseServiceBusQueues(connectionString, "critical", "default")
    // ... or queues with configuration action
    .UseServiceBusQueues(connectionString, configureAction, "critical", "default");
```

License
--------

HangFire.Azure.ServiceBusQueues is released under the [MIT License](http://www.opensource.org/licenses/MIT).
