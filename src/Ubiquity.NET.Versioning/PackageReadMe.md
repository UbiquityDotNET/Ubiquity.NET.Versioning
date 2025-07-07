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
var quad = new FileVersionQuad(SomeAPiThatRetrievesAFileVersionAsUInt64());
// ...
// NOTE: Since all that is available is a QUAD, which has only 1 bit for CI information,
// there is no way to translate that to a formal CSemVer-CI. Just test ordering of the quad.
if(quad > MinimumVer.FileVersion)
{
    // Good to go!
    if( quad.IsCiBuild )
    {
        // and it's a CI build!
    }
}

// ...
static readonly CSemVer MinimumVer = new(1,2,3/*, ...*/);
```

## Formatting
The library contains support for proper formatting of strings based on the rules
of a SemVer, CSemVer, and CSemVer-CI. The formatting is done case preserving when
possible (Some cases of CSemVer will use string substitution such that `PreRelease` would
simply become `pre`).

## Parsing
The library contains support for parsing of strings based on the rules of a
SemVer, CSemVer, and CSemVer-CI

## Ordering
The types all support `IComparable<T>`<sup>[1](#footnote_1)</sup> and properly handle correct
sort ordering of the versions according to the rules of SemVer (Which, CSemVer and CSemVer-CI
follow with the exception of explicit case insensitivity for AphaNumeric IDs)

>[!WARNING]
> The formal 'spec' for [CSemVer](https://csemver.org) remains mostly silent on the point of
> the short format. See this [known issue](https://github.com/CK-Build/csemver.org/issues/2).
> Since, the existence of that form was to support NuGet V2, which is now obsolete, this
> library does not support the short form at all. (This choice keeps documentation clarity
> [NOT SUPPORTED] and implementation simplicity)

------
<sup><a id="footnote_1">1</a></sup> `SemVer` contains constructors accepting an
`AlhpanumericOrdering` enumeration to identify the ordering expected for a given instance.
Unfortunately, major repositories using SemVer have chosen to use different comparisons. Thus,
a consumer is required to specify if the version is compared insensitive or not.

