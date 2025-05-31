# About
The Ubiquity.NET.Versioning library provides types to support use of a Constrained Semantic
Version ([CSemVer](https://csemver.org/)) in a build. It is viable as a standalone package to allow
validation of or comparisons to versions reported at runtime. (Especially from native interop that
does not support NuGet package dependencies or versioning at runtime.)

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
The library also contains support for proper formatting of strings based on the rules
of a CSemVer
