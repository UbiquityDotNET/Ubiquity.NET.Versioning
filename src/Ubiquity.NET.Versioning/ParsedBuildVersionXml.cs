// -----------------------------------------------------------------------
// <copyright file="ParsedBuildVersionXml.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Globalization;
using System.IO;
using System.Xml.Linq;

namespace Ubiquity.NET.Versioning
{
    /// <summary>POCO version of information parsed from a Build version XML file</summary>
    /// <param name="BuildMajor">Major build number</param>
    /// <param name="BuildMinor">Minor build number</param>
    /// <param name="BuildPatch">Patch build number</param>
    /// <param name="PreReleaseName">Pre-release name for the build</param>
    /// <param name="PreReleaseNumber">Pre-release number for the build</param>
    /// <param name="PreReleaseFix">Pre-release fix for the build</param>
    /// <remarks>
    /// This structure holds the values parsed from a build version XML file, these values serve
    /// as a common base input for generation of a final <see cref="CSemVer"/> for a given build.
    /// </remarks>
    public readonly record struct ParsedBuildVersionXml(
        int BuildMajor,
        int BuildMinor,
        int BuildPatch,
        string PreReleaseName,
        int PreReleaseNumber,
        int PreReleaseFix
        )
    {
        /// <summary>Parse a <see cref="ParsedBuildVersionXml"/> from an <see cref="XDocument"/></summary>
        /// <param name="xdoc">Document to parse</param>
        /// <returns>Parsed version information</returns>
        /// <remarks>
        /// <para>This is the core of the parsing support, all of the other overloads and parsing methods ultimately
        /// call this to perform the actual parsing of XML data.</para>
        /// <para>The schema requirements of the XML file are fairly simple. It
        /// consists of a single element 'BuildVersionData' which has a number of optional attributes:</para>
        /// <para><list type="list">
        /// <item><term>BuildMajor</term><description>Major build number [default: 0]</description></item>
        /// <item><term>BuildMinor</term><description>Minor build number [default: 0]</description></item>
        /// <item><term>BuildPatch</term><description>Build Patch number [default: 0]</description></item>
        /// <item><term>PreReleaseName</term><description>Pre-Release Name [default: Empty String]</description></item>
        /// <item><term>PreReleaseNumber</term><description>Pre-Release number [default: 0]</description></item>
        /// <item><term>PreReleaseFix</term><description>Pre-Release fix number [default: 0]</description></item>
        /// </list></para>
        /// <para>Other elements are ignored, Though other attributes on the 'BuildVersionData' result in an exception.</para>
        /// </remarks>
        /// <exception cref="FormatException">Data format of the document is not valid</exception>
        /// <exception cref="InvalidDataException">Attribute for the "BuildVersionData" element is not known</exception>
        public static ParsedBuildVersionXml Parse( XDocument xdoc )
        {
            xdoc.ThrowIfNull();

            // set default values
            int buildMajor = 0;
            int buildMinor = 0;
            int buildPatch = 0;
            string preReleaseName = string.Empty;
            int preReleaseNumber = 0;
            int preReleaseFix = 0;

            var data = xdoc.Element( "BuildVersionData" ) ?? throw new FormatException("XML element 'BuildVersionData' element not found");

            foreach( var attrib in data.Attributes( ) )
            {
                switch( attrib.Name.LocalName )
                {
                case "BuildMajor":
                    buildMajor = Convert.ToInt32( attrib.Value, CultureInfo.CurrentCulture );
                    break;

                case "BuildMinor":
                    buildMinor = Convert.ToInt32( attrib.Value, CultureInfo.CurrentCulture );
                    break;

                case "BuildPatch":
                    buildPatch = Convert.ToInt32( attrib.Value, CultureInfo.CurrentCulture );
                    break;

                case "PreReleaseName":
                    preReleaseName = attrib.Value;
                    break;

                case "PreReleaseNumber":
                    preReleaseNumber = Convert.ToInt32( attrib.Value, CultureInfo.CurrentCulture );
                    break;

                case "PreReleaseFix":
                    preReleaseFix = Convert.ToInt32( attrib.Value, CultureInfo.CurrentCulture );
                    break;

                default:
                    throw new InvalidDataException( $"Unexpected attribute {attrib.Name.LocalName}" );
                }
            }

            // correct malformed values
            if( string.IsNullOrWhiteSpace( preReleaseName ) )
            {
                preReleaseNumber = 0;
                preReleaseFix = 0;
            }

            return new(buildMajor, buildMinor, buildPatch, preReleaseName, preReleaseNumber, preReleaseFix);
        }

        /// <summary>Parse Build version XML from a <see cref="TextReader"/></summary>
        /// <param name="reader">Reader to parse data from</param>
        /// <returns>Parsed version information</returns>
        public static ParsedBuildVersionXml Parse(TextReader reader)
        {
            return Parse(XDocument.Load( reader, LoadOptions.None ));
        }

        /// <summary>Parse XML from an input string</summary>
        /// <param name="xmlTxt">string form of the XML text</param>
        /// <returns>Parsed version information</returns>
        /// <remarks>
        /// This is mostly used for internal testing where the XML is a string constant/literal.
        /// </remarks>
        public static ParsedBuildVersionXml Parse(string xmlTxt)
        {
            using var rdr = new StringReader(xmlTxt);
            return Parse(rdr);
        }

        /// <summary>Parse XML data for Build version information from an XML file</summary>
        /// <param name="path">Path of the file to parse</param>
        /// <returns>Parsed version information</returns>
        public static ParsedBuildVersionXml ParseFile(string path)
        {
            using var rdr = File.OpenText( path );
            return Parse(rdr);
        }
    }
}
