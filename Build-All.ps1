<#
.SYNOPSIS
    Script to build all of the code in this repo

.PARAMETER Configuration
    This sets the build configuration to use, default is "Release" though for inner loop development this may be set to "Debug"

.PARAMETER ForceClean
    Forces a complete clean (Recursive delete of the build output)

.DESCRIPTION
    This script is used by the automated build to perform the actual build. The Ubiquity.NET
    family of projects all employ a PowerShell driven build that is generally divorced from the
    automated build infrastructure used. This is done for several reasons, but the most
    important ones are the ability to reproduce the build locally for inner development and
    for flexibility in selecting the actual back end. The back ends have changed a few times
    over the years and re-writing the entire build in terms of those back ends each time is
    a lot of wasted effort. Thus, the projects settled on PowerShell as the core automated
    build tooling.
#>
[cmdletbinding()]
Param(
    [string]$Configuration="Release",
    [switch]$ForceClean
)

pushd $PSScriptRoot
$oldPath = $env:Path
try
{
    # Pull in the repo specific support and force a full initialization of all the environment
    # as this is a top level build command.
    . .\repo-buildutils.ps1
    $buildInfo = Initialize-BuildEnvironment -FullInit
    if((Test-Path -PathType Container $buildInfo['BuildOutputPath']) -and $ForceClean )
    {
        Write-Information "Cleaning output folder from previous builds"
        rd -Recurse -Force -Path $buildInfo['BuildOutputPath']
    }

    md $buildInfo['NuGetOutputPath'] -ErrorAction SilentlyContinue | Out-Null

    .\Build-Source.ps1
}
catch
{
    # everything from the official docs to the various articles in the blog-sphere says this isn't needed
    # and in fact it is redundant - They're all WRONG! By re-throwing the exception the original location
    # information is retained and the error reported will include the correct source file and line number
    # data for the error. Without this, only the error message is retained and the location information is
    # Line 1, Column 1, of the outer most script file, which is, of course, completely useless.
    throw
}
finally
{
    popd
    $env:Path = $oldPath
}

Write-Information "Done build"
