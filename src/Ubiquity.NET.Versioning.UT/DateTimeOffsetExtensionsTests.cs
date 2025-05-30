// -----------------------------------------------------------------------
// <copyright file="DateTimeOffsetExtensionsTests.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ubiquity.NET.Versioning.UT
{
    [TestClass]
    public class DateTimeOffsetExtensionsTests
    {
        [TestMethod]
        public void ToBuildIndexTest( )
        {
            string index = DateTimeOffset.Now.ToBuildIndex();
            var dto = new DateTimeOffset(2025, 5, 19, 17, 9, 0, TimeSpan.Zero);
            index = dto.ToBuildIndex();
            dto = dto.AddSeconds(1);
            string index2 = dto.ToBuildIndex();
            Assert.AreEqual(index, index2, "Increment of only 1 second, results in same index value");

            dto = dto.AddSeconds(1);
            index2 = dto.ToBuildIndex();
            Assert.AreNotEqual(index, index2,  "Increment of 2 seconds, results in different index value");
        }
    }
}
