class PrereleaseVersion
{
    [ValidateSet("", "alpha", "beta", "delta", "epsilon", "gamma", "kappa", "prerelease", "rc")]
    [string] $Name;

    [ValidateSet("", "a", "b", "d", "e", "g", "k", "p", "r")]
    [string] $ShortName;

    [ValidateRange(-1,8)]
    [int] $Index;

    [ValidateRange(0,99)]
    [int] $Number;

    [ValidateRange(0,99)]
    [int] $Fix;

    [string] $CiBuildName;

    [string] $CiBuildIndex;

    PrereleaseVersion([hashtable]$buildInfo)
    {
        $preRelName = $buildInfo['PreReleaseName']
        $preRelNumber = $buildInfo['PreReleaseNumber']
        $preRelFix = $buildInfo['PreReleaseFix']
        $this.CiBuildName = $buildInfo["CiBuildName"];
        $this.CiBuildIndex = $buildInfo["CiBuildIndex"];

        if( (![string]::IsNullOrEmpty($this.CiBuildName)) -and !($this.CiBuildName -match '\A[a-z0-9-]+\Z') )
        {
            throw "CiBuildName is invalid"
        }

        if( (![string]::IsNullOrEmpty($this.CiBuildIndex)) -and !($this.CiBuildIndex -match '\A[a-z0-9-]+\Z') )
        {
            throw "CiBuildIndex is invalid"
        }

        if( ![string]::IsNullOrWhiteSpace( $preRelName ) )
        {
            $this.Index = [PrereleaseVersion]::GetPrerelIndex($preRelName)
            $this.Name = ($this.Index -ge 0) ? [PrereleaseVersion]::PreReleaseNames[$this.Index] : ""
            $this.ShortName = ($this.Index -ge 0) ? [PrereleaseVersion]::PreReleaseShortNames[$this.Index] : ""
            $this.Number = $preRelNumber;
            $this.Fix = $preRelFix;
        }
        else
        {
            $this.Index = -1;
        }

        if( (![string]::IsNullOrEmpty( $this.CiBuildName )) -and [string]::IsNullOrEmpty( $this.CiBuildIndex ) )
        {
            throw "CiBuildIndex is required if CiBuildName is provided";
        }
    }

    [string] ToString([bool] $useShortForm = $false)
    {
        $hasCIBuild = ![string]::IsNullOrEmpty($this.CiBuildName)
        $hasPreRel = $this.Index -ge 0

        $bldr = [System.Text.StringBuilder]::new()
        if($hasPreRel)
        {
            $bldr.Append('-').Append($useShortForm ? $this.ShortName : $this.Name)
            $delimFormat = $useShortForm ? '-{0:D02}' : '.{0}'
            if(($this.Number -gt 0))
            {
                $bldr.AppendFormat($delimFormat, $this.Number)
                if(($this.Fix -gt 0))
                {
                    $bldr.AppendFormat($delimFormat, $this.Fix)
                }
            }
        }

        if($hasCIBuild)
        {
            $bldr.Append($hasPreRel ? '.' : '--')
            $bldr.AppendFormat('ci.{0}.{1}', $this.CiBuildIndex, $this.CiBuildName)
        }

        return $bldr.ToString()
    }

    hidden static [int] GetPrerelIndex([string] $preRelName)
    {
        $preRelIndex = -1
        if(![string]::IsNullOrWhiteSpace($preRelName))
        {
            $preRelIndex = [PrereleaseVersion]::PreRleaseNames |
                         ForEach-Object {$index=0} {@{Name = $_; Index = $index++}} |
                         Where-Object {$_["Name"] -ieq $preRelName} |
                         ForEach-Object {$_["Index"]} |
                         Select-Object -First 1

            # if not found in long names, test against the short names
            if($preRelIndex -lt 0)
            {
                $preRelIndex = [PrereleaseVersion]::PreReleaseShortNames |
                             ForEach-Object {$index=0} {@{Name = $_; Index = $index++}} |
                             Where-Object {$_["Name"] -ieq $preRelName} |
                             ForEach-Object {$_["Index"]} |
                             Select-Object -First 1
            }
        }
        return $preRelIndex
    }

    hidden static [string[]] $PreReleaseNames = @("alpha", "beta", "delta", "epsilon", "gamma", "kappa", "prerelease", "rc" );
    hidden static [string[]] $PreReleaseShortNames = @("a", "b", "d", "e", "g", "k", "p", "r");
}

