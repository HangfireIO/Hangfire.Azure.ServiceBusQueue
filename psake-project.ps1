Include "packages\Hangfire.Build.0.5.0\tools\psake-common.ps1"

Task Default -Depends Collect

Task Collect -Depends Compile -Description "Copy all artifacts to the build folder." {
    Collect-Assembly "Hangfire.Azure.ServiceBusQueue" "net461"
    Collect-Assembly "Hangfire.Azure.ServiceBusQueue" "netstandard2.0"
    Collect-File "license.txt"
    Collect-File "readme.md"
    Collect-File "icon.png"
}

Task Pack -Depends Collect -Description "Create NuGet packages and archive files." {
    $version = Get-PackageVersion
    
    Create-Package "Hangfire.Azure.ServiceBusQueue" $version
    Create-Archive "Hangfire.Azure.ServiceBusQueue-$version"
}

Task Sign -Depends Pack -Description "Sign artifacts." {
    $version = Get-PackageVersion

    Sign-ArchiveContents "Hangfire.Azure.ServiceBusQueue-$version" "hangfire"
}