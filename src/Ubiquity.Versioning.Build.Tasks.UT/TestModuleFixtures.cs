// -----------------------------------------------------------------------
// <copyright file="TestModuleFixtures.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

using Microsoft.Build.Utilities.ProjectCreation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ubiquity.Versioning.Build.Tasks.UT
{
    // Provides common location for one time initialization for all tests in this assembly
    // Doing the package repo construction here, allows tests to run in parallel without
    // hitting access denied errors due to the use of the location in some other test.
    [TestClass]
    public static class TestModuleFixtures
    {
        [AssemblyInitialize]
        public static void AssemblyInitialize( TestContext ctx )
        {
            ArgumentNullException.ThrowIfNull( ctx );
            ArgumentException.ThrowIfNullOrWhiteSpace( ctx.TestRunDirectory );

            MSBuildAssemblyResolver.Register();

            // This assumes the solutions '.runsettings' file has re-directed the run output
            // to a well known location. There's no way to "pass" in the location to the build
            // of this test.
            string buildOutputPath = Path.GetFullPath( Path.Combine(ctx.TestRunDirectory, "..", ".."));

            string buildRoot = Path.GetFullPath( Path.Combine(buildOutputPath, ".."));

            // Generate a fake directory.build.[props|targets] in the test run directory to prevent MSBUILD
            // from searching beyond these to find the REPO one in the root.
            ProjectCreator.Create()
                          .Save( Path.Combine( ctx.TestRunDirectory, "Directory.Build.props" ) );

            ProjectCreator.Create()
                          .Save( Path.Combine( ctx.TestRunDirectory, "Directory.Build.targets" ) );

            // Ensure environment is clear of any overrides to ensure tests are validating the correct behavior
            // Individual tests MAY NOT set these again see remarks below for details. But there is currently,
            // no consistent mechanism for controlling the set of variables provided to child processes for
            // the build of created projects.
            bool hasBadVars = false;
            foreach(string envVar in TaskInputPropertyNames)
            {
                string? value = Environment.GetEnvironmentVariable(envVar);
                if( value is not null)
                {
                    ctx.WriteLine($"ENV VAR SET: {envVar}");
                    hasBadVars = true;
                }
            }

            if (hasBadVars)
            {
                // Sadly, setting the bad env vars to null doesn't have any impact on the way that build generates the
                // child processes. (Even worse is that it wouldn't have ANY impact under non-windows systems as those
                // clone the environment locally and only impact the clone, you'd need to explicitly provide the env
                // for ALL child processes)
                throw new InvalidOperationException("Overriding environment variables exist, tests will NOT run correctly");
            }

            // Nuget.Config is written to the root path, so this folder must be a parent of the
            // test project files or the settings will NOT apply!
            PackageRepo = PackageRepository.Create(
                ctx.TestRunDirectory,                            // '.nuget/packages' repo folder goes here
                new Uri(Path.Combine(buildOutputPath, "NuGet")), // Local feed (Contains location of the build of the package under test)
                new Uri("https://api.nuget.org/v3/index.json") // standard NuGet Feed
            );
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup( )
        {
            PackageRepo?.Dispose();
        }

        private static void CopyFile(string srcDir, string fileName, string dstDir)
        {
            File.Copy(Path.Combine(srcDir, fileName), Path.Combine(dstDir, fileName), overwrite: true);
        }

        // internal static string? OldEnvNugetPackages { get; private set; }
        private static PackageRepository? PackageRepo;

        private static readonly ImmutableArray<string> TaskInputPropertyNames = [
            "BuildTime",
            "IsPullRequestBuild",
            "IsAutomatedBuild",
            "IsReleaseBuild",
            "CiBuildIndex",
            "CiBuildName",
            "BuildVersionXml",
            "BuildMajor",
            "FullBuildNumber",
            "FileVersion",
            "AssemblyVersion",
            "InformationalVersion"
        ];
    }
}
