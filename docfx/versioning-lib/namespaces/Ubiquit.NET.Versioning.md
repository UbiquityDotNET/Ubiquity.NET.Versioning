---
uid: Ubiquity.NET.Versioning
remarks: *content
---
This namespace contains the stand alone versioning support for the different forms of
version information supported.

### Class Diagram
The following diagram serves to illustrates the primary relationships between the various
types in this namespace:

``` mermaid
classDiagram
    note for SemVer "ALL CsemVer[-CI] strings are syntactically valid SemVer. However, the reverse is not always true."
    class SemVer {
        + BigInteger Major
        + BigInteger Minor
        + BigInteger Patch
        + ImmutableArray~string~ PreRelease
        + ImmutableArray~string~ BuildMeta
    }

    class CSemVer {
        + FileVersionQuad FileVersion
        + Int64 OrderedVersion
        + PreReleaseVersion? PreReleaseVersion
    }

    class CSemVerCI {
        + CSemVer BaseVersion
        + string BuildIndex
        + string BuildName
    }

    class PreReleaseVersion {
        + int Index
        + int Number
        + int Fix
        + string Name
        + char ShortName
    }

    note for FileVersionQuad "ODD values in Revision are reserved for CI builds"
    class FileVersionQuad {
        + UInt16 Major
        + UInt16 Minor
        + UInt16 Build
        + UInt16 Revision
    }

    <<struct>> FileVersionQuad
    <<struct>> PreReleaseVersion
    CSemVer *-- FileVersionQuad:FileVersion
    CSemVer "0..1" *-- PreReleaseVersion:PreReleaseVersion
    CSemVerCI "1" *-- CSemVer:BaseVersion
```
>[!IMPORTANT]
> There is no inheritance between a `SemVer`, `CSemVer`, and `CSemVerCI`. While
> conversion is possible in many cases they are not completely compatible due the
> constraints on ranges and rules of ALWAYS comparing case insensitive for `CSemVer`
> and `CSemVerCI`

The primary differences between a generic SemVer, a CSemVer and CSemVerCI is in how the
sequence of pre-release versioning components is handled and the constraints placed on
the Major, Minor and Patch version numbers.

>[!NOTE]
> A SemVer technically has no constraints on the range of the integral components and
> thus a `BigInteger` is used. Though, in practical terms, if any of the components
> exceeds the size of `UInt64` there's probably something wrong with how the thing the
> version applies to is versioned :confused:. More realistically, CSemVer[-CI]
> constrains the integral components to specific ranges to allow conversion to an ordered
> version and `FileVersionQuad`.


>[!IMPORTANT]
> A CSemVer[-CI] is ***ALWAYS*** ordered using a case-insensitive comparison for
> AlphaNumeric Identifiers in the version. Sadly, the SemVer spec is not explicit on the
> point of case sensitivity and various major implementations for popular frameworks have
> chosen different approaches. Thus a consumer of a pure SemVer needs to know which kind
> of comparison to use in order to get correct results.
>
> Due to the ambiguity of case sensitivity in ordering, it is recommended that all uses
> of SemVer in the real world use ALL the same case (All *UPPER* or all *lower*). This
> avoids the confusion and produces correct ordering no matter what variant of comparison
> a consumer uses. Problems come when the version uses a *Mixed* case format.

### CSemVer Constraints on the integral components
In particular the values are constrained
as follows:

| Name  | Range |
|-------|-----------|
| Major | [0-99999] |
| Minor | [0-49999] |
| Patch | [0-9999]  |

## CSemVer constraints on the release sequence
Technically, SemVer does not limit the number of components to a pre-release value. It
could be ANY finite set. This is, of course unreasonable in the real world so CSemVer
places constraints on the number of components AND attributes particular meaning to each
part. A CSemVer may have up to three pre-release components that are interpreted
according to the following table:

| Index | Name | Description |
|-------|------|-------------|
| 0 | Name<sup>[1](#footnote_1)</sup> | Name of the pre-release (one of a fixed set of 8 names) |
| 1 | Number | pre-release number for a build |
| 2 | Fix  | pre-release fix for a build |

### CSemVer[CI] Pre-release names
The names of a pre-release are constrained in CSemVer[-CI] to the following:<sup>[1](#footnote_1)</sup>

| Name | Index |
|------|-------|
| alpha   | 0 |
| beta    | 1 |
| delta   | 2 |
| epsilon | 3 |
| gamma   | 4 |
| kappa   | 5 |
| pre<sup>[2](#footnote_2)</sup>| 6
| rc | 7 |

------
<sup><a id="footnote_1">1</a></sup> This library does NOT support the short form of names in a
CSemVer. The exact string representation of the short form of a CSemVer as specified is
not entirely clear. (see:[this issue](https://github.com/CK-Build/csemver.org/issues/2))
This implementation has chosen to ignore the short form completely. Based on what little
is said about it in the spec, it was created to support a limitation in NuGet v2, which
is now obsolete. Thus, the libraries do not support producing strings using the short
form, nor do they recognize one when parsing. 

<sup><a id="footnote_2">2</a></sup> `prerelease` is always considered valid as well.
Internally it is automatically converted to the shorter `pre` form.

----
