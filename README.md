# Ubiquity.NET.Versioning
This repo includes support for versioning numbers. This support includes:
1) Automated Constrained Semantic Versioning ([CSemVer](https://csemver.org)) for MSBuild
   projects.
2) A standalone library useful for parsing, sorting and validating versions.
    - [SemVer](https://semver.org)
    - [CSemVer](https://csemver.org)
        - This is a Constrained Semantic Version (That is, a strict subset of a SemVer)
    - [CSemVer-CI](https://csemver.org)
        - This is also a Constrained Semantic Version but is designed for ***POST-RELEASE***
          CI build numbering. It is NOT a CSemVer but IS a SemVer.

## Status
[![NuGet Version](https://img.shields.io/nuget/vpre/Ubiquity.NET.Versioning)](https://www.nuget.org/packages/Ubiquity.NET.Versioning)
![NuGet](https://img.shields.io/nuget/dt/Ubiquity.NET.Versioning.svg)  
![PR/CI Work Flow Status](https://img.shields.io/github/actions/workflow/status/UbiquityDotNET/Ubiquity.NET.Versioning/pr-build.yml?label=PR%2FCI%20Build%20Status)
![Release Work Flow Status](https://img.shields.io/github/actions/workflow/status/UbiquityDotNET/Ubiquity.NET.Versioning/release-build.yml?label=Release%20Build%20Status)

## Supported Runtimes
This library supports the following .NET runtime versions.
* .NET 8.0 (LTSC) and later

.NET standard 2.0 does not support the many uses of `static abstract` that are utilized
in this library. Therefore, supporting that runtime is not possible with simple PolyFill's.
It requires fairly significant preprocessor conditional coding AND major changes to the
tests to validate BOTH variants. Thus, unless there is a strong compelling case that's not
something this library will support.

## Overview
Officially, NuGet Packages use a SemVer 2.0 (see http://semver.org).
However, SemVer 2.0 doesn't consider or account for publicly available CI builds.
SemVer is only concerned with official releases. This makes CI builds producing 
versioned packages challenging. Fortunately, someone has already defined a solution
to using SemVer in a specially constrained way to ensure compatibility, while also 
allowing for automated CI builds. These new versions are called a [Constrained Semantic
Version](http://csemver.org) (CSemVer).

## Constrained use of Constrained Semantic Versions
A CSemVer is unique for each CI build and always increments while supporting official
releases. In the real world there are often cases where there are additional builds that
are distinct from official releases and CI builds. Including Local developer builds,
builds generated from a Pull Request (a.k.a Automated buddy build). CSemVer doesn't
explicitly define any format for these cases. So this library defines a pattern of
versioning that is fully compatible with CSemVer and allows for the additional build types
in a way that retains precedence having the least surprising consequences. In particular,
local build packages have a higher precedence than automated builds (CI or release)
versions if all other components of the version match. This ensures that what you are
building includes the dependent packages you just built instead of the last one released
publicly.

>[!WARNING]
> The formal 'spec' for [CSemVer](https://csemver.org) remains silent on the point of the
> short format.<sup>[1](#footnote_1)</sup> Instead it relies on only examples. However,
> the examples are inconsistent on the requirement of a delimiter between the short name
> and number components of a version. It shows two examples '1.0.0-b03-01' ***AND***
> '5.0.0-r-04-13'. So, which is it? Is the delimiter required or not?
>
> This may seem like an entirely academic issue, but when parsing an input it impacts the
> validity of inputs. Also, when the dealing with ordering and the length of otherwise
> equal components comes into play it can impact the behavior as well. How are
> `1.0.0-b03-01` and `1.0.0-b-03-01` ordered in relation to each other? Is the former
> even a valid CSemVer?
>
> ***This implementation is making no assumptions and simply does NOT support the short
> form.*** That may seem like a hard stance but given the ambiguities of the spec,
> documenting the behavior is difficult. Additionally, handling all the potential
> variations makes for extremely complex implementation code. All of that for a feature
> in support of a NuGet client that is now obsolete. (NuGet v3 can handle the full name
> just fine!). Thus, the lack of support in this library.

## End User Documentation
Full documentation on the tasks is available in the project's
[docs site](https://ubiquitydotnet.github.io/Ubiquity.NET.Versioning/)

## Building the tasks
Documentation on building and general maintenance of this repo are provided in the
[Wiki](https://github.com/UbiquityDotNET/Ubiquity.NET.Versioning/wiki).

----
<sup><a id="footnote_1">1</a></sup>See: [This issue](https://github.com/CK-Build/csemver.org/issues/2)
which was reported upon testing this library and found ambiguities.
