class PreReleaseVersion
{
    [ValidateSet("alpha", "beta", "delta", "epsilon", "gamma", "kappa", "prerelease", "rc")]
    [string] $Name;

    [ValidateSet("a", "b", "d", "e", "g", "k", "p", "r")]
    [string] $ShortName;

    [ValidateRange(-1,7)]
    [int] $Index;

    [ValidateRange(0,99)]
    [int] $Number;

    [ValidateRange(0,99)]
    [int] $Fix;

    PreReleaseVersion([hashtable]$buildVersionXmlData)
    {
        $preRelName = $buildVersionXmlData['PreReleaseName']

        if( ![string]::IsNullOrWhiteSpace( $preRelName ) )
        {
            $this.Index = [PreReleaseVersion]::GetPrerelIndex($preRelName)
            if($this.Index -ge 0)
            {
                $this.Name = [PreReleaseVersion]::PreReleaseNames[$this.Index]
                $this.ShortName = [PreReleaseVersion]::PreReleaseShortNames[$this.Index]
            }

            $this.Number = $buildVersionXmlData['PreReleaseNumber'];
            $this.Fix = $buildVersionXmlData['PreReleaseFix'];
        }
        else
        {
            $this.Index = -1;
        }
    }

    [string] ToString([bool] $useShortForm = $false)
    {
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

        return $bldr.ToString()
    }

