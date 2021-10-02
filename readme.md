Hangfire.Azure.ServiceBusQueue
============================

[![Official Site](https://img.shields.io/badge/site-hangfire.io-blue.svg)](http://hangfire.io) [![Latest version](https://img.shields.io/badge/nuget-latest-blue.svg)](https://www.nuget.org/packages/HangFire.Azure.ServiceBusQueue/) [![Build status](https://ci.appveyor.com/api/projects/status/3l7dued0cvkjascj?svg=true)](https://ci.appveyor.com/project/odinserj/hangfire-azure-servicebusqueue) [![License MIT](https://img.shields.io/badge/license-MIT-green.svg)](http://opensource.org/licenses/MIT)

What is it?
-----------

Adds support for using Azure Service Bus Queues with [Hangfire](http://hangfire.io)'s SQL storage provider to reduce latency and remove the need to poll the database for new jobs.

All job data continues to be stored and maintained within SQL storage, but polling is removed in favour of pushing the job ids through the service bus.

Installation
-------------

Hangfire.Azure.ServiceBusQueue is available as a NuGet package. Install it using the NuGet Package Console window:

```
PM> Install-Package Hangfire.Azure.ServiceBusQueue
```

Compatibility
-------------

Hangfire v1.7+ introduced breaking changes to the SQL Server integration points and requires at least version 4.0.0 of this library. If you are on an older version of Hangfire please use a lower version of Hangfire.Azure.ServiceBusQueue

Usage
------

To use the queue it needs to be added to your existing SQL Server storage configuration, using one of the `UseServiceBusQueues` overloads:

```csharp
var sqlStorage = new SqlServerStorage("<connection string>");

// The connection string *must* be for the root namespace and have the "Manage"
// permission if used by the dashboard
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
        //
        // This can be useful in development environments particularly where the machine
        // name could be used to separate individual developers machines automatically
        // (i.e. "my-prefix-{machine-name}".Replace("{machine-name}", Environment.MachineName))
        QueuePrefix = "my-prefix-",
        
        // The queues to monitor. This *must* be specified, even to set just
        // the default queue as done here
        Queues = new [] { EnqueuedState.DefaultQueue },
        
        // By default queues will be checked and created on startup. This option
        // can be disabled if the application will only be sending / listening to 
        // the queue and you want to remove the 'Manage' permission from the shared
        // access policy.
        //
        // Note that the dashboard *must* have the 'Manage' permission otherwise the
        // queue length cannot be read
        CheckAndCreateQueues = false,
        
        // Typically a lower value is desired to keep the throughput of message processing high. A lower timeout means more calls to
        // Azure Service Bus which can increase costs, especially on an under-utilised server with few jobs.
        // Use a Higher value for lower costs in non production or non critical jobs
        LoopReceiveTimeout = TimeSpan.FromMilliseconds(500)
        
        // Delay between queue polling requests
        QueuePollInterval = TimeSpan.Zero
    });

GlobalConfiguration.Configuration
    .UseStorage(sqlStorage);
```
For .NETCore and beyond, you can also use the `IGlobalConfiguration extensions`:

```csharp

// Uses default options (no prefix or configuration) with the "default" queue only
services.AddHangfire(configuration => configuration
    .UseSqlServerStorage("<sql connection string>")
    .UseServiceBusQueues("<azure servicebus connection string>")
    
// Uses default options (no prefix or configuration) with the "critical" and "default" queues
services.AddHangfire(configuration => configuration
    .UseSqlServerStorage("<sql connection string>")
    .UseServiceBusQueues("<azure servicebus connection string>", "critical", "default")
    
// Configures queues on creation and uses the "crtical" and "default" queues
services.AddHangfire(configuration => configuration
    .UseSqlServerStorage("<sql connection string>")
    .UseServiceBusQueues("<azure servicebus connection string>", 
        queueOptions => {
            queueOptions.MaxSizeInMegabytes = 5120;
            queueOptions.DefaultMessageTimeToLive = new TimeSpan(0, 1, 0);
        } "critical", "default")
    
// Specifies all options
services.AddHangfire(configuration => configuration
    .UseSqlServerStorage("<sql connection string>")
    .UseServiceBusQueues(new ServiceBusQueueOptions
    {
        ConnectionString = connectionString,
                
        Configure = configureAction,
        
        // The actual queues used in Azure will have this prefix if specified
        // (e.g. the "default" queue will be created as "my-prefix-default")
        //
        // This can be useful in development environments particularly where the machine
        // name could be used to separate individual developers machines automatically
        // (i.e. "my-prefix-{machine-name}".Replace("{machine-name}", Environment.MachineName))
        QueuePrefix = "my-prefix-",
        
        // The queues to monitor. This *must* be specified, even to set just
        // the default queue as done here
        Queues = new [] { EnqueuedState.DefaultQueue },
        
        // By default queues will be checked and created on startup. This option
        // can be disabled if the application will only be sending / listening to 
        // the queue and you want to remove the 'Manage' permission from the shared
        // access policy.
        //
        // Note that the dashboard *must* have the 'Manage' permission otherwise the
        // queue length cannot be read
        CheckAndCreateQueues = false,
        
        // Typically a lower value is desired to keep the throughput of message processing high. A lower timeout means more calls to
        // Azure Service Bus which can increase costs, especially on an under-utilised server with few jobs.
        // Use a Higher value for lower costs in non production or non critical jobs
        LoopReceiveTimeout = TimeSpan.FromMilliseconds(500)
        
        // Delay between queue polling requests
        QueuePollInterval = TimeSpan.Zero
    }));
```

Questions? Problems?
---------------------

Open-source project are developing more smoothly, when all discussions are held in public.

If you have any questions or problems related to Hangfire itself or this queue implementation or want to discuss new features, please visit the [discussion forum](http://discuss.hangfire.io). You can sign in there using your existing Google or GitHub account, so it's very simple to start using it.

If you've discovered a bug, please report it to the [Hangfire GitHub Issues](https://github.com/HangfireIO/Hangfire/issues?state=open). Detailed reports with stack traces, actual and expected behavours are welcome. 

License
--------

Hangfire.Azure.ServiceBusQueues is released under the [MIT License](http://www.opensource.org/licenses/MIT).
