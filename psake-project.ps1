Properties {
    $solution = "HangFire.Azure.ServiceBusQueue.sln"
}

Include "packages\Hangfire.Build.0.1.3\tools\psake-common.ps1"

Task Default -Depends Collect

Task Collect -Depends Compile -Description "Copy all artifacts to the build folder." {
    Collect-Assembly "Hangfire.Azure.ServiceBusQueue" "Net45"
}

Task Pack -Depends Collect -Description "Create NuGet packages and archive files." {
    $version = Get-BuildVersion

    $tag = $env:APPVEYOR_REPO_TAG_NAME
    if ($tag -And $tag.StartsWith("v$version-")) {
        "Using tag-based version for packages."
        $version = $tag.Substring(1)
    }
    
    Create-Archive "Hangfire.Azure.ServiceBusQueue-$version"
    Create-Package "Hangfire.Azure.ServiceBusQueue" $version
}
