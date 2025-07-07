﻿// -----------------------------------------------------------------------
// <copyright file="FileVersionQuadTests.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ubiquity.NET.Versioning.UT
{
    [TestClass]
    public class FileVersionQuadTests
    {
        [TestMethod]
        public void ConstructorTest( )
        {
            var x = new FileVersionQuad(0x1234, 0x5678, 0x9ABC, 0xDEF0 );
            Assert.AreEqual((ushort)0x1234u, x.Major);
            Assert.AreEqual((ushort)0x5678u, x.Minor);
            Assert.AreEqual((ushort)0x9ABCu, x.Build);
            Assert.AreEqual((ushort)0xDEF0u, x.Revision);
        }

        [TestMethod]
        public void ToUInt64Test( )
        {
            var x = new FileVersionQuad(0x1234, 0x5678, 0x9ABC, 0xDEF0 );
            ulong y = x.ToUInt64();

            // NOTE: Major contains the Most significant "16 bits" of the result
            //       Minor the next...
            Assert.AreEqual(0x123456789ABCDEF0ul, y);
        }

        [TestMethod]
        public void CompareToTest( )
        {
            var valm1 = new FileVersionQuad(0x1234, 0x5678, 0x9ABC, 0xDEEF );
            var val = new FileVersionQuad(0x1234, 0x5678, 0x9ABC, 0xDEF0 );
            var valp1 = new FileVersionQuad(0x1234, 0x5678, 0x9ABC, 0xDEF1 );

            Assert.IsTrue(valm1.IsCiBuild);
            Assert.IsFalse(val.IsCiBuild);
            Assert.IsTrue(valp1.IsCiBuild);

            Assert.AreEqual(-1, valm1.CompareTo(val), "(val-1) < val");
            Assert.AreEqual(-1, valm1.CompareTo(valp1), "(val-1) < (val+1)");
            Assert.AreEqual(1, val.CompareTo(valm1), "val > (val-1)");
            Assert.AreEqual(1, valp1.CompareTo(valm1), "(val + 1) > (val - 1)");

            Assert.AreEqual(1, valp1.CompareTo(val), "(val+1) > val");
            Assert.AreEqual(1, valp1.CompareTo(valm1), "(val+1) > (val-1)");
            Assert.AreEqual(-1, val.CompareTo(valp1), "val < (val+1)");
            Assert.AreEqual(-1, valm1.CompareTo(valp1), "(val - 1) < (val - 1)");

            Assert.AreEqual(0, val.CompareTo(val), "val == val");
        }

        [TestMethod]
        public void ToVersionTest( )
        {
            var x = new FileVersionQuad(0x1234, 0x5678, 0x9ABC, 0xDEF0 );
            Assert.AreEqual(new Version(0x1234, 0x5678, 0x9ABC, 0xDEF0), x.ToVersion());
        }

        [TestMethod]
        public void FromTest( )
        {
            var x = new FileVersionQuad(0x123456789ABCDEF0ul);
            Assert.AreEqual((ushort)0x1234u, x.Major);
            Assert.AreEqual((ushort)0x5678u, x.Minor);
            Assert.AreEqual((ushort)0x9ABCu, x.Build);
            Assert.AreEqual((ushort)0xDEF0u, x.Revision);
        }
    }
}
