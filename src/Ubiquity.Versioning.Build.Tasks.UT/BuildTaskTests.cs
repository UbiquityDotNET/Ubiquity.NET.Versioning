// -----------------------------------------------------------------------
// <copyright file="BuildTaskTests.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using System.Xml.Linq;

using Microsoft.Build.Definition;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Utilities.ProjectCreation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ubiquity.Versioning.Build.Tasks.UT
{
    [TestClass]
    public class BuildTaskTests
        : MSBuildTestBase
    {
        public BuildTaskTests( TestContext ctx )
        {
            ArgumentNullException.ThrowIfNull(ctx);
            ArgumentException.ThrowIfNullOrWhiteSpace( ctx.TestResultsDirectory );

            Context = ctx;
        }

        public TestContext Context { get; }

        // Repo assembly versions are defined via PowerShell (Or hard coded in the project file
        // for IDE builds) as the build task is not usable for itself. This verifies the build
        // versioning used in the scripts matches what is expected for an end-consumer by testing
        // the assemblies versioning information against the output from THAT task assembly. This
        // should be identical for IDE builds AND command line builds. Basically this verifies the
        // manual IDE properties as well as the automated build scripting matches what the task
        // itself produces. If anything is out of whack, this will complain.
        [TestMethod]
        [DataRow("netstandard2.0")]
        [DataRow("net48")]
        [DataRow("net8.0")]
        public void ValidateRepoAssemblyVersion( string targetFramework)
        {
            var globalProperties = new Dictionary<string,string>
            {
                ["BuildVersionXml"] = Path.Combine(RepoRoot, "BuildVersion.xml"),
            };

            // For a CI build load the build time and ciBuild name from the generatedversion.props file
            // so the test knows what to expect. This verifies that the task creation of versions agrees
            // with how the build scripts created the version information. (Not the task in general)
            // That is, this test case is NOT free of the build environment and MUST take it into consideration
            // to properly verify the build scripts are doing things correctly. The CI Index is created from
            // the time stamp and the name is either provided directly as a build property or computed in the
            // Ubiquity.NET.Versioning.Build.Tasks.props file.
            var (buildTime, ciBuildName) = GetGeneratedCiBuildInfo();
            if(!string.IsNullOrWhiteSpace(ciBuildName))
            {
                globalProperties["CiBuildName"] = ciBuildName;
            }

            if(!string.IsNullOrWhiteSpace(buildTime))
            {
                globalProperties["BuildTime"] = buildTime;
            }

            using var collection = new ProjectCollection(globalProperties);

            var (testProject, props) = CreateTestProjectAndInvokeTestedPackage(
                targetFramework,
                projectCollection: collection
            );

            string? taskAssembly = testProject.ProjectInstance.GetOptionalProperty("_Ubiquity_NET_Versioning_Build_Tasks");
            Assert.IsNotNull( taskAssembly, "Task assembly property should contain full path to the task DLL (Not NULL)" );
            Context.WriteLine( $"Task Assembly: '{taskAssembly}'" );

            Assert.IsFalse( string.IsNullOrWhiteSpace( taskAssembly ), "Task assembly property should contain full path to the task DLL (Not Whitespace)" );
            Assert.IsNotNull( props.FileVersion, "Generated properties should have a 'FileVersion'" );
            Context.WriteLine( $"Generated FileVersion: {props.FileVersion}" );

            var alc = new AssemblyLoadContext("TestALC", isCollectible: true);
            try
            {
                var asm = alc.LoadFromAssemblyPath(taskAssembly);
                Assert.IsNotNull( asm, "should be able to load task assembly" );
                var asmName = asm.GetName();
                Version? asmVer = asmName.Version;
                Assert.IsNotNull( asmVer, "Task assembly should have a version" );
                Context.WriteLine( $"TaskAssemblyVersion: {asmVer}" );
                Context.WriteLine( $"AssemblyName: {asmName}" );

                Assert.IsNotNull( props.FileVersionMajor, "Property value for Major should exist" );
                Assert.AreEqual( (int)props.FileVersionMajor, asmVer.Major, "Major value of assembly version should match" );

                Assert.IsNotNull( props.FileVersionMinor, "Property value for Minor should exist" );
                Assert.AreEqual( (int)props.FileVersionMinor, asmVer.Minor, "Minor value of assembly version should match" );

                Assert.IsNotNull( props.FileVersionBuild, "Property value for Build should exist" );
                Assert.AreEqual( (int)props.FileVersionBuild, asmVer.Build, "Build value of assembly version should match" );

                Assert.IsNotNull( props.FileVersionRevision, "Property value for Revision should exist" );
                Assert.AreEqual( (int)props.FileVersionRevision, asmVer.Revision, "Revision value of assembly version should match" );

                // Test that AssemblyFileVersion attribute matches expected value
                string fileVersion = ( from attr in asm.CustomAttributes
                                       where attr.AttributeType.FullName == "System.Reflection.AssemblyFileVersionAttribute"
                                       let val = attr.ConstructorArguments.Single().Value as string
                                       where val is not null
                                       select val
                                     ).Single();

                Assert.AreEqual(props.FileVersion, fileVersion);

                // Test that AssemblyInformationalVersion attribute matches expected value
                string informationalVersion = ( from attr in asm.CustomAttributes
                                                where attr.AttributeType.FullName == "System.Reflection.AssemblyInformationalVersionAttribute"
                                                let val = attr.ConstructorArguments.Single().Value as string
                                                where val is not null
                                                select val
                                              ).Single();

                Assert.AreEqual(props.InformationalVersion, informationalVersion);
            }
            finally
            {
                alc.Unload();
            }
        }

        [TestMethod]
        [DataRow("netstandard2.0")]
        [DataRow("net48")]
        [DataRow("net8.0")]
        public void GoldenPathTest( string targetFramework )
        {
            var globalProperties = new Dictionary<string,string>
            {
                ["BuildMajor"] = "20",
                ["BuildMinor"] = "1",
                ["BuildPatch"] = "4",
                ["PreReleaseName"] = "alpha",
            };

            using var collection = new ProjectCollection(globalProperties);
            var (testProject, props) = CreateTestProjectAndInvokeTestedPackage(
                targetFramework,
                projectCollection: collection
            );

            // v20.1.4-alpha => 5.44854.3875.59946 [see: https://csemver.org/playground/site/#/]
            // NOTE: CI build is +1 (FileVersionRevision)!
            string expectedFullBuildNumber = $"20.1.4-alpha.ci.{props.CiBuildIndex}.{props.CiBuildName}";
            string expectedShortNumber = $"20.1.4-a.0.0.ci.{props.CiBuildIndex}.{props.CiBuildName}";
            string expectedFileVersion = "5.44854.3875.59947";

            Assert.IsNotNull( props.BuildMajor, "should have a value set for 'BuildMajor'" );
            Assert.AreEqual( 20u, props.BuildMajor.Value );

            Assert.IsNotNull( props.BuildMinor, "should have a value set for 'BuildMinor'" );
            Assert.AreEqual( 1u, props.BuildMinor.Value );

            Assert.IsNotNull( props.BuildPatch, "should have a value set for 'BuildPatch'" );
            Assert.AreEqual( 4, props.BuildPatch.Value );

            Assert.IsNotNull( props.PreReleaseName, "should have a value set for 'PreReleaseName'" );
            Assert.AreEqual( "alpha", props.PreReleaseName );

            Assert.IsNull( props.PreReleaseNumber, "Should NOT have a value set for 'PreReleaseNumber'" );
            Assert.IsNull( props.PreReleaseFix, "Should NOT have a value set for 'PreReleaseNumber'" );

            // NOTE: Since build index is based on time which is captured during build it
            // is not possible to know 'a priori' what the value will be... Other, tests
            // validate the behavior of that with an explicit setting...
            Assert.AreEqual( expectedFullBuildNumber, props.FullBuildNumber );
            Assert.AreEqual( expectedShortNumber, props.PackageVersion );

            // TODO: Test that time is in ISO-8601 format and within a few seconds of "now"
            // For now, just make sure they aren't null or empty
            Assert.IsFalse( string.IsNullOrWhiteSpace( props.BuildTime ) );
            Assert.IsFalse( string.IsNullOrWhiteSpace( props.CiBuildIndex ) );

            Assert.AreEqual( "ZZZ", props.CiBuildName );

            Assert.IsNotNull( props.FileVersionMajor );
            Assert.AreEqual( 5, props.FileVersionMajor.Value );

            Assert.IsNotNull( props.FileVersionMinor );
            Assert.AreEqual( 44854, props.FileVersionMinor.Value );

            Assert.IsNotNull( props.FileVersionBuild );
            Assert.AreEqual( 3875, props.FileVersionBuild.Value );

            Assert.IsNotNull( props.FileVersionRevision );
            Assert.AreEqual( 59947, props.FileVersionRevision.Value );

            Assert.AreEqual( expectedFileVersion, props.FileVersion );
            Assert.AreEqual( expectedFileVersion, props.AssemblyVersion );
            Assert.AreEqual( expectedFullBuildNumber, props.InformationalVersion );
        }

        [TestMethod]
        [DataRow("netstandard2.0")]
        [DataRow("net48")]
        [DataRow("net8.0")]
        public void BuildVersionXmlIsUsed( string targetFramework)
        {
            string buildVersionXml = CreateBuildVersionXml(20, 1, 5);
            string buildTime = DateTime.UtcNow.ToString( "o" );
            const string buildIndex = "ABCDEF12";
            var globalProperties = new Dictionary<string, string>
            {
                ["BuildTime"] = buildTime,
                ["CiBuildIndex"] = buildIndex,
                ["BuildVersionXml"] = buildVersionXml
            };

            using var collection = new ProjectCollection(globalProperties);

            var (testProject, props) = CreateTestProjectAndInvokeTestedPackage(
                targetFramework,
                projectCollection: collection
            );

            // v20.1.5 => 5.44854.3880.52268 [see: https://csemver.org/playground/site/#/]
            // NOTE: CI build is +1 (FileVersionRevision)!
            string expectedFullBuildNumber = $"20.1.5--ci.ABCDEF12.ZZZ";
            string expectedShortNumber = $"20.1.5--ci.ABCDEF12.ZZZ";
            string expectedFileVersion = "5.44854.3880.52269";

            Assert.IsNotNull( props.BuildMajor );
            Assert.AreEqual( 20u, props.BuildMajor.Value );

            Assert.IsNotNull( props.BuildMinor );
            Assert.AreEqual( 1u, props.BuildMinor.Value );

            Assert.IsNotNull( props.BuildPatch );
            Assert.AreEqual( 5u, props.BuildPatch.Value );

            Assert.IsNull( props.PreReleaseName );

            Assert.IsNotNull( props.PreReleaseNumber );
            Assert.AreEqual( 0, props.PreReleaseNumber.Value );

            Assert.IsNotNull( props.PreReleaseFix );
            Assert.AreEqual( 0, props.PreReleaseFix.Value );

            Assert.AreEqual( expectedFullBuildNumber, props.FullBuildNumber );
            Assert.AreEqual( expectedShortNumber, props.PackageVersion );

            // Test for expected global properties (Should not change values)
            Assert.AreEqual( buildTime, props.BuildTime );
            Assert.AreEqual( buildIndex, props.CiBuildIndex );

            Assert.AreEqual( "ZZZ", props.CiBuildName );

            Assert.IsNotNull( props.FileVersionMajor );
            Assert.AreEqual( 5, props.FileVersionMajor.Value );

            Assert.IsNotNull( props.FileVersionMinor );
            Assert.AreEqual( 44854, props.FileVersionMinor.Value );

            Assert.IsNotNull( props.FileVersionBuild );
            Assert.AreEqual( 3880, props.FileVersionBuild.Value );

            Assert.IsNotNull( props.FileVersionRevision );
            Assert.AreEqual( 52269, props.FileVersionRevision.Value );

            Assert.AreEqual( expectedFileVersion, props.FileVersion );
            Assert.AreEqual( expectedFileVersion, props.AssemblyVersion );
            Assert.AreEqual( expectedFullBuildNumber, props.InformationalVersion );
        }

        internal string RepoRoot => !string.IsNullOrWhiteSpace(Context.TestRunDirectory)
                                    ? Path.GetFullPath( Path.Combine( Context.TestRunDirectory, "..", "..", ".." ) )
                                    : throw new InvalidOperationException("Context.TestRunDirectory is not available");

        private (ProjectCreator Project, BuildProperties ResultProps) CreateTestProjectAndInvokeTestedPackage(
            string targetFramework,
            Action<ProjectCreator>? action = null,
            ProjectCollection? projectCollection = null
            )
        {
            var (testProject, resolveResult) = CreateAndResolveTestProject( targetFramework, action, projectCollection );
            Assert.IsTrue( resolveResult );

            // Since this project uses an imported target, it won't even exist until AFTER ResolvePackageDependencies[DesignTime|ForBuild] comes along.
            var (result, output) = testProject.ProjectInstance.Build("PrepareVersioningForBuild");
            Assert.IsNotNull( result );
            Assert.IsNotNull( result.ProjectStateAfterBuild );

            return (testProject, new BuildProperties( result ));
        }

        private (ProjectCreator Project, bool ResolveResult) CreateAndResolveTestProject(
            string targetFramework,
            Action<ProjectCreator>? action = null,
            ProjectCollection? projectCollection = null
            )
        {
            if (string.IsNullOrWhiteSpace(Context.TestResultsDirectory))
            {
                throw new InvalidOperationException("TestResultsDirectory is not available!");
            }

            if (string.IsNullOrWhiteSpace(Context.TestName))
            {
                throw new InvalidOperationException("TestName is not available!");
            }

            string projectPath = Path.Combine( Context.TestResultsDirectory, $"{nameof(BuildTaskTests)}-{Context.TestName}-{targetFramework}.csproj");
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
                LogBuildErrors( resolveBuildOutput );
            }

            return (project, resolveResult);
        }

        private void LogBuildErrors( BuildOutput buildOutput )
        {
            foreach(var err in buildOutput.ErrorEvents)
            {
                // Log build errors in standard MSBuild format
                // see: https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-diagnostic-format-for-tasks?view=vs-2022
                Context.WriteLine( $"{err.ProjectFile}({err.LineNumber},{err.ColumnNumber}) : {err.Subcategory} error {err.Code} : {err.Message}" );
            }
        }

        private string CreateBuildVersionXml(
            int buildMajor,
            int buildMinor,
            int buildPatch,
            string? preReleaseName = null,
            int? preReleaseNumber = null,
            int? preReleaseFix = null
            )
        {
            Assert.IsNotNull( Context.DeploymentDirectory );
            string retVal = Path.Combine(Context.DeploymentDirectory, Path.GetRandomFileName());
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
            Context.WriteLine( $"BuildVersionXML written to: '{retVal}'" );
            return retVal;
        }

        private static (string BuildTime, string CiBuildName) GetGeneratedCiBuildInfo( )
        {
            using var dummyCollection = new ProjectCollection();
            var options = new ProjectOptions()
            {
                ProjectCollection = dummyCollection
            };

            var project = Project.FromFile(Path.Combine(TestModuleFixtures.RepoRoot, "GeneratedVersion.props"), options);
            return (project.GetPropertyValue("BuildTime"), project.GetPropertyValue("CiBuildName"));
        }
    }
}
