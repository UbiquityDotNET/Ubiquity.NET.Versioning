// -----------------------------------------------------------------------
// <copyright file="CSemVerTests.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using System.Runtime.CompilerServices;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ubiquity.NET.Versioning.UT
{
    [TestClass]
    public class CSemVerTests
    {
        [TestMethod]
        [TestCategory("Constructor")]
        public void CSemVerTest( )
        {
            var ver = new CSemVer(1,2,3);
            Assert.AreEqual( 1, ver.Major );
            Assert.AreEqual( 2, ver.Minor );
            Assert.AreEqual( 3, ver.Patch );
            Assert.IsFalse( ver.IsPrerelease );
            Assert.IsFalse( ver.PrereleaseVersion.HasValue);
            Assert.AreEqual( 0, ver.BuildMeta.Length);

            var preRelInfo = new PrereleaseVersion(1, 2, 3);
            string[] expectedMeta = ["buildMeta"];
            ver = new CSemVer( 4, 5, 6, preRelInfo, expectedMeta );
            Assert.AreEqual( 4, ver.Major );
            Assert.AreEqual( 5, ver.Minor );
            Assert.AreEqual( 6, ver.Patch );
            Assert.IsTrue( ver.IsPrerelease );
            Assert.IsTrue( ver.PrereleaseVersion.HasValue );
            Assert.IsTrue( expectedMeta.SequenceEqual(ver.BuildMeta));
        }

        [TestMethod]
        public void DefaultConstructorTests( )
        {
            var ver = new CSemVer();
            Assert.AreEqual( 0, ver.Major );
            Assert.AreEqual( 0, ver.Minor );
            Assert.AreEqual( 0, ver.Patch );
            Assert.IsFalse( ver.IsPrerelease );
            Assert.IsFalse( ver.PrereleaseVersion.HasValue);
            Assert.AreEqual( 0, ver.BuildMeta.Length);
        }

        [TestMethod]
        public void ToStringTest( )
        {
            // Validate ToString("bogus") throws...
            var ver = new CSemVer(1,2,3);

            var alpha_0_0 = new PrereleaseVersion(0, 0, 0);
            var beta_1_0 = new PrereleaseVersion(1, 1, 0);
            var delta_0_1 = new PrereleaseVersion(2, 0, 1);

            // Validate ToString(null, null); // same as ToString("M")
            Assert.AreEqual("20.1.4+buildMeta", new CSemVer(20, 1, 4, default, ["buildMeta"]).ToString());

            // Validate ToString() P=0; CI=0
            Assert.AreEqual("20.1.4+buildMeta", new CSemVer(20, 1, 4, default,  ["buildMeta"]).ToString());

            // Validate ToString() P=1; CI=0
            Assert.AreEqual("20.1.4-alpha+buildMeta", new CSemVer(20, 1, 4, alpha_0_0, ["buildMeta"]).ToString());
            Assert.AreEqual("20.1.4-beta.1+buildMeta", new CSemVer(20, 1, 4, beta_1_0, ["buildMeta"]).ToString());
            Assert.AreEqual("20.1.4-delta.0.1+buildMeta", new CSemVer(20, 1, 4, delta_0_1, ["buildMeta"]).ToString());
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
            VerifyOrderedVersion(v0_0_0_Alpha, 0, 0, 0, 0, 0, 0);

            const UInt64 v0_0_0_Alpha_0_1 = 2;
            VerifyOrderedVersion(v0_0_0_Alpha_0_1, 0, 0, 0, 0, 0, 1);

            const UInt64 v0_0_0_Beta = 10001;
            VerifyOrderedVersion(v0_0_0_Beta, 0, 0, 0, 1, 0, 0);

            const UInt64 v20_1_4_Beta = 800010800340005ul;
            VerifyOrderedVersion(v20_1_4_Beta, 20, 1, 4, 1, 0, 0);

            const UInt64 v20_1_4 = 800010800410005ul;
            VerifyOrderedVersion(v20_1_4, 20, 1, 4);

            const UInt64 v20_1_5_Alpha = 800010800410006ul;
            VerifyOrderedVersion(v20_1_5_Alpha, 20, 1, 5, 0, 0, 0);
        }

        public static void VerifyOrderedVersion(
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
            // Now test Release variant
            var ver = CSemVer.FromOrderedVersion(orderedVersion);
            Assert.AreEqual(major, ver.Major, exp);
            Assert.AreEqual(minor, ver.Minor, exp);
            Assert.AreEqual(patch, ver.Patch, exp);
            Assert.IsTrue(ver.PrereleaseVersion.HasValue, exp);
            Assert.IsTrue(ver.IsPrerelease, exp);
            Assert.AreEqual(index, ver.PrereleaseVersion.Value.Index, exp);
            Assert.AreEqual(number, ver.PrereleaseVersion.Value.Number, exp);
            Assert.AreEqual(fix, ver.PrereleaseVersion.Value.Fix, exp);
            Assert.IsNotNull(ver.BuildMeta, $"non-nullable property should not be null for '{exp}'");
            Assert.AreEqual(0, ver.BuildMeta.Length, $"non-nullable property should be empty if not set for '{exp}'");
            Assert.AreEqual(orderedVersion, ver.OrderedVersion , $"builds should have the same ordered version number provided for '{exp}'");
        }

        public static void VerifyOrderedVersion(
            UInt64 orderedVersion,
            int major,
            int minor,
            int patch,
            [CallerArgumentExpression(nameof(orderedVersion))] string? exp = null
            )
        {
            var ver = CSemVer.FromOrderedVersion(orderedVersion);
            Assert.AreEqual(major, ver.Major, exp);
            Assert.AreEqual(minor, ver.Minor, exp);
            Assert.AreEqual(patch, ver.Patch, exp);
            Assert.IsFalse(ver.PrereleaseVersion.HasValue, exp);
            Assert.IsFalse(ver.IsPrerelease, exp);
            Assert.IsNotNull(ver.BuildMeta, $"non-nullable property should not be null for '{exp}'");
            Assert.AreEqual(0, ver.BuildMeta.Length, $"non-nullable property should be empty if not set for '{exp}'");
            Assert.AreEqual(orderedVersion, ver.OrderedVersion , $"should have the same ordered version number as provided for '{exp}'");
        }
    }
}
