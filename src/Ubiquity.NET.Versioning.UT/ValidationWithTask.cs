// -----------------------------------------------------------------------
// <copyright file="ValidationWithTask.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ubiquity.NET.Versioning.UT
{
    [TestClass]
    [TestCategory( "Build Task Assembly" )]
    public class ValidationWithTask
    {
        public ValidationWithTask( TestContext ctx )
        {
            ArgumentNullException.ThrowIfNull( ctx );
            ArgumentException.ThrowIfNullOrWhiteSpace( ctx.TestResultsDirectory );

            Context = ctx;
        }

        public TestContext Context { get; }

        // Repo assembly versions are Determined by the `Ubiquity.NET.Versioning.Build.Tasks`.
        // This verifies the build versioning from that task agrees with what this library would produce.
        // This should be identical for IDE builds AND command line builds. Basically this verifies the
        // manual IDE properties as well as the automated build scripting matches what the task itself
        // produces. If anything is out of whack, this will complain.
        [TestMethod]
        public void ValidateRepoAssemblyVersion( )
        {
            // NOTE: Concept of a CI build used here (and in CSemVer[-CI]) is anything that is NOT
            //       a release build. It does not distinguish between local, PR, or "nightly" etc...
            //       That information is lost in creation of the FileVersionQuad form anyway and
            //       only a single bit represents a CSemVer or CSemVer-CI.
            BuildKind buildKind = GetBuildKind();
            bool isCIBuild = buildKind != BuildKind.ReleaseBuild;

            // Build the expected quad from the information in the repo XML file combined
            // with the CI build status, which is NOT part of the static XML file
            var versionXmlInfo = ParsedBuildVersionXml.ParseFile(Path.Join(Context.GetRepoRoot(), "BuildVersion.xml"));
            var baseVer = CSemVer.From(versionXmlInfo, []);

            // NOTE: Quad does not need the baseBuild.Patch+1 technique as a FileVersion
            // includes an LSB that indicates a CI build (ODD numbered revision is CI). Therefore,
            // it already accounts for the correct ordering.
            var expectedQuad = new FileVersionQuad(baseVer.OrderedVersion, isCIBuild);

            Assembly asm = typeof(SemVer).Assembly;

            // get attributes from the assembly under tests and report it as an aid in diagnosing any failures
            string fileVersionString = GetAssemblyAttribute( asm, "System.Reflection.AssemblyFileVersionAttribute" );
            string informationalVersionString = GetAssemblyAttribute( asm, "System.Reflection.AssemblyInformationalVersionAttribute");
            Context.WriteLine( "FileVersion: {0}", fileVersionString );
            Context.WriteLine( "InformationalVersion: {0}", informationalVersionString );

            // Test that AssemblyFileVersion on the assembly under test matches what would be
            // produced by the assembly
            var actualFileQuad = FileVersionQuad.Parse(fileVersionString, null);
            Assert.AreEqual( expectedQuad, actualFileQuad );
            Assert.AreEqual( isCIBuild, actualFileQuad.IsCiBuild, "CI status should match expectations" );

            // Test that AssemblyInformationalVersion on the assembly under test matches what would be
            // produced by the assembly, it's a FULL version string SemVer/CSemVer/CSemVer-CI
            // Assembly uses CSemVer so it is always case insensitive.
            var actualVersion = SemVer.Parse(informationalVersionString, SemVerFormatProvider.CaseInsensitive);
            Assert.AreEqual( isCIBuild, actualVersion is CSemVerCI, "CI status should match expectations" );

            if(isCIBuild)
            {
                var actualCI = (CSemVerCI)actualVersion;

                // This should create a version with baseBuild.PatchBuild + 1;
                // For testing, the actual index is ignored.
                var expectedVersion = new CSemVerCI(baseVer, "1234", GetBuildNameForKind(buildKind));

                // This tests the behavior of this library to ensure it is correct.
                Assert.IsTrue( expectedVersion > baseVer, "CI builds should order > than base version" );

                // This is where the Patch+1 behavior is validated against the task
                // Both this library and the generated version for this library (from the task)
                // should agree on the build and base build. Bug: https://github.com/UbiquityDotNET/CSemVer.GitBuild/issues/64
                // on the task deals with the incorrect assumption that baseBuild == build and
                // generally ignores the Patch+1 requirement.
                Assert.AreEqual( expectedVersion.Major, actualCI.Major );
                Assert.AreEqual( expectedVersion.Minor, actualCI.Minor );
                Assert.AreEqual( expectedVersion.Patch, actualCI.Patch ); // Until the task is fixed this is expected to fail

                Assert.AreEqual( expectedVersion.Name, actualCI.Name);

                // NOTE: BuildTime is an env var that is set by the build scripts. It has no value in an
                // IDE build and isn't present for test runs either. This test isn't concerned with the exact
                // CI build index/name only that it is a CI build AND the base version is legit
                // as those are the most confusing and easiest to get wrong.
                // Assert.AreEqual( expectedVersion.Index, actualCI.Index);

                Assert.AreEqual( expectedVersion.PreRelease.Length, actualCI.PreRelease.Length, "prerelease sequence should have matching element count" );
            }
            else
            {
                var expectedVersion = baseVer;
                Assert.AreEqual( expectedVersion, actualVersion);
            }
        }

        private static string GetAssemblyAttribute( Assembly asm, string name )
        {
            var asmAttrQuery = from attr in asm.CustomAttributes
                               where attr.AttributeType.FullName == name
                               let val = attr.ConstructorArguments.Single().Value as string
                               where val is not null
                               select val;

            return asmAttrQuery.First();
        }

        private static BuildKind GetBuildKind( )
        {
            BuildKind currentBuildKind = BuildKind.LocalBuild;
            bool isAutomatedBuild = GetEnvBool("CI")
                                 || GetEnvBool("APPVEYOR")
                                 || GetEnvBool("GITHUB_ACTIONS");

            bool isReleaseBuild = false;
            if(isAutomatedBuild)
            {
                // PR and release builds have externally detected indicators that are tested
                // below, so default to a CiBuild (e.g. not a PR, And not a RELEASE)
                currentBuildKind = BuildKind.CiBuild;

                //IsPullRequestBuild indicates an automated buddy build and should not be trusted
                bool isPullRequestBuild = HasEnvVar("GITHUB_BASE_REF")
                                       || HasEnvVar("APPVEYOR_PULL_REQUEST_NUMBER");

                if(isPullRequestBuild)
                {
                    currentBuildKind = BuildKind.PullRequestBuild;
                }
                else
                {
                    if(GetEnvBool( "APPVEYOR" ))
                    {
                        isReleaseBuild = HasEnvVar( "APPVEYOR_REPO_TAG" );
                    }
                    else if(GetEnvBool( "GITHUB_ACTIONS" ))
                    {
                        string? githubRef = Environment.GetEnvironmentVariable("GITHUB_REF");

                        isReleaseBuild = !string.IsNullOrWhiteSpace( githubRef ) && githubRef.Contains( @"refs/tags/", StringComparison.Ordinal );
                    }

                    if(isReleaseBuild)
                    {
                        currentBuildKind = BuildKind.ReleaseBuild;
                    }
                }
            }

            return currentBuildKind;
        }

        private static bool GetEnvBool( string varName )
        {
            string? value = Environment.GetEnvironmentVariable(varName);
            return !string.IsNullOrWhiteSpace( value )
                 && Convert.ToBoolean( value, CultureInfo.InvariantCulture );
        }

        private static bool HasEnvVar( string name )
        {
            return !string.IsNullOrWhiteSpace( Environment.GetEnvironmentVariable( name ) );
        }

        private static string GetBuildNameForKind(BuildKind kind)
        {
            return kind switch
            {
            BuildKind.LocalBuild => "ZZZ",
            BuildKind.PullRequestBuild => "PRQ",
            BuildKind.CiBuild => "BLD",
            BuildKind.ReleaseBuild => string.Empty,
            _ => throw new InvalidEnumArgumentException(nameof(kind), (int)kind, typeof(BuildKind))
            };
        }

        private enum BuildKind
        {
            LocalBuild,
            PullRequestBuild,
            CiBuild,
            ReleaseBuild,
        }
    }
}
