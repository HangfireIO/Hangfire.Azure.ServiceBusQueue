Hangfire.Azure.ServiceBusQueue
============================

[![Official Site](https://img.shields.io/badge/site-hangfire.io-blue.svg)](http://hangfire.io) [![Latest version](https://img.shields.io/badge/nuget-latest-blue.svg)](https://www.nuget.org/packages/HangFire.Azure.ServiceBusQueue/) [![Build status](https://ci.appveyor.com/api/projects/status/3l7dued0cvkjascj?svg=true)](https://ci.appveyor.com/project/odinserj/hangfire-azure-servicebusqueue) [![License MIT](https://img.shields.io/badge/license-MIT-green.svg)](http://opensource.org/licenses/MIT)

What is it?
-----------

Adds support for using Azure Service Bus Queues with [Hangfire](http://hangfire.io)'s SQL storage provider to reduce latency and remove the need to poll the database for new jobs.

All job data continues to be stored and maintained within SQL storage, but polling is removed in favur of pushing the job ids through the service bus.

Installation
-------------

Hangfire.Azure.ServiceBusQueue is available as a NuGet package. So, you can install it using the NuGet Package Console window:

```
PM> Install-Package Hangfire.Azure.ServiceBusQueue
```

Usage
------

To use the queue it needs to be added to your existing SQL Server storage configuration, using one of the `UseServiceBusQueues` overloads:

```csharp
var sqlStorage = new SqlServerStorage("<connection string>");

// The connection string *must* be for the root namespace and have the "Manage"
// permission as queues will be created on startup if they do not exist
var connectionString = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString");

// You can configure queues on first creation using this action
Action<QueueDescription> configureAction = qd =>
{
    qd.MaxSizeInMegabytes = 5120;
    qd.DefaultMessageTimeToLive = new TimeSpan(0, 1, 0);
};

// Uses default options (no prefix or configuration) with the "default" queue only
sqlStorage.UseServiceBusQueues(connectionString);

// Uses default options (no prefix or configuration) with the "critical" and "default" queues
sqlStorage.UseServiceBusQueues(connectionString, "critical", "default"); 

// Configures queues on creation and uses the "crtical" and "default" queues
sqlStorage.UseServiceBusQueues(connectionString, configureAction, "critical", "default"); 
    
// Specifies all options
sqlStorage.UseServiceBusQueues(new ServiceBusQueueOptions
    {
        ConnectionString = connectionString,
        
        Configure = configureAction,
        
        // The actual queues used in Azure will have this prefix if specified
        // (e.g. the "default" queue will be created as "my-prefix-default")
        QueuePrefix = "my-prefix-",
        
        // The queues to monitor. This *must* be specified, even to set just
        // the default queue as done here
        Queues = new [] { EnqueuedState.DefaultQueue }
    });

GlobalConfiguration.Configuration
    .UseStorage(sqlStorage);
```

Questions? Problems?
---------------------

Open-source project are developing more smoothly, when all discussions are held in public.

If you have any questions or problems related to Hangfire itself or this queue implementation or want to discuss new features, please visit the [discussion forum](http://discuss.hangfire.io). You can sign in there using your existing Google or GitHub account, so it's very simple to start using it.

If you've discovered a bug, please report it to the [Hangfire GitHub Issues](https://github.com/HangfireIO/Hangfire/issues?state=open). Detailed reports with stack traces, actual and expected behavours are welcome. 

License
--------

Hangfire.Azure.ServiceBusQueues is released under the [MIT License](http://www.opensource.org/licenses/MIT).
