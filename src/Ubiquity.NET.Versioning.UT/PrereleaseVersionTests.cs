// -----------------------------------------------------------------------
// <copyright file="PrereleaseVersionTests.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;

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
            Assert.AreEqual( (byte)8, ex.ActualValue );
            Assert.AreEqual( "index", ex.ParamName );

            ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>( ( ) => _ = new PrereleaseVersion( 1, 100, 0 ) );
            Assert.AreEqual( (byte)100, ex.ActualValue );
            Assert.AreEqual( "number", ex.ParamName );

            ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>( ( ) => _ = new PrereleaseVersion( 1, 0, 100 ) );
            Assert.AreEqual( (byte)100, ex.ActualValue );
            Assert.AreEqual( "fix", ex.ParamName );

            var prv = new PrereleaseVersion(2, 3, 4);
            Assert.AreEqual( (byte)2, prv.Index );
            Assert.AreEqual( (byte)3, prv.Number );
            Assert.AreEqual( (byte)4, prv.Fix );
            Assert.AreEqual( "delta", prv.Name );
        }

        [TestMethod]
        public void StringConstructionTests( )
        {
            var argex = Assert.ThrowsExactly<ArgumentException>( ( ) => _ = new PrereleaseVersion( string.Empty, 3, 4 ) );
            Assert.AreEqual( "preRelName", argex.ParamName );

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            // Test is validating claimed behavior
            var argn = Assert.ThrowsExactly<ArgumentNullException>( ( ) => _ = new PrereleaseVersion( null, 3, 4 ) );
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            Assert.AreEqual( "preRelName", argex.ParamName );

            var prv = new PrereleaseVersion( "beta", 3, 4 );
            Assert.AreEqual( (byte)1, prv.Index );
            Assert.AreEqual( (byte)3, prv.Number );
            Assert.AreEqual( (byte)4, prv.Fix );
            Assert.AreEqual( "beta", prv.Name );

            prv = new PrereleaseVersion( "EPSILON", 3, 4 );
            Assert.AreEqual( (byte)3, prv.Index );
            Assert.AreEqual( (byte)3, prv.Number );
            Assert.AreEqual( (byte)4, prv.Fix );
            Assert.AreEqual( "epsilon", prv.Name );
        }

        [TestMethod]
        public void ToStringTest( )
        {
            var prv = new PrereleaseVersion( "EPSILON", 3, 4 );
            Assert.AreEqual( "epsilon.3.4", prv.ToString() );

            prv = new PrereleaseVersion( "alpha", 0, 0 );
            Assert.AreEqual( "alpha", prv.ToString() );

            prv = new PrereleaseVersion( "alpha", 0, 1 );
            Assert.AreEqual( "alpha.0.1", prv.ToString() );

            prv = new PrereleaseVersion( "alpha", 1, 0 );
            Assert.AreEqual( "alpha.1", prv.ToString() );

            prv = new PrereleaseVersion( "beta", 1, 0 );
            Assert.AreEqual( "beta.1", prv.ToString() );

            string[] expectedSeq = ["beta", "0", "0"];
            prv = new PrereleaseVersion( "beta", 0, 0 );
            Assert.IsTrue( expectedSeq.SequenceEqual( prv.FormatElements( alawaysIncludeZero: true ) ) );

            expectedSeq = ["beta"];
            Assert.IsTrue( expectedSeq.SequenceEqual( prv.FormatElements( alawaysIncludeZero: false ) ) );
        }
    }
}
