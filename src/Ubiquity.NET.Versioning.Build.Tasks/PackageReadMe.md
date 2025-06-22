# About
The Ubiquity.NET.Versioning.Build.Tasks package provides automated support for build versioning
using a Constrained Semantic Version ([CSemVer](https://csemver.org/)).

>[!WARNING]
> As a [Breaking change in .NET SDK 8](https://learn.microsoft.com/en-us/dotnet/core/compatibility/sdk/8.0/source-link)
> is now setting the build meta data for the `InformationalVersion` property without user
> consent. (A Highly controversial choice that was more easily handled via an OPT-IN pattern)
> Unfortunately, this was set ON by default and made into an 'OPT-OUT' scenario. This library
> will honor such a setting and does not alter/interfere with it in any way. (Though the
> results can, unfortunately, produce surprising behavior).
>
> If you wish to disable this behavior you can set an MSBUILD property to OPT-OUT as follows:  
> `<IncludeSourceRevisionInInformationalVersion>false</IncludeSourceRevisionInInformationalVersion>`  
>  
> This choice of ignoring the additional data is considered to have the least impact on those
> who are aware of the change and those who use this library to set an explicit build meta data
> string. (Principle of least surprise for what this library can control).
>
> The default OPT-OUT behavior is to use the Repository ID (usually a GIT commit hash). This
> is appended with a leading `+` if one isn't already in the `InformationalVersion` property. If
> build metadata is already included (Like from use of this task) the id is appended using a `.`
> instead. Unless the project has opted out of this behavior by setting the property as
> previously described.
>
>Thus, it is ***strongly recommended*** that projects using this package OPT-OUT
> of the new behavior.

## Overview
Officially, SemVer 2.0 doesn't consider or account for publicly available CI builds.
SemVer is only concerned with official releases. This makes CI builds producing 
versioned packages challenging. Fortunately, someone has already defined a solution
to using SemVer in a specially constrained way to ensure compatibility, while also 
allowing for automated CI builds. These new versions are called a [Constrained Semantic
Version](http://csemver.org) (CSemVer).

A CSemVer is unique for each CI build and always increments while supporting official releases.
In the real world there are often cases where there are additional builds that are distinct
from official releases and CI builds. Including Local developer builds, builds generated from
a Pull Request (a.k.a Automated buddy build). CSemVer doesn't explicitly define any format for
these cases. So this library defines a pattern of versioning that is fully compatible with
CSemVer and allows for the additional build types in a way that retains precedence having the
least surprising consequences. In particular, local build packages have a higher precedence
than CI or release versions if all other components of the version match. This ensures that
what you are building includes the dependent packages you just built instead of the last one
released publicly.

The following is a list of the version formats in descending order of precedence:

| Build Type | Format |
|------------|--------|
| Local build  | `{BuildMajor}.{BuildMinor}.{BuildPatch}-ci.{UTCTIME of build }.ZZZ` |
| Pull Request | `{BuildMajor}.{BuildMinor}.{BuildPatch}-ci.{UTCTIME of PR Commit}-PRQ+{BuildMeta}` |
| Official CI builds | `{BuildMajor}.{BuildMinor}.{BuildPatch}-ci.{UTCTIME of build}.BLD+{BuildMeta}` |
| Official PreRelease | `{BuildMajor}.{BuildMinor}.{BuildPatch}-{PreReleaseName}[.PreReleaseNumber][.PreReleaseFix]+{BuildMeta}` |
| Official Release | `{BuildMajor}.{BuildMinor}.{BuildPatch}+{BuildMeta}` |

This package provides a means to automate the generation of these versions in an easy
to use NuGet Package.

The package creates File and Assembly Versions and defines the appropriate MsBuild properties
so the build will automatically incorporate them.
> **NOTE:**  
The automatic use of MsBuild properties requires using the new SDK attribute support for .NET
projects. Where the build auto generates the assembly info. If you are using some other means
to auto generate the assembly level versioning attributes. You can use the properties generated
by this package to generate the attributes.

File and AssemblyVersions are computed based on the CSemVer "Ordered version", which
is a 64 bit value that maps to a standard windows FILEVERSION Quad with each part
consuming 16 bits. This ensures a strong relationship between the  assembly/file versions
and the packages as well as ensures that CI builds can function properly. Furthermore, this
guarantees that each build has a different file and assembly version so that strong name
signing functions properly to enable loading different versions in the same process.

The Major, Minor and Patch versions are only updated in the primary branch at the time
of a release. This ensures the concept that SemVer versions define released products. The
version numbers used are stored in the repository in the BuildVersion.xml

Full [documentation](https://ubiquitydotnet.github.io/CSemVer.GitBuild/) is available at
the project's documentation site.
