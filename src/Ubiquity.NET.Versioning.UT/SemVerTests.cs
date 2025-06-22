// -----------------------------------------------------------------------
// <copyright file="SemVerTests.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ubiquity.NET.Versioning.UT
{
    [TestClass]
    public class SemVerTests
    {
        [TestMethod]
        public void SemVerTest( )
        {
            var sv1 = new SemVer(1, 2, 3);
            Assert.AreEqual(1, sv1.Major);
            Assert.AreEqual(2, sv1.Minor);
            Assert.AreEqual(3, sv1.Patch);
            Assert.AreEqual(0, sv1.PreRelease.Length);
            Assert.AreEqual(0, sv1.BuildMeta.Length);

            var sv2 = new SemVer(1, 2, 3, ["pre-rel"]);
            Assert.AreEqual(1, sv2.Major);
            Assert.AreEqual(2, sv2.Minor);
            Assert.AreEqual(3, sv2.Patch);
            Assert.AreEqual(1, sv2.PreRelease.Length);
            Assert.AreEqual("pre-rel", sv2.PreRelease[0]);
            Assert.AreEqual(0, sv2.BuildMeta.Length);

            var sv3 = new SemVer(1, 2, 3, [], ["meta"]);
            Assert.AreEqual(1, sv3.Major);
            Assert.AreEqual(2, sv3.Minor);
            Assert.AreEqual(3, sv3.Patch);
            Assert.AreEqual(0, sv3.PreRelease.Length);
            Assert.AreEqual(1, sv3.BuildMeta.Length);
            Assert.AreEqual("meta", sv3.BuildMeta[0]);
        }

        [TestMethod]
        public void SortOrderIngTestsCaseInsensitive( )
        {
            // 1.0.0-alpha < 1.0.0-alpha.1 < 1.0.0-alpha.beta < 1.0.0-beta < 1.0.0-beta.2 < 1.0.0-beta.11 < 1.0.0-rc.1 < 1.0.0.
            string[] verStrings = [
                "1.0.0-alpha",
                "1.0.0-alpha.1",
                "1.0.0-alpha.beta",
                "1.0.0-beta",
                "1.0.0-beta.2",
                "1.0.0-beta.11",
                "1.0.0-rc.1",
                "1.0.0",
                "1.1.0-alpha",
                "1.1.1-alpha",
                "1.1.1",
                "2.1.1",
            ];

            SemVer[] sample = [ .. verStrings.Select(s=>SemVer.Parse(s, null)) ];
            var comparer = SemVerComparer.SemVer;
            for(int i = 1; i < verStrings.Length; ++i)
            {
                var lhs = sample[i-1];
                var rhs = sample[i];

                Assert.IsTrue(comparer.Compare(lhs, rhs) < 0, $"'{lhs}' should compare less than '{rhs}'");

                // inverted should produce correct results too.
                Assert.IsTrue(comparer.Compare(rhs, lhs) > 0, $"'{rhs}' should compare greater than '{lhs}'");

                Assert.IsTrue(comparer.Compare(lhs, null) > 0, "Any version should compare greater than null");
#pragma warning disable IDE0002 // Simplify Member Access
                // Simplification results in a different API call! Test is for a specific case
                Assert.IsTrue(SemVer.Equals(null, null));
                Assert.IsFalse(SemVer.Equals(lhs, null));
                Assert.IsFalse(SemVer.Equals(null, rhs));
#pragma warning restore IDE0002 // Simplify Member Access

                var otherRef = lhs;
                Assert.AreEqual(0, comparer.Compare(lhs, otherRef), $"'{lhs}' should be == to itself");

                // Clone it to validate value equality
                var sameVal = new SemVer(lhs.Major, lhs.Minor, lhs.Patch, lhs.PreRelease, lhs.BuildMeta);
                Assert.AreEqual( 0, comparer.Compare( lhs, sameVal ), "new instance of same value should be equal");
            }
        }

        [TestMethod]
        public void ToStringTest( )
        {
            var verCore = new SemVer(1, 2, 3);
            Assert.AreEqual( "1.2.3", verCore.ToString() );

            var verCoreWithPreRel = new SemVer(3, 4, 5, ["part1", "part2"]);
            Assert.AreEqual( "3.4.5-part1.part2", verCoreWithPreRel.ToString() );

            var verCoreWith1PreRel = new SemVer(3, 4, 5, ["part1"]);
            Assert.AreEqual( "3.4.5-part1", verCoreWith1PreRel.ToString() );

            var verCoreWithBuildMeta = new SemVer(6, 7, 8, [], ["part1", "part2"] );
            Assert.AreEqual( "6.7.8+part1.part2", verCoreWithBuildMeta.ToString() );

            var verCoreWith1BuildMeta = new SemVer(6, 7, 8, [], ["part1"] );
            Assert.AreEqual( "6.7.8+part1", verCoreWith1BuildMeta.ToString() );

            var verCoreWithPreRelAndBuildMeta = new SemVer(9, 10, 11, ["pr-part1", "pr-part2"], ["build1", "build2"]);
            Assert.AreEqual( "9.10.11-pr-part1.pr-part2+build1.build2", verCoreWithPreRelAndBuildMeta.ToString() );

            foreach(var kvp in ValidSemVerStrings)
            {
                Assert.AreEqual(kvp.Key, kvp.Value.ToString());
            }
        }

        [TestMethod]
        public void ParseTest( )
        {
            foreach(var kvp in ValidSemVerStrings)
            {
                var result = SemVer.Parse( kvp.Key, null );
                ValidateEquavalent( kvp.Value, result, kvp.Key );
            }

            foreach(string ver in InvalidSemVerStrings)
            {
                // For invalid strings testing success/Failure of parse is all that's reasonably possible
                Assert.ThrowsExactly<FormatException>(()=>_ = SemVer.Parse( ver, null));
            }
        }

        [TestMethod]
        public void TryParseTest( )
        {
            foreach(var kvp in ValidSemVerStrings)
            {
                Assert.IsTrue( SemVer.TryParse( kvp.Key, null, out SemVer? result ), $"Expected valid version to parse for '{kvp.Key}')" );
                ValidateEquavalent( kvp.Value, result, kvp.Key );
            }

            foreach(string ver in InvalidSemVerStrings)
            {
                // For invalid strings testing success/Failure of parse is all that's reasonably possible
                Assert.IsFalse( SemVer.TryParse( ver, null, out _ ), $"Expected invalid version to fail: {ver}" );
            }
        }

        private static void ValidateEquavalent( SemVer expected, SemVer actual, string input )
        {
            Assert.AreEqual( expected.Major, actual.Major, $"Major should match for '{input}'" );
            Assert.AreEqual( expected.Minor, actual.Minor, $"Minor should match for '{input}'" );
            Assert.AreEqual( expected.Patch, actual.Patch, $"Patch should match for '{input}'" );
            Assert.AreEqual( expected.PreRelease.Length, actual.PreRelease.Length, $"PreRelease.Count should match for '{input}'" );
            Assert.AreEqual( expected.BuildMeta.Length, actual.BuildMeta.Length, $"BuildMeta.Count should match for '{input}'" );
            for(int i = 0; i < expected.PreRelease.Length; ++i)
            {
                Assert.AreEqual( expected.PreRelease[ i ], actual.PreRelease[ i ], $"PreRelease[{i}] should match for '{input}'" );
            }

            for(int i = 0; i < expected.BuildMeta.Length; ++i)
            {
                Assert.AreEqual( expected.BuildMeta[ i ], actual.BuildMeta[ i ], $"BuildMeta[{i}] should match for '{input}'" );
            }
        }

        // Subset of valid SemVer Strings from https://regex101.com/r/Ly7O1x/3/
        private static readonly Dictionary<string,SemVer> ValidSemVerStrings
        = new()
        {
            ["0.0.4"] = new(0, 0, 4),
            ["1.2.3"] = new(1, 2, 3),
            ["10.20.30"] = new(10, 20, 30),
            ["1.1.2-prerelease+meta"] = new(1, 1, 2, ["prerelease"], ["meta"]),
            ["1.1.2+meta"] = new(1, 1, 2, [], ["meta"]),
            ["1.1.2+meta-valid"] = new(1, 1, 2, [], ["meta-valid"]),
            ["1.0.0-alpha"] = new(1, 0, 0, ["alpha"]),
            ["1.0.0-beta"] = new(1, 0, 0, ["beta"]),
            ["1.0.0-alpha.beta"] = new(1, 0, 0, ["alpha", "beta"]),
            ["1.0.0-alpha.beta.1"] = new(1, 0, 0, ["alpha", "beta", "1"]),
            ["1.0.0-alpha.1"] = new(1, 0, 0, ["alpha", "1"]),
            ["1.0.0-alpha0.valid"] = new(1, 0, 0, ["alpha0", "valid"]),
            ["1.0.0-alpha.0valid"] = new(1, 0, 0, ["alpha", "0valid"]),
            ["1.0.0-alpha-a.b-c-somethinglong+build.1-aef.1-its-okay"] = new(1, 0, 0, ["alpha-a", "b-c-somethinglong"], ["build", "1-aef", "1-its-okay"]),
            ["1.0.0-rc.1+build.1"] = new(1, 0, 0, ["rc", "1"], ["build", "1"]),
            ["2.0.0-rc.1+build.123"] = new(2, 0, 0, ["rc", "1"], ["build", "123"]),
            ["1.2.3-beta"] = new(1, 2, 3, ["beta"]),
            ["10.2.3-DEV-SNAPSHOT"] = new(10, 2, 3, ["DEV-SNAPSHOT"]),
            ["1.2.3-SNAPSHOT-123"] = new(1, 2, 3, ["SNAPSHOT-123"]),
            ["1.0.0"] = new(1, 0, 0),
            ["2.0.0"] = new(2, 0, 0),
            ["1.1.7"] = new(1, 1, 7),
            ["2.0.0+build.1848"] = new(2, 0, 0, [], ["build", "1848"]),
            ["2.0.1-alpha.1227"] = new(2, 0, 1, ["alpha", "1227"]),
            ["1.0.0-alpha+beta"] = new(1, 0, 0, ["alpha"], ["beta"]),
            ["1.2.3----RC-SNAPSHOT.12.9.1--.12+788"] = new(1, 2, 3, ["---RC-SNAPSHOT", "12", "9", "1--", "12"], ["788"]),
            ["1.2.3----R-S.12.9.1--.12+meta"] = new(1, 2, 3, ["---R-S", "12", "9", "1--", "12"], ["meta"]),
            ["1.2.3----RC-SNAPSHOT.12.9.1--.12"] = new(1, 2, 3, ["---RC-SNAPSHOT", "12", "9", "1--", "12"]),
            ["1.0.0+0.build.1-rc.10000aaa-kk-0.1"] = new(1, 0, 0, [], ["0", "build", "1-rc", "10000aaa-kk-0", "1"]),
            ["99999999999999999999999.999999999999999999.99999999999999999"] = new( make_big_int("99999999999999999999999"), make_big_int("999999999999999999"), make_big_int("99999999999999999")),
            ["1.0.0-0A.is.legal"] = new(1, 0, 0, ["0A", "is", "legal"]),
        };

        [SuppressMessage( "StyleCop.CSharp.NamingRules", "SA1300:Element should begin with upper-case letter", Justification = "UtilityFunction - get over it!" )]
        [SuppressMessage( "Style", "IDE1006:Naming Styles", Justification = "UtilityFunction - get over it!" )]
        private static BigInteger make_big_int(string val)
        {
            return BigInteger.Parse(val, CultureInfo.InvariantCulture);
        }

        // Subset of invalid SemVer Strings from https://regex101.com/r/Ly7O1x/3/
        private static readonly string[] InvalidSemVerStrings =
        [
            "1",
            "1.2",
            "1.2.3-0123",
            "1.2.3-0123.0123",
            "1.1.2+.123",
            "+invalid",
            "-invalid",
            "-invalid+invalid",
            "-invalid.01",
            "alpha",
            "alpha.beta",
            "alpha.beta.1",
            "alpha.1",
            "alpha+beta",
            "alpha_beta",
            "alpha.",
            "alpha..",
            "beta",
            "1.0.0-alpha_beta",
            "-alpha.",
            "1.0.0-alpha..",
            "1.0.0-alpha..1",
            "1.0.0-alpha...1",
            "1.0.0-alpha....1",
            "1.0.0-alpha.....1",
            "1.0.0-alpha......1",
            "1.0.0-alpha.......1",
            "01.1.1",
            "1.01.1",
            "1.1.01",
            "1.2",
            "1.2.3.DEV",
            "1.2-SNAPSHOT",
            "1.2.31.2.3----RC-SNAPSHOT.12.09.1--..12+788",
            "1.2-RC-SNAPSHOT",
            "-1.0.3-gamma+b7718",
            "+justmeta",
            "9.8.7+meta+meta",
            "9.8.7-whatever+meta+meta",
            "99999999999999999999999.999999999999999999.99999999999999999----RC-SNAPSHOT.12.09.1--------------------------------..12",
        ];
    }
}
