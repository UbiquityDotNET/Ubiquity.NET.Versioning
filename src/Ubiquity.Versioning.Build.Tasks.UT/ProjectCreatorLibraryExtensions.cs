// -----------------------------------------------------------------------
// <copyright file="ProjectCreatorLibraryExtensions.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Microsoft.Build.Evaluation;
using Microsoft.Build.Utilities.ProjectCreation;

namespace Ubiquity.Versioning.Build.Tasks.UT
{
    internal static class ProjectCreatorLibraryExtensions
    {
        [SuppressMessage( "Style", "IDE0060:Remove unused parameter", Justification = "Syntactical sugar" )]
        [SuppressMessage( "Style", "IDE0046:Convert to conditional expression", Justification = "Result is Less than 'simplified'" )]
        public static ProjectCreator VersioningProject(
            this ProjectCreatorTemplates templates,
            string targetFramework,
            string packageVersion,
            Action<ProjectCreator>? customAction = null,
            string? path = null,
            string? defaultTargets = null,
            string? initialTargets = null,
            string sdk = "Microsoft.NET.Sdk",
            string? toolsVersion = null,
            string? treatAsLocalProperty = null,
            ProjectCollection? projectCollection = null,
            IDictionary<string, string>? globalProperties = null,
            NewProjectFileOptions? projectFileOptions = NewProjectFileOptions.None)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(targetFramework);

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
                                ).ItemPackageReference(PackageUnderTestId, version: packageVersion, privateAssets: "All")
                                 .Property("Nullable", "disable")
                                 .Property("ManagePackageVersionsCentrally", "false")
                                 .Property("ImplicitUsings","disable")
                                 ;
        }

        private const string PackageUnderTestId = @"Ubiquity.NET.Versioning.Build.Tasks";
    }
}