class CSemVer
{
    [ValidateRange(0,99999)]
    [int] $Major;

    [ValidateRange(0,49999)]
    [int] $Minor;

    [ValidateRange(0,9999)]
    [int] $Patch;

    [ValidateLength(0,20)]
    [string] $BuildMetadata;

    [ulong] $OrderedVersion;

    [Version] $FileVersion;

    [PrereleaseVersion] $PrereleaseVersion;

    CSemVer([hashtable]$buildInfo)
    {
        $this.Major = $buildInfo["BuildMajor"]
        $this.Minor = $buildInfo["BuildMinor"]
        $this.Patch = $buildInfo["BuildPatch"]
        $this.PrereleaseVersion = [PrereleaseVersion]::new($buildInfo)
        $this.BuildMetadata = $buildInfo["BuildMetadata"]
        $this.OrderedVersion = [CSemVer]::GetOrderedVersion($this.Major, $this.Minor, $this.Patch, $this.PreleaseVersion)
        $this.FileVersion = [CSemVer]::ConvertToVersion($this.OrderedVersion -shl 1)
    }

    [string] ToString([bool] $includeMetadata, [bool]$useShortForm)
    {
        $bldr = [System.Text.StringBuilder]::new()
        $bldr.AppendFormat('{0}.{1}.{2}', $this.Major, $this.Minor, $this.Patch)
        if($this.PrereleaseVersion)
        {
            $bldr.Append($this.PrereleaseVersion.ToString($useShortForm))
        }

        if(![string]::IsNullOrWhitespace($this.BuildMetadata) -and $includeMetadata)
        {
            $bldr.AppendFormat( '+{0}', $this.BuildMetadata )
        }

        return $bldr.ToString();
    }

    [string] ToString()
    {
        return $this.ToString($true, $false);
    }

    hidden static [ulong] GetOrderedVersion($Major, $Minor, $Patch, [PrereleaseVersion] $PreReleaseVersion)
    {
        [ulong] $MulNum = 100;
        [ulong] $MulName = $MulNum * 100;
        [ulong] $MulPatch = ($MulName * 8) + 1;
        [ulong] $MulMinor = $MulPatch * 10000;
        [ulong] $MulMajor = $MulMinor * 50000;

        [ulong] $retVal = (([ulong]$Major) * $MulMajor) + (([ulong]$Minor) * $MulMinor) + ((([ulong]$Patch) + 1) * $MulPatch);
        if( $PrereleaseVersion -and $PrereleaseVersion.Index -gt 0 )
        {
            $retVal -= $MulPatch - 1;
            $retVal += [ulong]($PrereleaseVersion.Index) * $MulName;
            $retVal += [ulong]($PrereleaseVersion.Number) * $MulNum;
            $retVal += [ulong]($PrereleaseVersion.Fix);
        }
        return $retVal;
    }

    hidden static [Version] ConvertToVersion([ulong]$value)
    {
        $revision = [ushort]($value % 65536);
        $rem = [ulong](($value - $revision) / 65536);

        $build = [ushort]($rem % 65536);
        $rem = ($rem - $build) / 65536;

        $minorNum = [ushort]($rem % 65536);
        $rem = ($rem - $minorNum) / 65536;

        $majorNum = [ushort]($rem % 65536);

        return [Version]::new( $majorNum, $minorNum, $build, $revision );
    }
}

