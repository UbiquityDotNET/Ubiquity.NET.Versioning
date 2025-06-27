// -----------------------------------------------------------------------
// <copyright file="AppContextSwitches.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

// IMPLEMENTATION NOTE:
// ALL AppContext switches in the Ubiquity.NET family of projects use the same naming format for consistency
// <Fully Qualified Namespace>.AppContextSwitches.<SwitchName>
// The FullyQualified Namespace should begin with `Ubiquity.NET` and should be the root namespace of the
// assembly the switch is controlling behavior for.
// In code, they use the same pattern as is used here:
// A static class called AppContextSwitches in the assembly namespace (matching the one in the string form of the name)
// with a const string containing the name of the switch and bool Get/Set properties as convenience accessors.
//
// TODO: Source generator to create this from simpler class that declares properties.
// TODO: Analyzer that detects if class is names "AppContextSwitches" to ensure only one exists in the namespace and
//       follows the correct pattern.
// CONSIDER: Might not be able to use a generator/analyzer in this library as those would likely need to USE the versioning
//           task to apply a version number... May be worth considering how to isolate the task from the versioning lib so
//           that only the task itself has limits on dependencies.

namespace Ubiquity.NET.Versioning
{
    /// <summary>Utility class for supporting <see cref="AppContext"/> switches</summary>
    /// <remarks>
    /// <para>These switches define behavior with regard to some ambiguous aspect of the CSemVer/CSemVer-CI
    /// specs as of spec v1.0.0-rc.1</para>
    /// <note>
    /// Once published in a non-preview release, the name of a switch CANNOT change. It may become inert, but
    /// is NEVER re-purposed to a different meaning or even changed to correct a mis-spelling. Such a correction
    /// would ADD a new (correctly spelled) name and then adjust the implementation to treat the old and new forms
    /// identically. The published name and it's associated behavior is immutable. Whether it does anything or not
    /// depends on the version, but the behavior itself may never be re-defined. That is, it always either does what
    /// it was documented to do in the first release available, or it does nothing. It ***NEVER*** does something else.
    /// </note>
    /// </remarks>
    /// <seealso href="https://csemver.org/"/>
    public static class AppContextSwitches
    {
        /// <summary>Name of the switch that controls the <see cref="CSemVerCIOnlySupportsBuildMetaOnZeroTimedVersions"/> property</summary>
        public const string CSemVerCIOnlySupportsBuildMetaOnZeroTimedVersionsName
            = "Ubiquity.NET.Versioning.AppContextSwitches.CSemVerCIOnlySupportsBuildMetaOnZeroTimedVersions";

        /// <summary>Gets or sets a value indicating whether the <see cref="CSemVerCIOnlySupportsBuildMetaOnZeroTimedVersionsName"/> switch is enabled or not</summary>
        /// <remarks>
        /// <para>The spec is vague on the precise syntax of a CSemVer-CI value and whether a given form can include build meta data.
        /// It is at least explicitly allowed for ZeroTimed base values. But it is unclear if it is supposed to be allowed
        /// for any "latest release" forms. Sadly, .NET builds using SourceLink will automatically add the build meta if not present
        /// so this implementation chooses to assume that is legit. This seems reasonable as the build meta doesn't participate
        /// in the ordering of version values and is syntactically compatible with both forms of CSemVer-CI. That is, the default behavior
        /// of allowing it has low risk of causing problems, but the reverse can.</para>
        /// <para>As with all <see cref="AppContext"/> switches the default state of this switch is OFF. Setting it ON, is a manual operation
        /// that may use one of the standard mechanisms supported by <see cref="AppContext"/> using this name. Or, programmatically via the
        /// <see cref="CSemVerCIOnlySupportsBuildMetaOnZeroTimedVersions"/>.</para>
        /// </remarks>
        public static bool CSemVerCIOnlySupportsBuildMetaOnZeroTimedVersions
        {
            get => GetSwitchValue(CSemVerCIOnlySupportsBuildMetaOnZeroTimedVersionsName);
            set => AppContext.SetSwitch(CSemVerCIOnlySupportsBuildMetaOnZeroTimedVersionsName, value);
        }

        private static bool GetSwitchValue(string name)
        {
            bool found = AppContext.TryGetSwitch(name, out bool currentVal);
            return found && currentVal;
        }
    }
}
