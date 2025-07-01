# About
The Ubiquity.NET.Versioning library provides types to support use of versioning via
1) [Semantic Versioning](https://semver.org)
2) [Constrained Semantic Versioning](https://csemver.org)
    - Including Continuous Integration (CI) via CSemVer-CI

It is viable as a standalone package to allow validation of or comparisons to versions
reported at runtime. (Especially from native interop that does not support package
dependencies or versioning at runtime.)

## Example
``` C#
var epectedMinimum = new CSemVer(20, 1, 5, "alpha");
var actual = CSemVer.From(SomeAPIToRetrieveAVersionAsUInt64());
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
The types all support `IComparable<T>` and properly handle correct sort ordering of the
versions according to the rules of SemVer (Which, CSemVer and CSemVer-CI follow)

>[!WARNING]
> The [SemVer spec](https://semver.org) does not explicitly mention case sensitivity for
> comparing the pre-release components (AlphaNumeric Identifiers) in a version. It does
> state that they are compared lexicographically, which would imply they are case
> sensitive. However, major repository implementations have chosen different approaches
> to how the strings are compared and thus the ambiguities of reality win out over any
> specified implicit behavior. Thus consumers, MUST specify the expected ordering for
> a SemVer when creating it.

>[!WARNING]
> The formal 'spec' for [CSemVer](https://csemver.org) remains mostly silent on the point
> of the short format. See this [known issue](https://github.com/CK-Build/csemver.org/issues/2).
> Since, the existence of that form was to support NuGet V2, which is now obsolete, this
> library does not support the short form at all. (This choice keeps documentation
> clarity [NOT SUPPORTED] and implementation simplicity)

