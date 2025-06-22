# Ubiquity.NET.Versioning
`Ubiquity.NET.Versioning*` family of libraries provides support for a number of scenarios
but the primary focus is automated build versioning that embraces the principal of least surprise
while conforming to the syntax of CSemVer and CSemVer-CI

## The Libraries in this repository
(At least the ones generating docs at this point anyway! :grin:)

| Library | Description |
|---------|-------------|
| [Ubiquity.NET.Versioning](versioning-lib/index.md) | This library contains support for use of CSemVer at runtime |
| [Ubiquity.NET.Versioning.Build.Tasks](build-tasks/index.md) | This library contains support for automated versioning at BUILD time |

>[!IMPORTANT]
> There is confusion on the ordering of a CI build with relation to a release build with
> CSemVer. A CI Build is either an initial build of an unreleased version with
> [Major.Minor.Patch] == [0.0.0]. Or, it is based on the previously released version and is
> [Major.Minor.Patch+1]. That is, a CI build is ordered BEFORE all other release builds, or it
> is ordered AFTER, and is based on, a release build! In particular a CI build version does NOT
> indicate what it will become when it is finally released, but what release it was based on
> (If any).

---
[Attributions](Attributions.md)
