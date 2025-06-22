// -----------------------------------------------------------------------
// <copyright file="CSemVerCITests.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ubiquity.NET.Versioning.UT
{
    [TestClass]
    public class CSemVerCITests
    {
        [TestMethod]
        [TestCategory("Constructor")]
        public void CSemVerCITest( )
        {
            var preRelInfo = new PrereleaseVersion(1, 2, 3);
            var ver = new CSemVerCI( new CSemVer(7, 8, 9, preRelInfo, ["meta-man"]), "c-index", "c-name");

            // NOTE: "beta" => 1
            string[] expctedPreReleaseSeq = ["beta", "2", "3", "ci", "c-index", "c-name"];
            Assert.AreEqual( 7, ver.Major );
            Assert.AreEqual( 8, ver.Minor );
            Assert.AreEqual( 9, ver.Patch, "While CI builds should use a Patch+1 that's not verifiable" );
            Assert.AreEqual( preRelInfo, ver.PrereleaseVersion );
            Assert.IsTrue( expctedPreReleaseSeq.SequenceEqual( ver.PreRelease ) );
            Assert.AreEqual( "c-index", ver.Index );
            Assert.AreEqual( "c-name", ver.Name );
        }

        [TestMethod]
        [TestCategory("Constructor")]
        public void CSemVer_construction_with_pre_release_zero_timed_base_throws( )
        {
            var preRelInfo = new PrereleaseVersion(1, 2, 3);
            var zeorPrereleaseVer = new CSemVer(0, 0, 0, preRelInfo, ["meta-man"]);
            var ex = Assert.ThrowsExactly<ArgumentException>(()=>_ = new CSemVerCI( zeorPrereleaseVer, "c-index", "c-name") );
            Assert.AreEqual("baseBuild", ex.ParamName);
        }

        [TestMethod]
        [TestCategory("Constructor")]
        public void CSemVer_construction_with_bogus_args_throws( )
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            // VALIDATING behavior of API is the point of this test.
            var argnex = Assert.ThrowsExactly<ArgumentNullException>(()=>_=new CSemVerCI((string?)null, "c-name", null));
            Assert.AreEqual("index", argnex.ParamName, "null parameter should throw"); // sadly there is no refactoring safe nameof() expression for a parameter at the call site...
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

            var argex = Assert.ThrowsExactly<ArgumentException>(()=>_=new CSemVerCI(string.Empty, "c-name", null));
            Assert.AreEqual("index", argex.ParamName, "empty string should throw");

            argex = Assert.ThrowsExactly<ArgumentException>(()=>_=new CSemVerCI(" \r\n\t", "c-name", null));
            Assert.AreEqual("index", argex.ParamName, "all whitespace string should throw");

            argex = Assert.ThrowsExactly<ArgumentException>(()=>_=new CSemVerCI("invalid#id","c-name", null));
            Assert.AreEqual("index", argex.ParamName, "invalid index pattern should throw");

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            // VALIDATING behavior of API is the point of this test.
            argnex = Assert.ThrowsExactly<ArgumentNullException>(()=>_=new CSemVerCI("c-index", null, null));
            Assert.AreEqual("name", argnex.ParamName, "null parameter should throw"); // sadly there is no refactoring safe nameof() expression for a parameter at the call site...
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

            argex = Assert.ThrowsExactly<ArgumentException>(()=>_=new CSemVerCI("c-index", string.Empty, null));
            Assert.AreEqual("name", argex.ParamName, "empty string should throw");

            argex = Assert.ThrowsExactly<ArgumentException>(()=>_=new CSemVerCI("c-index", " \r\n\t", null));
            Assert.AreEqual("name", argex.ParamName, "all whitespace string should throw");

            argex = Assert.ThrowsExactly<ArgumentException>(()=>_=new CSemVerCI("c-index", "invalid#id", null));
            Assert.AreEqual("name", argex.ParamName, "invalid name pattern should throw");
        }

        [TestMethod]
        [TestCategory("Constructor")]
        public void CSemVerCITest1( )
        {
            // ZeroTime based constructor
            string[] expectedBuildMeta = ["meta-man"];
            var ver = new CSemVerCI( "c-index", "c-name", expectedBuildMeta);

            string[] expctedPreReleaseSeq = ["-ci", "c-index", "c-name"];
            Assert.AreEqual( 0, ver.Major );
            Assert.AreEqual( 0, ver.Minor );
            Assert.AreEqual( 0, ver.Patch );
            Assert.IsNull( ver.PrereleaseVersion );
            Assert.IsTrue( expctedPreReleaseSeq.SequenceEqual( ver.PreRelease ) );
            Assert.AreEqual( "c-index", ver.Index );
            Assert.AreEqual( "c-name", ver.Name );
            Assert.IsTrue(expectedBuildMeta.SequenceEqual(ver.BuildMeta));
        }

        [TestMethod]
        [TestCategory("Constructor")]
        [TestCategory("AppContext Switch")]
        public void CSemVerCI_with_BuildMeta_and_switch_enabled_throws( )
        {
            // Temporarily enable the switch to validate behavior
            Assert.IsFalse(AppContextSwitches.CSemVerCIOnlySupportsBuildMetaOnZeroTimedVersions, "Default switch state should be OFF");
            using(var x = AutoRestoreAppContextSwitch.Configure(AppContextSwitches.CSemVerCIOnlySupportsBuildMetaOnZeroTimedVersionsName, true))
            {
                Assert.IsTrue(AppContextSwitches.CSemVerCIOnlySupportsBuildMetaOnZeroTimedVersions, "Switch should be ON now");

                var baseVerWithMetaData = new CSemVer( 20, 1, 4, null, [ "buildMeta" ] );
                var ex = Assert.ThrowsExactly<ArgumentException>(()=>new CSemVerCI( baseVerWithMetaData, "c-index", "c-name"));
                Assert.AreEqual("baseBuild", ex.ParamName);
            }

            Assert.IsFalse(AppContextSwitches.CSemVerCIOnlySupportsBuildMetaOnZeroTimedVersions, "switch state should be restored to OFF");
        }

        [TestMethod]
        [TestCategory("ToString")]
        public void ToStringTests( )
        {
            // Validate ToString() P=0; CI=1
            const string testIndex = "BuildIndex";
            const string testName = "BuildName";

            // Verifies distinct syntax for a release build["double dash"] (vs. pre-release [no special leading])
            var ver_no_prerel = new CSemVerCI( new CSemVer( 20, 1, 4, null, [ "buildMeta" ] ), testIndex, testName );
            Assert.AreEqual("20.1.4--ci.BuildIndex.BuildName+buildMeta", ver_no_prerel.ToString());

            // Validate ToString() P=1; CI=1
            var alpha_0_0 = new PrereleaseVersion(0, 0, 0);
            var beta_1_0 = new PrereleaseVersion(1, 1, 0);
            var delta_0_1 = new PrereleaseVersion(2, 0, 1);

            var ver_alpha = new CSemVerCI( new CSemVer(20, 1, 4, alpha_0_0, ["buildMeta"]), testIndex, testName);
            var ver_beta = new CSemVerCI( new CSemVer(20, 1, 4, beta_1_0, ["buildMeta"]), testIndex, testName);
            var ver_delta = new CSemVerCI( new CSemVer(20, 1, 4, delta_0_1, ["buildMeta"]), testIndex, testName);

            Assert.AreEqual("20.1.4-alpha.ci.BuildIndex.BuildName+buildMeta", ver_alpha.ToString());
            Assert.AreEqual("20.1.4-beta.1.ci.BuildIndex.BuildName+buildMeta", ver_beta.ToString());
            Assert.AreEqual("20.1.4-delta.0.1.ci.BuildIndex.BuildName+buildMeta", ver_delta.ToString());
        }

        [TestMethod]
        [TestCategory("Ordering")]
        public void CompareToTestsNoPrelNoMeta( )
        {
            var ver_name_1 = new CSemVerCI( new CSemVer( 20, 1, 4 ), "BuildIndex01", "BuildName01" );
            var ver_name_1_same = new CSemVerCI( new CSemVer( 20, 1, 4 ), "BuildIndex01", "BuildName01" );
            var ver_name_2 = new CSemVerCI( new CSemVer( 20, 1, 4 ), "BuildIndex01", "BuildName02" );

            Assert.IsTrue(ver_name_1.CompareTo(null) > 0, "Any instance should compare greater than null");

            Assert.IsTrue(ver_name_1.CompareTo(ver_name_2) < 0);
            Assert.IsTrue(ver_name_2.CompareTo(ver_name_1) > 0);

            Assert.AreEqual(0, ver_name_1.CompareTo( ver_name_1_same ));
            Assert.AreEqual(0, ver_name_1_same.CompareTo( ver_name_1 ));
            Assert.AreNotEqual(0, ver_name_1.CompareTo( ver_name_2 ));
        }

        [TestMethod]
        [TestCategory("Ordering")]
        public void CompareToTestsNoPrelWithMeta( )
        {
            var ver_name_1 = new CSemVerCI( new CSemVer( 20, 1, 4, null, [ "buildMeta" ] ), "BuildIndex01", "BuildName01" );
            var ver_name_1_same = new CSemVerCI( new CSemVer( 20, 1, 4, null, [ "buildMeta" ] ), "BuildIndex01", "BuildName01" );
            var ver_name_2 = new CSemVerCI( new CSemVer( 20, 1, 4, null, [ "buildMeta" ] ), "BuildIndex01", "BuildName02" );
            Assert.IsTrue(ver_name_1.CompareTo(null) > 0, "Any instance should compare greater than null");

            Assert.IsTrue(ver_name_1.CompareTo(ver_name_2) < 0);
            Assert.IsTrue(ver_name_2.CompareTo(ver_name_1) > 0);

            Assert.AreEqual(0, ver_name_1.CompareTo( ver_name_1_same ));
            Assert.AreEqual(0, ver_name_1_same.CompareTo( ver_name_1 ));
            Assert.AreNotEqual(0, ver_name_1.CompareTo( ver_name_2 ));
        }

        [TestMethod]
        [TestCategory("Ordering")]
        public void CompareToTestsWithPrelNoMeta( )
        {
            var alpha_0_1 = new PrereleaseVersion(0, 0, 1);
            var ver_name_1 = new CSemVerCI( new CSemVer( 1, 2, 3, alpha_0_1 ), "BuildIndex01", "BuildName01" );
            var ver_name_1_same = new CSemVerCI( new CSemVer( 1, 2, 3, alpha_0_1 ), "BuildIndex01", "BuildName01" );
            var ver_name_2 = new CSemVerCI( new CSemVer( 1, 2, 3, alpha_0_1 ), "BuildIndex01", "BuildName02" );
            Assert.IsTrue(ver_name_1.CompareTo(null) > 0, "Any instance should compare greater than null");

            Assert.IsTrue(ver_name_1.CompareTo(ver_name_2) < 0);
            Assert.IsTrue(ver_name_2.CompareTo(ver_name_1) > 0);

            Assert.AreEqual(0, ver_name_1.CompareTo( ver_name_1_same ));
            Assert.AreEqual(0, ver_name_1_same.CompareTo( ver_name_1 ));
            Assert.AreNotEqual(0, ver_name_1.CompareTo( ver_name_2 ));
        }

        [TestMethod]
        [TestCategory("Ordering")]
        public void CompareToTestsWithPrelWithMeta( )
        {
            // Build meta should have no impact on ordering

            var alpha_0_1 = new PrereleaseVersion(0, 0, 1);
            var ver_name_1 = new CSemVerCI( new CSemVer( 1, 2, 3, alpha_0_1, ["BuildMeta"] ), "BuildIndex01", "BuildName01" );
            var ver_name_1_same = new CSemVerCI( new CSemVer( 1, 2, 3, alpha_0_1, ["BuildMeta", "MoreMeta"] ), "BuildIndex01", "BuildName01" );
            var ver_name_2 = new CSemVerCI( new CSemVer( 1, 2, 3, alpha_0_1, ["SomeOhterMeta"] ), "BuildIndex01", "BuildName02" );
            Assert.IsTrue(ver_name_1.CompareTo(null) > 0, "Any instance should compare greater than null");

            Assert.IsTrue(ver_name_1.CompareTo(ver_name_2) < 0);
            Assert.IsTrue(ver_name_2.CompareTo(ver_name_1) > 0);

            Assert.AreEqual(0, ver_name_1.CompareTo( ver_name_1_same ));
            Assert.AreEqual(0, ver_name_1_same.CompareTo( ver_name_1 ));
            Assert.AreNotEqual(0, ver_name_1.CompareTo( ver_name_2 ));
        }
    }
}
