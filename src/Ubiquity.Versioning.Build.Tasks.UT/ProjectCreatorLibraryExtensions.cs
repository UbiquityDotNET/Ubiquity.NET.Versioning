// -----------------------------------------------------------------------
// <copyright file="ProjectCreatorLibraryExtensions.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml.Linq;

using Microsoft.Build.Evaluation;
using Microsoft.Build.Utilities.ProjectCreation;

namespace Ubiquity.Versioning.Build.Tasks.UT
{
    internal static class ProjectCreatorLibraryExtensions
    {
        public static PackageRepository SourceMapping( this PackageRepository pkgRepo, string pkgSourceKey, string pattern )
        {
            var nugetConfig = XDocument.Load(pkgRepo.NuGetConfigPath);
            XElement configuration = GetOrCreateConfigurationElement( nugetConfig );
            XElement sourceMapping = GetOrCreateSourceMappingElement( configuration );
            XElement pkgSrcElement = GetOrCreatePackageSource( sourceMapping, pkgSourceKey );
            _ = GetOrCreatePackageElement(pkgSrcElement, pattern);
            nugetConfig.Save(pkgRepo.NuGetConfigPath);
            return pkgRepo;
        }

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
            NewProjectFileOptions? projectFileOptions = NewProjectFileOptions.None )
        {
            ArgumentException.ThrowIfNullOrWhiteSpace( targetFramework );

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
                                ).ItemPackageReference( PackageUnderTestId, version: packageVersion, privateAssets: "All" )
                                 .Property( "Nullable", "disable" )
                                 .Property( "ManagePackageVersionsCentrally", "false" )
                                 .Property( "ImplicitUsings", "disable" )
                                 ;
        }

        private static XElement GetOrCreateSourceMappingElement( XElement configuration )
        {
            XElement? sourceMapping = configuration.Element("packageSourceMapping");
            if(sourceMapping is null)
            {
                sourceMapping = new XElement( "packageSourceMapping" );
                configuration.Add( sourceMapping );
            }

            return sourceMapping;
        }

        private static XElement GetOrCreateConfigurationElement( XDocument nugetConfig )
        {
            XElement? configuration = nugetConfig.Element("configuration");
            if(configuration is null)
            {
                configuration = new XElement( "configuration" );
                nugetConfig.Add( configuration );
            }

            return configuration;
        }

        private static XElement GetOrCreatePackageSource( XElement sourceMapping, string pkgSource )
        {
            XElement? packageSource = ( from e in sourceMapping.Elements("packageSource")
                                        from a in e.Attributes()
                                        where a.Name == "key" && a.Value == pkgSource
                                        select e
                                      ).FirstOrDefault();

            if(packageSource is null)
            {
                packageSource = new XElement( "packageSource", new XAttribute( "key", pkgSource ) );
                sourceMapping.Add( packageSource );
            }

            return packageSource;
        }

        private static XElement GetOrCreatePackageElement( XElement pkgSrc, string pattern )
        {
            XElement? package = ( from e in pkgSrc.Elements("package")
                                  from a in e.Attributes()
                                  where a.Name == "pattern" && a.Value == pattern
                                  select e
                                ).FirstOrDefault();

            if(package is null)
            {
                package = new XElement( "package", new XAttribute( "pattern", pattern) );
                pkgSrc.Add( package );
            }

            return package;
        }

        private const string PackageUnderTestId = @"Ubiquity.NET.Versioning.Build.Tasks";
    }
}
