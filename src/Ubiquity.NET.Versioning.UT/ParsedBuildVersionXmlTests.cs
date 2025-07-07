// -----------------------------------------------------------------------
// <copyright file="ParsedBuildVersionXmlTests.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using System.Xml.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ubiquity.NET.Versioning.UT
{
    [TestClass]
    public class ParsedBuildVersionXmlTests
    {
        [TestMethod]
        public void ParseFromFile( )
        {
            var parsedData = ParsedBuildVersionXml.ParseFile("TestBuildVersion.xml");
            Assert.AreEqual(1, parsedData.BuildMajor);
            Assert.AreEqual(2, parsedData.BuildMinor);
            Assert.AreEqual(3, parsedData.BuildPatch);
            Assert.AreEqual("beta", parsedData.PreReleaseName);
            Assert.AreEqual(4, parsedData.PreReleaseNumber);
            Assert.AreEqual(5, parsedData.PreReleaseFix);
        }

        [TestMethod]
        public void Parse_from_release_produces_non_prerelease( )
        {
            var parsedData = ParsedBuildVersionXml.Parse(Xml_5_0_4);
            Assert.AreEqual(5, parsedData.BuildMajor);
            Assert.AreEqual(0, parsedData.BuildMinor);
            Assert.AreEqual(4, parsedData.BuildPatch);
            Assert.AreEqual(string.Empty, parsedData.PreReleaseName);
            Assert.AreEqual(0, parsedData.PreReleaseNumber);
            Assert.AreEqual(0, parsedData.PreReleaseFix);
        }

        [TestMethod]
        public void ParseExtraAttributeShouldFail( )
        {
            var ex = Assert.ThrowsExactly<InvalidDataException>(
                ()=> ParsedBuildVersionXml.Parse(InvalidAttributeXml)
            );

            Assert.AreEqual("Unexpected attribute foo", ex.Message);
        }

        [TestMethod]
        public void ParseEmptyDocShouldFail( )
        {
            var ex = Assert.ThrowsExactly<FormatException>(
                ()=> ParsedBuildVersionXml.Parse(EmptyXmlDoc)
            );

            Assert.AreEqual("XML element 'BuildVersionData' element not found", ex.Message);
        }

        [TestMethod]
        public void Parsing_of_minimally_valid_xml_should_provide_defaults( )
        {
            var results = ParsedBuildVersionXml.Parse(MinimallyValidXml);
            Assert.AreEqual(0, results.BuildMajor);
            Assert.AreEqual(0, results.BuildMinor);
            Assert.AreEqual(0, results.BuildPatch);
            Assert.AreEqual(string.Empty, results.PreReleaseName);
            Assert.AreEqual(0, results.PreReleaseNumber);
            Assert.AreEqual(0, results.PreReleaseFix);
        }

        private static readonly XDocument InvalidAttributeXml
            = new( new XElement("BuildVersionData",
                       new XAttribute("BuildMajor", "1"),
                       new XAttribute("BuildMinor", "2"),
                       new XAttribute("BuildPatch", "3"),
                       new XAttribute("PreReleaseName", "gamma"),
                       new XAttribute("PreReleaseNumber", "4"),
                       new XAttribute("PreReleaseFix", "5"),
                       new XAttribute("foo", "bar")
                    )
                 );

        private static readonly XDocument Xml_5_0_4
            = new( new XElement("BuildVersionData",
                       new XAttribute("BuildMajor", "5"),
                       new XAttribute("BuildMinor", "0"),
                       new XAttribute("BuildPatch", "4")
                    )
                 );

        private static readonly XDocument EmptyXmlDoc = new( );

        private static readonly XDocument MinimallyValidXml = new( new XElement("BuildVersionData"));
    }
}
