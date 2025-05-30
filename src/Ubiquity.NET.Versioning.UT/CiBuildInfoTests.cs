// -----------------------------------------------------------------------
// <copyright file="CiBuildInfoTests.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ubiquity.NET.Versioning.UT
{
    [TestClass]
    public class CiBuildInfoTests
    {
        [TestMethod]
        public void CiBuildInfoTest( )
        {
            CiBuildInfo defaultCi = default;
            Assert.IsFalse(defaultCi.IsValid);

#pragma warning disable MSTEST0025 // Use 'Assert.Fail' instead of an always-failing assert
            // Warning is BS! - This is VALIDATING runtime behavior that contradicts nullability annotations
            // Default construct is effectively memset(0) which sets it to a null value ignoring the annotations.
            Assert.IsNull(defaultCi.BuildIndex);
            Assert.IsNull(defaultCi.BuildName);
#pragma warning restore MSTEST0025 // Use 'Assert.Fail' instead of an always-failing assert

            // "Golden path..."
            var bi = new CiBuildInfo("index", "name");
            Assert.AreEqual("index", bi.BuildIndex);
            Assert.AreEqual("name", bi.BuildName);
            Assert.IsTrue(bi.IsValid);

            // Now test how it handles bogus input...

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
// validating CLAIMs of the API is the point of this part of the test
            var argnex = Assert.ThrowsExactly<ArgumentNullException>(()=> _ = new CiBuildInfo("index", null));
            Assert.AreEqual("name", argnex.ParamName);

            argnex = Assert.ThrowsExactly<ArgumentNullException>(()=> _ = new CiBuildInfo(null, "name"));
            Assert.AreEqual("index", argnex.ParamName);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

            var argex = Assert.ThrowsExactly<ArgumentException>(()=> _ = new CiBuildInfo("index", string.Empty));
            Assert.AreEqual("name", argex.ParamName);

            argex = Assert.ThrowsExactly<ArgumentException>(()=> _ = new CiBuildInfo(string.Empty, "name"));
            Assert.AreEqual("index", argex.ParamName);

            // '~' is not valid for either name or index...
            argex = Assert.ThrowsExactly<ArgumentException>(()=> _ = new CiBuildInfo("1234~5", "name"));
            Assert.AreEqual("index", argex.ParamName);

            argex = Assert.ThrowsExactly<ArgumentException>(()=> _ = new CiBuildInfo("12345", "na~me"));
            Assert.AreEqual("name", argex.ParamName);
        }

        [TestMethod]
        public void ToStringTest( )
        {
            var bi = new CiBuildInfo("index", "name");
            var argex = Assert.ThrowsExactly<ArgumentException>(()=> _ = bi.ToString("F", null));
            Assert.AreEqual("format", argex.ParamName);

            Assert.AreEqual(".ci.index.name", bi.ToString(), "Should default to 'P' format");
            Assert.AreEqual(".ci.index.name", bi.ToString("P", null));
            Assert.AreEqual("--ci.index.name", bi.ToString("R", null));
        }
    }
}
