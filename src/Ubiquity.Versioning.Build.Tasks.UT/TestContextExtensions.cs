// -----------------------------------------------------------------------
// <copyright file="TestContextExtensions.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml.Linq;

using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Utilities.ProjectCreation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ubiquity.Versioning.Build.Tasks.UT
{
    [SuppressMessage( "StyleCop.CSharp.DocumentationRules", "SA1649:File name should match first type name", Justification = "Simple record used here" )]
    internal readonly record struct ProjectBuildResults(ProjectCreator Creator, BuildOutput Output, bool Success);

    [SuppressMessage( "StyleCop.CSharp.DocumentationRules", "SA1649:File name should match first type name", Justification = "Simple record used here" )]
    internal readonly record struct VersioningProjectBuildResults(ProjectBuildResults BuildResults, BuildProperties Properties);

    internal static class TestContextExtensions
    {
        /// <summary>Determines the repository root from the test context</summary>
        /// <param name="ctx">Test context to use for determining the repository root</param>
        /// <returns>Root of the repository</returns>
        /// <exception cref="InvalidOperationException">If the root is indeterminable from the <paramref name="ctx"/></exception>
        /// <remarks>
        /// The repository root is generally not something the test needs or should care about. However, the test case that validates
        /// the task assembly itself requires access to the BuildVersion XML file used along with the 'generatedversion.props'
        /// file to know what was used to validate against.
        /// </remarks>
        internal static string GetRepoRoot(this TestContext ctx)
        {
            // Currently this assumes a fixed relationship between the "TestRunDirectory" and the actual root
            // If that ever changes, this is the one place to change it.
            return !string.IsNullOrWhiteSpace(ctx.TestRunDirectory)
                   ? Path.GetFullPath( Path.Combine( ctx.TestRunDirectory, "..", "..", ".." ) )
                   : throw new InvalidOperationException("Context.TestRunDirectory is not available");
        }

        internal static VersioningProjectBuildResults CreateTestProjectAndInvokeTestedPackage(
            this TestContext ctx,
            string targetFramework,
            ProjectCollection projectCollection,
            Action<ProjectCreator>? action = null
            )
        {
            var resolveResults = ctx.CreateAndResolveTestProject( targetFramework, action, projectCollection );
            if (!resolveResults.Success)
            {
                return new(resolveResults, default);
            }

            // Since this project uses an imported target, it won't even exist until AFTER ResolvePackageDependencies[DesignTime|ForBuild] comes along.
            var (result, output) = resolveResults.Creator.ProjectInstance.Build("PrepareVersioningForBuild");
            Assert.IsNotNull( result );
            Assert.IsNotNull( result.ProjectStateAfterBuild );

            return new(
                new ProjectBuildResults(resolveResults.Creator, output,  result.OverallResult == BuildResultCode.Success),
                new BuildProperties( result.ProjectStateAfterBuild )
            );
        }

        internal static ProjectBuildResults CreateAndResolveTestProject(
            this TestContext ctx,
            string targetFramework,
            Action<ProjectCreator>? action = null,
            ProjectCollection? projectCollection = null
            )
        {
            if (string.IsNullOrWhiteSpace(ctx.TestResultsDirectory))
            {
                throw new InvalidOperationException("TestResultsDirectory is not available!");
            }

            if (string.IsNullOrWhiteSpace(ctx.TestName))
            {
                throw new InvalidOperationException("TestName is not available!");
            }

            string projectPath = Path.Combine( ctx.TestResultsDirectory, $"{nameof(BuildTaskTests)}-{ctx.TestName}-{targetFramework}.csproj");
            var project = ProjectCreator.Templates
                                        .VersioningProject(targetFramework, customAction: action, projectCollection: projectCollection )
                                        .Save( projectPath )
                                        .TryBuild(
                                            restore: true,
                                            "ResolvePackageDependenciesDesignTime",
                                            out bool resolveResult,
                                            out BuildOutput resolveBuildOutput
                                            );

            using(resolveBuildOutput)
            {
                ctx.LogBuildErrors( resolveBuildOutput );
            }

            return new(project, resolveBuildOutput, resolveResult);
        }

        internal static void LogBuildErrors(this TestContext ctx, BuildOutput buildOutput)
        {
            foreach(var err in buildOutput.ErrorEvents)
            {
                // Log build errors in standard MSBuild format
                // see: https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-diagnostic-format-for-tasks?view=vs-2022
                ctx.WriteLine( $"{err.ProjectFile}({err.LineNumber},{err.ColumnNumber}) : {err.Subcategory} error {err.Code} : {err.Message}" );
            }
        }

        internal static string CreateBuildVersionXmlWithRandomName(
            this TestContext ctx,
            int buildMajor,
            int buildMinor,
            int buildPatch,
            string? preReleaseName = null,
            int? preReleaseNumber = null,
            int? preReleaseFix = null
            )
        {
            if (string.IsNullOrWhiteSpace(ctx.DeploymentDirectory))
            {
                throw new InvalidOperationException("DeploymentDirectory is not available!");
            }

            string retVal = Path.Combine(ctx.DeploymentDirectory, Path.GetRandomFileName());
            using var strm = File.Open(retVal, FileMode.CreateNew);
            var element = new XElement("BuildVersionData",
                                        new XAttribute("BuildMajor", buildMajor),
                                        new XAttribute("BuildMinor", buildMinor),
                                        new XAttribute("BuildPatch", buildPatch)
                                        );
            if(!string.IsNullOrWhiteSpace( preReleaseName ))
            {
                element.Add( new XAttribute( "PreReleaseName", preReleaseName ) );
            }

            if(preReleaseNumber.HasValue)
            {
                element.Add( new XAttribute( "PreReleaseNumber", preReleaseNumber.Value ) );
            }

            if(preReleaseFix.HasValue)
            {
                element.Add( new XAttribute( "PreReleaseNumber", preReleaseFix.Value ) );
            }

            element.Save( strm );
            ctx.WriteLine( $"BuildVersionXML written to: '{retVal}'" );
            return retVal;
        }
    }
}
