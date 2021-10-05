Properties {
    $solution = "HangFire.Azure.ServiceBusQueue.sln"
}

Include "packages\Hangfire.Build.0.2.6\tools\psake-common.ps1"

Task Default -Depends Collect

Task CompileCore -Depends Clean {
    Exec { dotnet build -c Release }
}

Task Collect -Depends CompileCore -Description "Copy all artifacts to the build folder." {
    Collect-Assembly "Hangfire.Azure.ServiceBusQueue" "net461"
    Collect-Assembly "Hangfire.Azure.ServiceBusQueue" "netstandard2.0"
}

Task Pack -Depends Collect -Description "Create NuGet packages and archive files." {
    $version = Get-PackageVersion
    
    Create-Archive "Hangfire.Azure.ServiceBusQueue-$version"
    Create-Package "Hangfire.Azure.ServiceBusQueue" $version
}
