// -----------------------------------------------------------------------
// <copyright file="PrereleaseVersionTests.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ubiquity.NET.Versioning.Tests
{
    [TestClass]
    public class PrereleaseVersionTests
    {
        [TestMethod]
        public void IntegerConstructionTests( )
        {
            var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>( ( ) => _ = new PrereleaseVersion( 8, 0, 0 ) );
            Assert.AreEqual(8, ex.ActualValue);
            Assert.AreEqual("index", ex.ParamName);

            ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>( ( ) => _ = new PrereleaseVersion( 1, 100, 0 ) );
            Assert.AreEqual(100, ex.ActualValue);
            Assert.AreEqual("number", ex.ParamName);

            ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>( ( ) => _ = new PrereleaseVersion( 1, 0, 100 ) );
            Assert.AreEqual(100, ex.ActualValue);
            Assert.AreEqual("fix", ex.ParamName);

            var prv = new PrereleaseVersion(2, 3, 4);
            Assert.AreEqual(2, prv.Index);
            Assert.AreEqual(3, prv.Number);
            Assert.AreEqual(4, prv.Fix);
            Assert.AreEqual("delta", prv.Name);
            Assert.AreEqual("d", prv.ShortName);
        }

        [TestMethod]
        public void StringConstructionTests( )
        {
            var argex = Assert.ThrowsExactly<ArgumentException>( ( ) => _ = new PrereleaseVersion( string.Empty, 3, 4 ) );
            Assert.AreEqual("preRelName", argex.ParamName);

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            // Test is validating claimed behavior
            var argn = Assert.ThrowsExactly<ArgumentNullException>( ( ) => _ = new PrereleaseVersion( null, 3, 4 ) );
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            Assert.AreEqual("preRelName", argex.ParamName);

            var prv = new PrereleaseVersion( "beta", 3, 4 );
            Assert.AreEqual(1, prv.Index);
            Assert.AreEqual(3, prv.Number);
            Assert.AreEqual(4, prv.Fix);
            Assert.AreEqual("beta", prv.Name);
            Assert.AreEqual("b", prv.ShortName);

            prv = new PrereleaseVersion( "EPSILON", 3, 4 );
            Assert.AreEqual(3, prv.Index);
            Assert.AreEqual(3, prv.Number);
            Assert.AreEqual(4, prv.Fix);
            Assert.AreEqual("epsilon", prv.Name);
            Assert.AreEqual("e", prv.ShortName);
        }

        [TestMethod]
        public void ToStringTest( )
        {
            var prv = new PrereleaseVersion( "EPSILON", 3, 4 );
            Assert.AreEqual("-epsilon.3.4", prv.ToString());

            prv = new PrereleaseVersion( "alpha", 0, 0);
            Assert.AreEqual("-alpha", prv.ToString());

            prv = new PrereleaseVersion( "alpha", 0, 1);
            Assert.AreEqual("-alpha.0.1", prv.ToString());

            prv = new PrereleaseVersion( "alpha", 1, 0);
            Assert.AreEqual("-alpha.1", prv.ToString());

            prv = new PrereleaseVersion( "beta", 1, 0);
            Assert.AreEqual("-b01",prv.ToString("S", null));

            // The Z format is used when there is CI build info available
            Assert.AreEqual("-beta.1.0",prv.ToString("Z", null));
            Assert.AreEqual("-b01-00",prv.ToString("SZ", null), "format combinations are allowed");
        }
    }
}
