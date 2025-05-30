// -----------------------------------------------------------------------
// <copyright file="ProjectCreatorLibraryExtensions.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;

using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities.ProjectCreation;

namespace Ubiquity.Versioning.Build.Tasks.UT
{
    internal static class ProjectCreatorLibraryExtensions
    {
        public static T? GetPropertyAs<T>(this ProjectInstance self, string name, T? defaultValue = null)
            where T : struct
        {
            var prop = self.GetProperty(name);
            return prop is null ? defaultValue : (T?)Convert.ChangeType(prop.EvaluatedValue, typeof(T), CultureInfo.InvariantCulture);
        }

        public static string? GetOptionalProperty(this ProjectInstance self, string name, string? defaultValue = null)
        {
            var prop = self.GetProperty(name);
            return prop is null ? defaultValue : prop.EvaluatedValue;
        }

        // Sadly, project creator library doesn't have support for after build state retrieval...
        // Fortunately, it is fairly easy to create an extension to handle that scenario.
        public static BuildResult Build(this ProjectInstance self, params string[] targetsToBuild)
        {
            return BuildManager.DefaultBuildManager.Build(
                                    new BuildParameters(),
                                    new BuildRequestData(
                                        self,
                                        targetsToBuild,
                                        new HostServices(),
                                        BuildRequestDataFlags.ProvideProjectStateAfterBuild
                                        )
                                    );
        }

        [SuppressMessage( "Style", "IDE0060:Remove unused parameter", Justification = "Syntactical sugar" )]
        public static ProjectCreator VersioningProject(
            this ProjectCreatorTemplates templates,
            Action<ProjectCreator>? customAction = null,
            string? path = null,
#if NETFRAMEWORK
            string targetFramework = "net472",
#else
            string targetFramework = "netstandard2.0",
#endif
            string? defaultTargets = null,
            string? initialTargets = null,
            string sdk = "Microsoft.NET.Sdk",
            string? toolsVersion = null,
            string? treatAsLocalProperty = null,
            ProjectCollection? projectCollection = null,
            IDictionary<string, string>? globalProperties = null,
            NewProjectFileOptions? projectFileOptions = NewProjectFileOptions.None)
        {
            return templates.SdkCsproj(
                                path,
                                sdk,
                                targetFramework,
                                outputType: null,
                                customAction,
                                defaultTargets,
                                initialTargets,
                                treatAsLocalProperty,
                                projectCollection,
                                projectFileOptions,
                                globalProperties
                                ).ItemPackageReference(PackageUnderTestId, version: "5.0.0-*", privateAssets: "All")
                                 .Property("Nullable", "disable")
                                 .Property("ManagePackageVersionsCentrally", "false")
                                 .Property("ImplicitUsings","disable")
                                 ;
        }

        private const string PackageUnderTestId = @"Ubiquity.NET.Versioning.Build.Tasks";
    }
}
