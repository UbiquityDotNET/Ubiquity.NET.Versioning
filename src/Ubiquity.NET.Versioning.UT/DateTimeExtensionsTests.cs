// -----------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Globalization;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ubiquity.NET.Versioning.UT
{
    [TestClass]
    public class DateTimeExtensionsTests
    {
        [TestMethod]
        public void ToBuildIndexTest( )
        {
            var timeStamp = new DateTime(2025, 5, 19, 17, 9, 0, DateTimeKind.Local);
            string index = timeStamp.ToBuildIndex();
            timeStamp = timeStamp.AddSeconds(1);
            string index2 = timeStamp.ToBuildIndex();
            Assert.AreEqual(index, index2, "Increment of only 1 second, results in same index value");

            timeStamp = timeStamp.AddSeconds(1);
            index2 = timeStamp.ToBuildIndex();
            Assert.AreNotEqual(index, index2,  "Increment of 2 seconds, results in different index value");
        }

        [TestMethod]
        public void RoundTrippingProducesExpectedValue( )
        {
            // validate assumptions for framework consumers will use
            string testIso8601 = "2025-06-02T15:16:05.6936163Z";
            var testDt = DateTime.Parse(testIso8601, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            string roundTrippedIso = testDt.ToString("o", CultureInfo.InvariantCulture);
            Assert.AreEqual(testIso8601, roundTrippedIso);

            // Validate that a well known value is the result
            string actualIndex = testDt.ToBuildIndex();
            Assert.AreEqual("608463706", actualIndex);
        }
    }
}