function Initialize-BuildEnvironment
{
<#
.SYNOPSIS
    Initializes the build environment for the build scripts

.PARAMETER FullInit
    Performs a full initialization. A full initialization includes forcing a re-capture of the time stamp for local builds
    as well as writes details of the initialization to the information and verbose streams.

.DESCRIPTION
    This script is used to initialize the build environment in a central place, it returns the
    build info Hashtable with properties determined for the build. Script code should use these
    properties instead of any environment variables. While this script does setup some environment
    variables for non-script tools (i.e., MSBuild) script code should not rely on those.

    This script will setup the PATH environment variable to contain the path to MSBuild so it is
    readily available for all subsequent script code.

    Environment variables set for non-script tools:

    | Name               | Description |
    |--------------------|-------------|
    | IsAutomatedBuild   | "true" if in an automated build environment "false" for local developer builds |
    | IsPullRequestBuild | "true" if this is a build from an untrusted pull request (limited build, no publish etc...) |
    | IsReleaseBuild     | "true" if this is an official release build |
    | CiBuildName        | Name of the build for Constrained Semantic Version construction |
    | BuildTime          | ISO-8601 formatted time stamp for the build (local builds are based on current time, automated builds use the time from the HEAD commit)

    The Hashtable returned from this function includes all the values retrieved from
    the common build function Initialize-CommonBuildEnvironment plus additional repository specific
    values. In essence, the result is like a derived type from the common base. The
    additional properties added are:

    | Name                       | Description                                                                                            |
    |----------------------------|--------------------------------------------------------------------------------------------------------|
    | OfficialGitRemoteUrl       | GIT Remote URL for ***this*** repository                                                               |
#>
    # support common parameters
    [cmdletbinding()]
    [OutputType([hashtable])]
    Param(
        $repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..' '..' '..')),
        [switch]$FullInit
    )
    try
    {
        # use common repo-neutral function to perform most of the initialization
        $buildInfo = Initialize-CommonBuildEnvironment $repoRoot -FullInit:$FullInit
        if($IsWindows -and !(Find-OnPath MSBuild))
        {
            Write-Information "Adding MSBUILD to PATH"
            $env:PATH += ";$(vswhere -find MSBuild\Current\Bin\MSBuild.exe | split-path -parent)"
        }

        if(!(Find-OnPath MSBuild))
        {
            throw "MSBuild not found - currently required for LIBLLVM builds"
        }

        # Add repo specific values
        $buildInfo['PackagesRoot'] = Join-Path $buildInfo['BuildOutputPath'] 'packages'
        $buildInfo['OfficialGitRemoteUrl'] = 'https://github.com/UbiquityDotNET/CSemVer.GitBuild.git'

        # make sure directories required (but not created by build tools) exist
        New-Item -ItemType Directory -Path $buildInfo['BuildOutputPath'] -ErrorAction SilentlyContinue | Out-Null
        New-Item -ItemType Directory -Path $buildInfo['PackagesRoot'] -ErrorAction SilentlyContinue | Out-Null
        New-Item -ItemType Directory $buildInfo['NuGetOutputPath'] -ErrorAction SilentlyContinue | Out-Null

        # Disable the default "terminal logger" support as it's a breaking change that should NEVER
        # have been anything but OPT-IN. It's a terrible experience that ends up hiding/overwriting
        # information and generally makes it HARDER to see what's going on, not easier as it claims.
        $env:MSBUILDTERMINALLOGGER='off'

        $verInfo = Get-ParsedBuildVersionXML -BuildInfo $buildInfo
        $verInfo['CiBuildIndex'] = ConvertTo-BuildIndex $env:BuildTime
        $verInfo['CiBuildName'] = $buildInfo['CiBuildName']

        # force env overloads of variables, normally generated by tasks
        # but the tasks cannot be used in this build as it is building the tasks
        $csemVer = [CSemVer]::New($verInfo)
        $env:BuildMajor = $csemVer.Major
        $env:BuildMinor = $csemVer.Minor
        $env:BuildPatch = $csemVer.Patch
        $env:BuildMeta = $csemVer.BuildMetadata
        $env:FullBuildNumber = $csemVer.ToString()
        $env:PackageVersion = $csemVer.ToString($false,$true)

        $fileVer = $csemVer.FileVersion
        $env:FileVersion = $fileVer.ToString()
        $env:FileVersionMajor = $fileVer.Major
        $env:FileVersionMinor = $fileVer.Minor
        $env:FileVersionBuild = $fileVer.Build
        $env:FileVersionRevision = $fileVer.Revision

        if($csemVer.PrereleaseVersion)
        {
            $env:PreReleaseName = $csemVer.PrereleaseVersion.Name
            $env:PreReleaseNumber = $csemVer.PrereleaseVersion.Number
            $env:PreReleaseFix = $csemVer.PrereleaseVersion.Fix
            $env:CiBuildName = $csemVer.PrereleaseVersion.CiBuildName
            $env:CiBuildIndex = $csemVer.PrereleaseVersion.CiBuildIndex
        }

        if($FullInit)
        {
            Write-Information (Show-FullBuildInfo $buildInfo | out-string)
        }

        return $buildInfo
    }
    catch
    {
        # everything from the official docs to the various articles in the blog-sphere says this isn't needed
        # and in fact it is redundant - They're all WRONG! By re-throwing the exception the original location
        # information is retained and the error reported will include the correct source file and line number
        # data for the error. Without this, only the error message is retained and the location information is
        # Line 1, Column 1, of the outer most script file, or the calling location neither of which is useful.
        throw
    }
}
