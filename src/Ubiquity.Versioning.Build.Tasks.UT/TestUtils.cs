// -----------------------------------------------------------------------
// <copyright file="TestUtils.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

using Microsoft.Build.Definition;
using Microsoft.Build.Evaluation;

namespace Ubiquity.Versioning.Build.Tasks.UT
{
    internal static class TestUtils
    {
        internal static (string CiBuildIndex, string CiBuildName, string BuildTime, IDisposable EnvControl) GetGeneratedBuildInfo( )
        {
            using var dummyCollection = new ProjectCollection();
            var options = new ProjectOptions()
            {
                ProjectCollection = dummyCollection
            };

            var project = Project.FromFile(Path.Combine(TestModuleFixtures.RepoRoot, "GeneratedVersion.props"), options);

            return (
                project.GetPropertyValue( "CiBuildIndex" ),
                project.GetPropertyValue( "CiBuildName" ),
                project.GetPropertyValue( "BuildTime" ),
                project.SetEnvFromGeneratedVersionInfo()
                );
        }

        [SuppressMessage( "Performance", "CA1859:Use concrete types when possible for improved performance", Justification = "Not possible, file scoped type" )]
        private static IDisposable SetEnvFromGeneratedVersionInfo( this Project project )
        {
            // Reset-environment variables for this test process based on the build-kind set by build scripts
            // This is needed as the tests don't inherit the environment of the command that runs them.
            switch(project.GetPropertyValue( "BuildKind" ))
            {
            case "LocalBuild":
                Environment.SetEnvironmentVariable( "IsAutomatedBuild", "false" );
                Environment.SetEnvironmentVariable( "IsPullRequestBuild", "false" );
                Environment.SetEnvironmentVariable( "IsReleaseBuild", "false" );
                break;

            case "PullRequestBuild":
                Environment.SetEnvironmentVariable( "IsAutomatedBuild", "true" );
                Environment.SetEnvironmentVariable( "IsPullRequestBuild", "true" );
                Environment.SetEnvironmentVariable( "IsReleaseBuild", "false" );
                break;

            case "CiBuild":
                Environment.SetEnvironmentVariable( "IsAutomatedBuild", "true" );
                Environment.SetEnvironmentVariable( "IsPullRequestBuild", "false" );
                Environment.SetEnvironmentVariable( "IsReleaseBuild", "false" );
                break;

            case "ReleaseBuild":
                Environment.SetEnvironmentVariable( "IsAutomatedBuild", "true" );
                Environment.SetEnvironmentVariable( "IsPullRequestBuild", "false" );
                Environment.SetEnvironmentVariable( "IsReleaseBuild", "true" );
                break;

            default:
                throw new InvalidOperationException( "Unknown build kind in GeneratedVersion.props" );
            }

            return new ResetEnv();
        }
    }

    [SuppressMessage( "StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "DUH! It's file scoped!" )]
    file sealed class ResetEnv
        : IDisposable
    {
        public void Dispose( )
        {
            Environment.SetEnvironmentVariable( "IsAutomatedBuild", null );
            Environment.SetEnvironmentVariable( "IsPullRequestBuild", null );
            Environment.SetEnvironmentVariable( "IsReleaseBuild", null );
        }
    }
}
