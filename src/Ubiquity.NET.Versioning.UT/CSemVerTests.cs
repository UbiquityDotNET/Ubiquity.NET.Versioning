// -----------------------------------------------------------------------
// <copyright file="CSemVerTests.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ubiquity.NET.Versioning.UT
{
    [TestClass]
    public class CSemVerTests
    {
        [TestMethod]
        public void CSemVerTest( )
        {
            var ver = new CSemVer(1,2,3);
            Assert.AreEqual( 1, ver.Major );
            Assert.AreEqual( 2, ver.Minor );
            Assert.AreEqual( 3, ver.Patch );
            Assert.IsFalse( ver.IsPrerelease );
            Assert.IsFalse( ver.IsCIBuild );
            Assert.IsFalse( ver.PrereleaseVersion.IsValid);
            Assert.IsFalse( ver.CiBuildInfo.IsValid);

            var preRelInfo = new PrereleaseVersion(1, 2, 3);
            ver = new CSemVer( 4, 5, 6, preRelInfo, default, "buildMeta" );
            Assert.AreEqual( 4, ver.Major );
            Assert.AreEqual( 5, ver.Minor );
            Assert.AreEqual( 6, ver.Patch );
            Assert.IsTrue( ver.IsPrerelease );
            Assert.IsFalse( ver.IsCIBuild );
            Assert.IsTrue( ver.PrereleaseVersion.IsValid );
            Assert.IsFalse( ver.CiBuildInfo.IsValid );

            var ciBuildInfo = new CiBuildInfo("ci-index", "ci-name");
            ver = new CSemVer( 7, 8, 9, preRelInfo, ciBuildInfo, "meta-man" );
            Assert.AreEqual( 7, ver.Major );
            Assert.AreEqual( 8, ver.Minor );
            Assert.AreEqual( 9, ver.Patch );
            Assert.IsTrue( ver.IsPrerelease );
            Assert.IsTrue( ver.IsCIBuild );
            Assert.IsTrue( ver.PrereleaseVersion.IsValid );
            Assert.IsTrue( ver.CiBuildInfo.IsValid );
            Assert.AreEqual( "ci-index", ver.CiBuildInfo.BuildIndex);
            Assert.AreEqual( "ci-name", ver.CiBuildInfo.BuildName);
        }

        [TestMethod]
        public void ToStringTest( )
        {
            // Validate ToString("bogus") throws...
            var ver = new CSemVer(1,2,3);
            var argex = Assert.ThrowsExactly<ArgumentException>(()=> _ = ver.ToString("bogus", null));
            Assert.AreEqual("format", argex.ParamName);

            var alpha_0_0 = new PrereleaseVersion(0, 0, 0);
            var beta_1_0 = new PrereleaseVersion(1, 1, 0);
            var delta_0_1 = new PrereleaseVersion(2, 0, 1);

            var ciInfo = new CiBuildInfo("BuildIndex", "BuildName");

            // Validate ToString(null, null); // same as ToString("M")
            Assert.AreEqual("20.1.4+buildMeta", new CSemVer(20, 1, 4, default, default, "buildMeta").ToString(null, null));
            Assert.AreEqual("20.1.4+buildMeta", new CSemVer(20, 1, 4, default, default, "buildMeta").ToString("M", null));
            Assert.AreEqual("20.1.4+buildMeta", new CSemVer(20, 1, 4, default, default, "buildMeta").ToString());

            // Validate ToString("M") P=0; CI=0
            Assert.AreEqual("20.1.4+buildMeta", new CSemVer(20, 1, 4, default, default, "buildMeta").ToString("M", null));

            // Validate ToString("M") P=0; CI=1
            Assert.AreEqual("20.1.4--ci.BuildIndex.BuildName+buildMeta", new CSemVer(20, 1, 4, default, ciInfo, "buildMeta").ToString("M", null));

            // Validate ToString("M") P=1; CI=0
            Assert.AreEqual("20.1.4-alpha+buildMeta", new CSemVer(20, 1, 4, alpha_0_0, default, "buildMeta").ToString("M", null));
            Assert.AreEqual("20.1.4-beta.1+buildMeta", new CSemVer(20, 1, 4, beta_1_0, default, "buildMeta").ToString("M", null));
            Assert.AreEqual("20.1.4-delta.0.1+buildMeta", new CSemVer(20, 1, 4, delta_0_1, default, "buildMeta").ToString("M", null));

            // Validate ToString("M") P=1; CI=1
            Assert.AreEqual("20.1.4-alpha.ci.BuildIndex.BuildName+buildMeta", new CSemVer(20, 1, 4, alpha_0_0, ciInfo, "buildMeta").ToString("M", null));
            Assert.AreEqual("20.1.4-beta.1.ci.BuildIndex.BuildName+buildMeta", new CSemVer(20, 1, 4, beta_1_0, ciInfo, "buildMeta").ToString("M", null));
            Assert.AreEqual("20.1.4-delta.0.1.ci.BuildIndex.BuildName+buildMeta", new CSemVer(20, 1, 4, delta_0_1, ciInfo, "buildMeta").ToString("M", null));

            // Validate ToString("S") P=0; CI=0
            Assert.AreEqual("20.1.4", new CSemVer(20, 1, 4, default, default, "buildMeta").ToString("S", null));

            // Validate ToString("S") P=0; CI=1
            Assert.AreEqual("20.1.4--ci.BuildIndex.BuildName", new CSemVer(20, 1, 4, default, ciInfo, "buildMeta").ToString("S", null));

            // Validate ToString("S") P=1; CI=0
            Assert.AreEqual("20.1.4-a", new CSemVer(20, 1, 4, alpha_0_0, default, "buildMeta").ToString("S", null));
            Assert.AreEqual("20.1.4-b.1", new CSemVer(20, 1, 4, beta_1_0, default, "buildMeta").ToString("S", null));
            Assert.AreEqual("20.1.4-d.0.1", new CSemVer(20, 1, 4, delta_0_1, default, "buildMeta").ToString("S", null));

            // Validate ToString("S") P=1; CI=1
            Assert.AreEqual("20.1.4-a.ci.BuildIndex.BuildName", new CSemVer(20, 1, 4, alpha_0_0, ciInfo, "buildMeta").ToString("S", null));
            Assert.AreEqual("20.1.4-b.1.ci.BuildIndex.BuildName", new CSemVer(20, 1, 4, beta_1_0, ciInfo, "buildMeta").ToString("S", null));
            Assert.AreEqual("20.1.4-d.0.1.ci.BuildIndex.BuildName", new CSemVer(20, 1, 4, delta_0_1, ciInfo, "buildMeta").ToString("S", null));

            // Validate ToString("MS") P=0; CI=0
            Assert.AreEqual("20.1.4+buildMeta", new CSemVer(20, 1, 4, default, default, "buildMeta").ToString("MS", null));

            // Validate ToString("S") P=0; CI=1
            Assert.AreEqual("20.1.4--ci.BuildIndex.BuildName+buildMeta", new CSemVer(20, 1, 4, default, ciInfo, "buildMeta").ToString("MS", null));

            // Validate ToString("S") P=1; CI=0
            Assert.AreEqual("20.1.4-a+buildMeta", new CSemVer(20, 1, 4, alpha_0_0, default, "buildMeta").ToString("MS", null));
            Assert.AreEqual("20.1.4-b.1+buildMeta", new CSemVer(20, 1, 4, beta_1_0, default, "buildMeta").ToString("MS", null));
            Assert.AreEqual("20.1.4-d.0.1+buildMeta", new CSemVer(20, 1, 4, delta_0_1, default, "buildMeta").ToString("MS", null));

            // Validate ToString("S") P=1; CI=1
            Assert.AreEqual("20.1.4-a.ci.BuildIndex.BuildName+buildMeta", new CSemVer(20, 1, 4, alpha_0_0, ciInfo, "buildMeta").ToString("MS", null));
            Assert.AreEqual("20.1.4-b.1.ci.BuildIndex.BuildName+buildMeta", new CSemVer(20, 1, 4, beta_1_0, ciInfo, "buildMeta").ToString("MS", null));
            Assert.AreEqual("20.1.4-d.0.1.ci.BuildIndex.BuildName+buildMeta", new CSemVer(20, 1, 4, delta_0_1, ciInfo, "buildMeta").ToString("MS", null));

            // Validate ToString("SM") P=0; CI=0
            Assert.AreEqual("20.1.4+buildMeta", new CSemVer(20, 1, 4, default, default, "buildMeta").ToString("SM", null));

            // Validate ToString("S") P=0; CI=1
            Assert.AreEqual("20.1.4--ci.BuildIndex.BuildName+buildMeta", new CSemVer(20, 1, 4, default, ciInfo, "buildMeta").ToString("SM", null));

            // Validate ToString("S") P=1; CI=0
            Assert.AreEqual("20.1.4-a+buildMeta", new CSemVer(20, 1, 4, alpha_0_0, default, "buildMeta").ToString("SM", null));
            Assert.AreEqual("20.1.4-b.1+buildMeta", new CSemVer(20, 1, 4, beta_1_0, default, "buildMeta").ToString("SM", null));
            Assert.AreEqual("20.1.4-d.0.1+buildMeta", new CSemVer(20, 1, 4, delta_0_1, default, "buildMeta").ToString("SM", null));

            // Validate ToString("S") P=1; CI=1
            Assert.AreEqual("20.1.4-a.ci.BuildIndex.BuildName+buildMeta", new CSemVer(20, 1, 4, alpha_0_0, ciInfo, "buildMeta").ToString("SM", null));
            Assert.AreEqual("20.1.4-b.1.ci.BuildIndex.BuildName+buildMeta", new CSemVer(20, 1, 4, beta_1_0, ciInfo, "buildMeta").ToString("SM", null));
            Assert.AreEqual("20.1.4-d.0.1.ci.BuildIndex.BuildName+buildMeta", new CSemVer(20, 1, 4, delta_0_1, ciInfo, "buildMeta").ToString("SM", null));
        }

        [TestMethod]
        public void CompareToTest( )
        {
            var valm1 = new CSemVer(1,2,2); // "-1"
            var val = new CSemVer(1,2,3);
            var val2 = new CSemVer(1,2,3);
            var valp1 = new CSemVer(1,2,4); // "+1"
            Assert.IsTrue(val.CompareTo(valm1) > 0, "[CompareTo] val > (val -1)");
            Assert.IsTrue(valm1.CompareTo(val) < 0, "[CompareTo] (val - 1) < val");
            Assert.AreEqual(0, val.CompareTo(val2), "[CompareTo] val == val");
            Assert.IsTrue(val.CompareTo(valp1) < 0, "[CompareTo] val < (val + 1)");
            Assert.IsTrue(valp1.CompareTo(val) > 0, "[CompareTo] (val + 1) > val");

            // Ensure operator variants are correct
            // (They should internally use CompareTo, this verifies correct behavior
            Assert.IsTrue(val > valm1, "[Operator] val > (val -1)");
            Assert.IsTrue(valm1 < val, "[Operator] (val - 1) < val");
            Assert.IsTrue(val == val2, "[Operator] val == val");
            Assert.IsTrue(val < valp1, "[Operator] val < (val + 1)");
            Assert.IsTrue(valp1 > val, "[Operator] (val + 1) > val");

            Assert.IsTrue(val.Equals(val2));    // Equals(CSemVer?)
            Assert.IsFalse(val.Equals("val2"));

#pragma warning disable IDE0004 // Remove Unnecessary Cast
            // While it is technically redundant, it clarifies the test case.
            // These tests with null are calling two different APIs.
            Assert.IsFalse(val.Equals((CSemVer?)null));
            Assert.IsFalse(val.Equals((object?)null));
#pragma warning restore IDE0004 // Remove Unnecessary Cast
        }

        [TestMethod]
        public void FromTest( )
        {
            const UInt64 v0_0_0_Alpha = 1;
            VerifyOrderedVersionPair(v0_0_0_Alpha, 0, 0, 0, 0, 0, 0);

            const UInt64 v0_0_0_Alpha_0_1 = 2;
            VerifyOrderedVersionPair(v0_0_0_Alpha_0_1, 0, 0, 0, 0, 0, 1);

            const UInt64 v0_0_0_Beta = 10001;
            VerifyOrderedVersionPair(v0_0_0_Beta, 0, 0, 0, 1, 0, 0);

            const UInt64 v20_1_4_Beta = 800010800340005ul;
            VerifyOrderedVersionPair(v20_1_4_Beta, 20, 1, 4, 1, 0, 0);

            const UInt64 v20_1_4 = 800010800410005ul;
            VerifyOrderedVersionPair(v20_1_4, 20, 1, 4);

            const UInt64 v20_1_5_Alpha = 800010800410006ul;
            VerifyOrderedVersionPair(v20_1_5_Alpha, 20, 1, 5, 0, 0, 0);
        }

        public static void VerifyOrderedVersionPair(
            UInt64 orderedVersion,
            int major,
            int minor,
            int patch,
            int index,
            int number,
            int fix,
            [CallerArgumentExpression(nameof(orderedVersion))] string? exp = null
            )
        {
            var f64 = FileVersionQuad.From(orderedVersion << 1);
            var ver = CSemVer.From(f64.ToUInt64());
            Assert.AreEqual(major, ver.Major, exp);
            Assert.AreEqual(minor, ver.Minor, exp);
            Assert.AreEqual(patch, ver.Patch, exp);
            Assert.IsTrue(ver.PrereleaseVersion.IsValid, exp);
            Assert.IsTrue(ver.IsPrerelease, exp);
            Assert.AreEqual(index, ver.PrereleaseVersion.Index, exp);
            Assert.AreEqual(number, ver.PrereleaseVersion.Number, exp);
            Assert.AreEqual(fix, ver.PrereleaseVersion.Fix, exp);
            Assert.IsFalse(ver.CiBuildInfo.IsValid, exp);
            Assert.IsFalse(ver.IsCIBuild, exp);
            Assert.IsNotNull(ver.BuildMetaData, $"non-nullable property should not be null for '{exp}'");
            Assert.AreEqual(string.Empty, ver.BuildMetaData, $"non-nullable property should be an empty string if not set '{exp}'");
            Assert.AreEqual(orderedVersion, ver.OrderedVersion, exp);
            Assert.AreEqual(f64, ver.FileVersion, exp);

            // Now test CI variant
            var f64CI = FileVersionQuad.From((orderedVersion << 1) + 1);
            var verCI = CSemVer.From(f64CI.ToUInt64());
            Assert.AreEqual(major, verCI.Major, exp);
            Assert.AreEqual(minor, verCI.Minor, exp);
            Assert.AreEqual(patch, verCI.Patch, exp);
            Assert.IsTrue(verCI.PrereleaseVersion.IsValid, exp);
            Assert.IsTrue(verCI.IsPrerelease, exp);
            Assert.AreEqual(index, verCI.PrereleaseVersion.Index, exp);
            Assert.AreEqual(number, verCI.PrereleaseVersion.Number, exp);
            Assert.AreEqual(fix, verCI.PrereleaseVersion.Fix, exp);
            Assert.IsFalse(verCI.CiBuildInfo.IsValid, $"Conversion from integer should NOT have any CI build info for '{exp}'");
            Assert.IsTrue(verCI.IsCIBuild, $"Conversion from integer should still indicate it IS a CI build for '{exp}'");
            Assert.IsNotNull(verCI.BuildMetaData, $"non-nullable property should not be null for '{exp}'");
            Assert.AreEqual(string.Empty, verCI.BuildMetaData, $"non-nullable property should be an empty string if not set '{exp}'");
            Assert.AreEqual(orderedVersion, verCI.OrderedVersion , $"CI builds should have the same ordered version number as a non-CI build for '{exp}'");
            Assert.AreEqual(f64CI, verCI.FileVersion, exp);
        }

        public static void VerifyOrderedVersionPair(
            UInt64 orderedVersion,
            int major,
            int minor,
            int patch,
            [CallerArgumentExpression(nameof(orderedVersion))] string? exp = null
            )
        {
            var f64 = FileVersionQuad.From(orderedVersion << 1);
            var ver = CSemVer.From(f64.ToUInt64());
            Assert.AreEqual(major, ver.Major, exp);
            Assert.AreEqual(minor, ver.Minor, exp);
            Assert.AreEqual(patch, ver.Patch, exp);
            Assert.IsFalse(ver.PrereleaseVersion.IsValid, exp);
            Assert.IsFalse(ver.IsPrerelease, exp);
            Assert.IsFalse(ver.CiBuildInfo.IsValid, exp);
            Assert.IsFalse(ver.IsCIBuild, exp);
            Assert.IsNotNull(ver.BuildMetaData, $"non-nullable property should not be null for '{exp}'");
            Assert.AreEqual(string.Empty, ver.BuildMetaData, $"non-nullable property should be an empty string if not set for '{exp}'");
            Assert.AreEqual(orderedVersion, ver.OrderedVersion, exp);
            Assert.AreEqual(f64, ver.FileVersion, exp);

            // Now test CI variant
            var f64CI = FileVersionQuad.From((orderedVersion << 1) + 1);
            var verCI = CSemVer.From(f64CI.ToUInt64());
            Assert.AreEqual(major, verCI.Major, exp);
            Assert.AreEqual(minor, verCI.Minor, exp);
            Assert.AreEqual(patch, verCI.Patch, exp);
            Assert.IsFalse(verCI.PrereleaseVersion.IsValid, exp);
            Assert.IsFalse(verCI.IsPrerelease, exp);
            Assert.IsFalse(verCI.CiBuildInfo.IsValid, $"Conversion from integer should NOT have any CI build info for '{exp}'");
            Assert.IsTrue(verCI.IsCIBuild, $"Conversion from integer should indicate it IS a CI build for '{exp}'");
            Assert.IsNotNull(verCI.BuildMetaData, $"non-nullable property should not be null for '{exp}'");
            Assert.AreEqual(string.Empty, verCI.BuildMetaData, $"non-nullable property should be an empty string if not set for '{exp}'");
            Assert.AreEqual(orderedVersion, verCI.OrderedVersion , $"CI builds should have the same ordered version number as a non-CI build for '{exp}'");
            Assert.AreEqual(f64CI, verCI.FileVersion, exp);
        }
    }
}
