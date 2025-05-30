// -----------------------------------------------------------------------
// <copyright file="BuildProperties.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

using Microsoft.Build.Execution;

namespace Ubiquity.Versioning.Build.Tasks.UT
{
    /// <summary>Captures all properties for the build task</summary>
    /// <remarks>
    /// It is worth noting that the properties captured are ALL considered
    /// "OPTIONAL" here. It is up to tests to VERIFY if a property is supposed
    /// to be present or not.
    /// </remarks>
    internal readonly record struct BuildProperties
    {
        public BuildProperties( BuildResult result )
        {
            ArgumentNullException.ThrowIfNull(result.ProjectStateAfterBuild);
            var inst = result.ProjectStateAfterBuild;

            // Manually or from Ubiquity.NET.Versioning.Build.Tasks.props
            BuildTime = inst.GetPropertyValue("BuildTime");
            CiBuildName = inst.GetPropertyValue("CiBuildName");

            // from Ubiquity.NET.Versioning.Build.Tasks.targets/GetRepositoryInfo/GetBuildIndexFromTime task
            CiBuildIndex = inst.GetPropertyValue("CiBuildIndex");

            // Either manually or from Ubiquity.NET.Versioning.Build.Tasks.targets/GetRepositoryInfo/ParseBuildVersionXml task
            BuildMajor = inst.GetPropertyAs<UInt16>("BuildMajor");
            BuildMinor = inst.GetPropertyAs<UInt16>("BuildMinor");
            BuildPatch = inst.GetPropertyAs<UInt16>("BuildPatch");
            PreReleaseName = inst.GetOptionalProperty("PreReleaseName");
            PreReleaseNumber = inst.GetPropertyAs<UInt16>("PreReleaseNumber");
            PreReleaseFix = inst.GetPropertyAs<UInt16>("PreReleaseFix");

            // from Ubiquity.NET.Versioning.Build.Tasks.targets/GetRepositoryInfo/CreateVersionInfo task
            FullBuildNumber = inst.GetOptionalProperty("FullBuildNumber");
            FileVersionMajor = inst.GetPropertyAs<UInt16>("FileVersionMajor");
            FileVersionMinor =inst.GetPropertyAs<UInt16>("FileVersionMinor");
            FileVersionBuild = inst.GetPropertyAs<UInt16>("FileVersionBuild");
            FileVersionRevision = inst.GetPropertyAs<UInt16>("FileVersionRevision");
            PackageVersion = inst.GetOptionalProperty("PackageVersion");

            // from Ubiquity.NET.Versioning.Build.Tasks.targets/SetVersionDependentProperties target
            FileVersion = inst.GetOptionalProperty("FileVersion");
            AssemblyVersion = inst.GetOptionalProperty("AssemblyVersion");
            InformationalVersion = inst.GetOptionalProperty("InformationalVersion");

            IsPullRequestBuild = inst.GetPropertyAs<bool>("IsPullRequestBuild");
            IsAutomatedBuild = inst.GetPropertyAs<bool>("IsAutomatedBuild");
            IsReleaseBuild = inst.GetPropertyAs<bool>("IsReleaseBuild");
        }

        public UInt16? BuildMajor { get; }

        public UInt16? BuildMinor { get; }

        public UInt16? BuildPatch { get; }

        public string? PreReleaseName { get; }

        public UInt16? PreReleaseNumber { get; }

        public UInt16? PreReleaseFix { get; }

        public string? FullBuildNumber { get; }

        public string? PackageVersion { get; }

        public string? BuildTime { get; }

        public string? CiBuildIndex { get; }

        public string? CiBuildName { get; }

        public UInt16? FileVersionMajor { get; }

        public UInt16? FileVersionMinor { get; }

        public UInt16? FileVersionBuild { get; }

        public UInt16? FileVersionRevision { get; }

        public string? BuildMeta { get; }

        public string? FileVersion { get; }

        public string? AssemblyVersion { get; }

        public string? InformationalVersion { get; }

        public bool? IsPullRequestBuild { get; }

        public bool? IsAutomatedBuild { get; }

        public bool? IsReleaseBuild { get; }
    }
}