    hidden static [int] GetPrerelIndex([string] $preRelName)
    {
        $preRelIndex = -1
        if(![string]::IsNullOrWhiteSpace($preRelName))
        {
            $preRelIndex = [PreReleaseVersion]::PreRleaseNames |
                         ForEach-Object {$index=0} {@{Name = $_; Index = $index++}} |
                         Where-Object {$_["Name"] -ieq $preRelName} |
                         ForEach-Object {$_["Index"]} |
                         Select-Object -First 1

            # if not found in long names, test against the short names
            if($preRelIndex -lt 0)
            {
                $preRelIndex = [PreReleaseVersion]::PreReleaseShortNames |
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

    [ValidatePattern('\A[a-z0-9-]+\Z')]
    [string] $CiBuildIndex;

    [ValidatePattern('\A[a-z0-9-]+\Z')]
    [string] $CiBuildName;

    [ulong] $OrderedVersion;

    [Version] $FileVersion;

    [PreReleaseVersion] $PreReleaseVersion;

    CSemVer([hashtable]$buildVersionXmlData)
    {
        $this.Major = $buildVersionXmlData["BuildMajor"]
        $this.Minor = $buildVersionXmlData["BuildMinor"]
        $this.Patch = $buildVersionXmlData["BuildPatch"]
        if($buildVersionXmlData["PreReleaseName"])
        {
            $this.PreReleaseVersion = [PreReleaseVersion]::new($buildVersionXmlData)
            if(!$this.PreReleaseVersion)
            {
                throw "Internal ERROR: PreReleaseVersion version is NULL!"
            }
        }

        $this.BuildMetadata = $buildVersionXmlData["BuildMetadata"]

        $this.CiBuildName = $buildVersionXmlData["CiBuildName"];
        $this.CiBuildIndex = $buildVersionXmlData["CiBuildIndex"];

        if( (![string]::IsNullOrEmpty( $this.CiBuildName )) -and [string]::IsNullOrEmpty( $this.CiBuildIndex ) )
        {
            throw "CiBuildIndex is required if CiBuildName is provided";
        }

        if( (![string]::IsNullOrEmpty( $this.CiBuildIndex )) -and [string]::IsNullOrEmpty( $this.CiBuildName ) )
        {
            throw "CiBuildName is required if CiBuildIndex is provided";
        }

        $this.OrderedVersion = [CSemVer]::GetOrderedVersion($this.Major, $this.Minor, $this.Patch, $this.PreReleaseVersion)
        $fileVer64 = $this.OrderedVersion -shl 1
        if($this.CiBuildIndex -and $this.CiBuildName)
        {
            $fileVer64 += 1;
        }

        $this.FileVersion = [CSemVer]::ConvertToVersion($fileVer64)
    }

    [string] ToString([bool] $includeMetadata, [bool]$useShortForm)
    {
        $bldr = [System.Text.StringBuilder]::new()
        $bldr.AppendFormat('{0}.{1}.{2}', $this.Major, $this.Minor, $this.Patch)
        if($this.PreReleaseVersion)
        {
            $bldr.Append($this.PreReleaseVersion.ToString($useShortForm))
        }

        $hasPreRel = $this.PreReleaseVersion -and $this.PreReleaseVersion.Index -ge 0
        if($this.CiBuildIndex -and $this.CiBuildName)
        {
            $bldr.Append($hasPreRel ? '.' : '--')
            $bldr.AppendFormat('ci.{0}.{1}', $this.CiBuildIndex, $this.CiBuildName)
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

    hidden static [ulong] GetOrderedVersion($Major, $Minor, $Patch, [PreReleaseVersion] $PreReleaseVersion)
    {
        [ulong] $MulNum = 100;
        [ulong] $MulName = $MulNum * 100;
        [ulong] $MulPatch = ($MulName * 8) + 1;
        [ulong] $MulMinor = $MulPatch * 10000;
        [ulong] $MulMajor = $MulMinor * 50000;

        [ulong] $retVal = (([ulong]$Major) * $MulMajor) + (([ulong]$Minor) * $MulMinor) + ((([ulong]$Patch) + 1) * $MulPatch);
        if( $PreReleaseVersion -and $PreReleaseVersion.Index -ge 0 )
        {
            $retVal -= $MulPatch - 1;
            $retVal += [ulong]($PreReleaseVersion.Index) * $MulName;
            $retVal += [ulong]($PreReleaseVersion.Number) * $MulNum;
            $retVal += [ulong]($PreReleaseVersion.Fix);
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

        if($FullInit)
        {
            # PowerShell doesn't export enums from a script module, so the type of this return is
            # "unpronounceable" [In C++ terminology]. So, convert it to a string so it is usable in this
            # script.
            [string] $buildKind = Get-CurrentBuildKind

            $verInfo = Get-ParsedBuildVersionXML -BuildInfo $buildInfo
            if($buildKind -ne "ReleaseBuild")
            {
                $verInfo['CiBuildIndex'] = ConvertTo-BuildIndex $env:BuildTime
            }

            switch($buildKind)
            {
                "LocalBuild" { $verInfo['CiBuildName'] = "ZZZ" }
                "PullRequestBuild" { $verInfo['CiBuildName'] = "PRQ" }
                "CiBuild" { $verInfo['CiBuildName'] = "BLD" }
                "ReleaseBuild" { }
                default {throw "unknown build kind" }
            }

            # Generate props file with the version information for this build
            # While it is plausible to use ENV vars to overload or set properties
            # that leads to dangling values during development, which makes for
            # a LOT of wasted time chasing down why a change didn't work...
            $csemVer = [CSemVer]::New($verInfo)
            $xmlDoc = [System.Xml.XmlDocument]::new()
            $projectElement = $xmlDoc.CreateElement("Project")
            $xmlDoc.AppendChild($projectElement) | Out-Null

            $propGroupElement = $xmlDoc.CreateElement("PropertyGroup")
            $projectElement.AppendChild($propGroupElement) | Out-Null

            $fileVersionElement = $xmlDoc.CreateElement("FileVersion")
            $fileVersionElement.InnerText = $csemVer.FileVersion.ToString()
            $propGroupElement.AppendChild($fileVersionElement) | Out-Null

            $packageVersionElement = $xmlDoc.CreateElement("PackageVersion")
            $packageVersionElement.InnerText = $csemVer.ToString($false,$true) # short form of version
            $propGroupElement.AppendChild($packageVersionElement) | Out-Null

            $productVersionElement = $xmlDoc.CreateElement("ProductVersion")
            $productVersionElement.InnerText = $csemVer.ToString($true, $false) # long form of version
            $propGroupElement.AppendChild($productVersionElement) | Out-Null

            $assemblyVersionElement = $xmlDoc.CreateElement("AssemblyVersion")
            $assemblyVersionElement.InnerText = $csemVer.FileVersion.ToString()
            $propGroupElement.AppendChild($assemblyVersionElement) | Out-Null

            $informationalVersionElement = $xmlDoc.CreateElement("InformationalVersion")
            $informationalVersionElement.InnerText = $csemVer.ToString($true, $false) # long form of version
            $propGroupElement.AppendChild($informationalVersionElement) | Out-Null

            $buildGeneratedPropsPath = Join-Path $buildInfo["RepoRootPath"] "GeneratedVersion.props"
            $xmlDoc.Save($buildGeneratedPropsPath)

        }

        Write-Information 'Deleting common build versioning env vars'
        # override the build version related values set from CommonBuild
        # This repo is unique in that it CREATES the package that uses these.
        # The actual build of these packages use `GeneratedVersion.props`
        $env:IsAutomatedBuild = $null
        $env:IsPullRequestBuild =$null
        $env:IsReleaseBuild =$null
        $env:CiBuildName =$null
        $env:BuildTime =$null

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
