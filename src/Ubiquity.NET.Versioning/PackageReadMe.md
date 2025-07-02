# About
The Ubiquity.NET.Versioning library provides types to support use of versioning via
1) [Semantic Versioning](https://semver.org)
2) [Constrained Semantic Versioning](https://csemver.org)
    - Including Continuous Integration (CI) via CSemVer-CI

It is viable as a standalone package to allow validation of or comparisons to versions reported
at runtime. (Especially from native interop that does not support package dependencies or
versioning at runtime.)

## Example
``` C#
var epectedMinimum = new CSemVer(20, 1, 5, "alpha"); // Usually static
//...

var versionQuad = new FileVersionQuad(SomeAPIToRetrieveAVersionAsUInt64());
SemVer actual = versionQuad.ToSemVer();
if (actual < expectedMinimum)
{
    // Uh-OH! "older" version!
}

// Good to go...
```

## Formatting
The library contains support for proper formatting of strings based on the rules
of a SemVer, CSemVer, and CSemVer-CI

## Parsing
The library contains support for parsing of strings based on the rules of a
SemVer, CSemVer, and CSemVer-CI

## Ordering
The types all support `IComparable<T>`<sup>[1](#footnote_1)</sup> and properly handle correct
sort ordering of the versions according to the rules of SemVer (Which, CSemVer and CSemVer-CI
follow)

>[!WARNING]
> The formal 'spec' for [CSemVer](https://csemver.org) remains mostly silent on the point
> of the short format. See this [known issue](https://github.com/CK-Build/csemver.org/issues/2).
> Since, the existence of that form was to support NuGet V2, which is now obsolete, this
> library does not support the short form at all. (This choice keeps documentation clarity
> [NOT SUPPORTED] and implementation simplicity)

------
<sup><a id="footnote_1">1</a></sup>Unfortunately, major repositories using SemVer have
chosen to use different comparisons. Thus, a consumer is required to know a-priori if the
version is compared insensitive or not. Thus all constructors accept an enum indicating
the sort ordering to use. Additional, parsing accepts an IFormatProvider, which should
provide an `AlphaNumeirvOrdering` value to specify the ordering. If none is provided, the
default is used. (SemVer uses CaseSensitive comparisons, CSemVer and CSemVerCI ALWAYS use
case insensitive) `IComparer<SemVer>` instances are available for cases where the versions
are from mixed sources and the application wishes to order the versions.

