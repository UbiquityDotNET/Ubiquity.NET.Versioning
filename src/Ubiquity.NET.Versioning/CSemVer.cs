// -----------------------------------------------------------------------
// <copyright file="CSemVer.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Globalization;
using System.Linq;

namespace Ubiquity.NET.Versioning
{
    /// <summary>Holds a Constrained Semantic Version (CSemVer) value</summary>
    /// <remarks>Based on CSemVer v1.0.0-rc.1</remarks>
    /// <seealso href="https://csemver.org/"/>
    public class CSemVer
        : IFormattable
        , IComparable<CSemVer>
        , IEquatable<CSemVer>
    {
        /// <summary>Initializes a new instance of the <see cref="CSemVer"/> class</summary>
        /// <param name="major">Major version value [0-99999]</param>
        /// <param name="minor">Minor version value [0-49999]</param>
        /// <param name="patch">Patch version value [0-9999]</param>
        /// <param name="ciBuildInfo">CI build information for this build</param>
        /// <param name="preRelVersion">Pre-release version information (if a pre-release build)</param>
        /// <param name="buildMetaData">[Optional]Additional build meta data [default: empty string]</param>
        public CSemVer( int major
                      , int minor
                      , int patch
                      , PrereleaseVersion preRelVersion = default
                      , CiBuildInfo ciBuildInfo = default
                      , string buildMetaData = ""
                      )
            : this( major, minor, patch, isCIBuild: ciBuildInfo.IsValid, preRelVersion, buildMetaData )
        {
            CiBuildInfo = ciBuildInfo;
        }

        /// <summary>Gets the Major version value</summary>
        public int Major { get; }

        /// <summary>Gets the Minor version value</summary>
        public int Minor { get; }

        /// <summary>Gets the Patch version value</summary>
        public int Patch { get; }

        /// <summary>Gets the Pre-Release version value (if any)</summary>
        public PrereleaseVersion PrereleaseVersion { get; }

        /// <summary>Gets the CI Build info (if any)</summary>
        public CiBuildInfo CiBuildInfo { get; }

        /// <summary>Gets the build meta data (or an empty string)</summary>
        public string BuildMetaData { get; }

        /// <summary>Gets the <see cref="FileVersionQuad"/> representation of this <see cref="CSemVer"/></summary>
        /// <remarks>
        /// Since a <see cref="FileVersionQuad"/> is entirely numeric the conversion is somewhat "lossy" but does
        /// NOT lose any relation to other versions converted. That, is the loss does not include any information
        /// that impacts build version sort ordering. (any data lost is ignored for sort ordering anyway)
        /// </remarks>
        public FileVersionQuad FileVersion
        {
            get
            {
                ulong orderedNum = OrderedVersion << 1;
                return FileVersionQuad.From( IsCIBuild ? orderedNum + 1 : orderedNum );
            }
        }

        /// <summary>Gets the CSemVer ordered version value of the version</summary>
        /// <remarks>
        /// This is similar to an integral representation of the <see cref="FileVersion"/>
        /// except that it does NOT include any information about whether it is a CI build
        /// or not.
        /// </remarks>
        public ulong OrderedVersion
        {
            get
            {
                ulong retVal = ((ulong)Major * MulMajor) + ((ulong)Minor * MulMinor) + (((ulong)Patch + 1) * MulPatch);

                if(IsPrerelease)
                {
                    retVal -= MulPatch - 1; // Remove the fix+1 multiplier
                    retVal += (ulong)PrereleaseVersion.Index * MulName;
                    retVal += (ulong)PrereleaseVersion.Number * MulNum;
                    retVal += (ulong)PrereleaseVersion.Fix;
                }

                return retVal;
            }
        }

        /// <summary>Gets a value indicating whether this version represents a CI build</summary>
        /// <remarks>
        /// <para>Information regarding CI Builds are not included in the numeric representation (except a File Version "quad").
        /// This is used to indicate if a build represents a CI build or not.</para>
        /// <para>The CI build information is contained in the optional <see cref="CiBuildInfo"/> property. However, if this
        /// instance was produced from a purely numeric value then such information is lost though it is possible to indicate
        /// a CI build using the low bit of the revision part of a <see cref="FileVersionQuad"/>.
        /// </para>
        /// </remarks>
        public bool IsCIBuild { get; }

        /// <summary>Gets a value indicating whether this is a pre-release version</summary>
        public bool IsPrerelease => PrereleaseVersion.IsValid;

        #region Comparison operators

        /// <inheritdoc/>
        public bool Equals( CSemVer? other ) => other is not null && CompareTo(other) == 0;

        /// <inheritdoc/>
        public override bool Equals( object? obj )
        {
            return Equals(obj as CSemVer);
        }

        /// <inheritdoc/>
        public override int GetHashCode( )
        {
            return HashCode.Combine(Major, Minor, Patch, PrereleaseVersion, CiBuildInfo, BuildMetaData);
        }

        /// <inheritdoc/>
        public int CompareTo( CSemVer? other )
        {
            // By definition, any object compares greater than null, and two null references compare equal to each other.
            return other is null ? 1 : FileVersion.CompareTo( other.FileVersion );
        }

        /// <summary>Compares two <see cref="CSemVer"/> values (Less than)</summary>
        /// <param name="left">Left hand side of the operation</param>
        /// <param name="right">Right hand side of the operation</param>
        /// <returns>Result of the comparison</returns>
        public static bool operator <( CSemVer left, CSemVer right ) => left.CompareTo( right ) < 0;

        /// <summary>Compares two <see cref="CSemVer"/> values (Less than or equal)</summary>
        /// <param name="left">Left hand side of the operation</param>
        /// <param name="right">Right hand side of the operation</param>
        /// <returns>Result of the comparison</returns>
        public static bool operator <=( CSemVer left, CSemVer right ) => left.CompareTo( right ) <= 0;

        /// <summary>Compares two <see cref="CSemVer"/> values (Greater than)</summary>
        /// <param name="left">Left hand side of the operation</param>
        /// <param name="right">Right hand side of the operation</param>
        /// <returns>Result of the comparison</returns>
        public static bool operator >( CSemVer left, CSemVer right ) => left.CompareTo( right ) > 0;

        /// <summary>Compares two <see cref="CSemVer"/> values (Greater than or equal)</summary>
        /// <param name="left">Left hand side of the operation</param>
        /// <param name="right">Right hand side of the operation</param>
        /// <returns>Result of the comparison</returns>
        public static bool operator >=( CSemVer left, CSemVer right ) => left.CompareTo( right ) >= 0;

        /// <summary>Compares two <see cref="CSemVer"/> values (Equals)</summary>
        /// <param name="left">Left hand side of the operation</param>
        /// <param name="right">Right hand side of the operation</param>
        /// <returns>Result of the comparison</returns>
        public static bool operator ==( CSemVer? left, CSemVer? right ) => ReferenceEquals(left, right) || (left is not null && left.Equals(right));

        /// <summary>Compares two <see cref="CSemVer"/> values (Not Equal)</summary>
        /// <param name="left">Left hand side of the operation</param>
        /// <param name="right">Right hand side of the operation</param>
        /// <returns>Result of the comparison</returns>
        public static bool operator !=( CSemVer? left, CSemVer? right ) => !(left == right);
        #endregion

        /// <inheritdoc/>
        public override string ToString( ) => ToString( null, null );

        /// <summary>Handles formatted string conversion</summary>
        /// <param name="format">Format string (See remarks section for details of values supported)</param>
        /// <param name="formatProvider">Format provider. [ignored, formatting is well defined externally and not subject to localization]</param>
        /// <returns>String form of the version information</returns>
        /// <remarks>
        /// The values supported for <paramref name="format"/> are: <br/>
        /// M - Include Meta data (if present) <br/>
        /// S - Use short names for CI information <br/>
        /// Any combination/ordering of those formats is valid.
        /// </remarks>
        /// <exception cref="ArgumentException">The <paramref name="format"/> value specified, is invalid</exception>
        /// <seealso href="https://csemver.org/">Formal documentation of Constrained Semantic Versions</seealso>
        public string ToString( string? format, IFormatProvider? formatProvider )
        {
            format ??= "M";

            // Format provider is ignored; Representation is well defined externally and not dependent on culture
            // formatProvider ??= CultureInfo.InvariantCulture;
            if(format.Length > 2 || format.Any( c => c != 'M' && c != 'S' ))
            {
                throw new ArgumentException( $"Invalid format '{format}'", nameof( format ) );
            }

            bool includeMetadata = format.Contains('M', StringComparison.Ordinal);
            bool useShortNames = format.Contains('S', StringComparison.Ordinal);

            var bldr = new System.Text.StringBuilder( );

            bldr.AppendFormat( CultureInfo.InvariantCulture, "{0}.{1}.{2}", Major, Minor, Patch );

            if(IsPrerelease)
            {
                bldr.AppendFormat( CultureInfo.InvariantCulture, useShortNames ? "{0:S}" : "{0:F}", PrereleaseVersion );
            }

            if(CiBuildInfo.IsValid)
            {
                bldr.AppendFormat(CultureInfo.InvariantCulture, IsPrerelease ? "{0:P}" : "{0:R}", CiBuildInfo);
            }

            if(!string.IsNullOrWhiteSpace( BuildMetaData ) && includeMetadata)
            {
                bldr.AppendFormat( CultureInfo.InvariantCulture, $"+{BuildMetaData}" );
            }

            return bldr.ToString();
        }

        /// <summary>Converts a file version form (as a <see cref="UInt64"/>) of a CSemVer into a full <see cref="CSemVer"/></summary>
        /// <param name="fileVersion">File version as an unsigned 64 bit value</param>
        /// <param name="buildMetaData">Optional build meta data value for the version</param>
        /// <returns><see cref="CSemVer"/> for the specified file version</returns>
        /// <remarks>
        /// <para>A file version is a quad of 4 <see cref="UInt16"/> values. This is convertible to a <see cref="UInt64"/> in the following
        /// pattern:
        /// (bits are numbered with MSB as the highest numeric value [Actual ordering depends on platform endianess])
        /// <list type="table">
        ///     <listheader><term>Field</term><term>Description</term></listheader>
        ///     <item><term>bits 48-63</term><description> Major part of Build number</description></item>
        ///     <item><term>bits 32-47</term><description> Minor part of Build number</description></item>
        ///     <item><term>bits 16-31</term><description> Build part of Build number</description></item>
        ///     <item><term>bits 0-15</term><description> Revision part of Build number (Odd Numbers indicate a CI build</description></item>
        /// </list>
        /// </para>
        /// <para>A file version cast as a <see cref="UInt64"/> is <i><b>NOT</b></i> the same as an Ordered version number. The file version
        /// includes a "bit" for the status as a CI Build. Thus a "file version" as a <see cref="UInt64"/> is the ordered version shifted
        /// left by one bit and the LSB indicates if it is a CI build</para>
        /// </remarks>
        public static CSemVer From( UInt64 fileVersion, string? buildMetaData = null )
        {
            bool isCIBuild = (fileVersion & 1) == 1;
            return FromOrderedVersion(fileVersion >> 1, isCIBuild, buildMetaData); // Drop the CI bit to get the "ordered" number
        }

        /// <summary>Converts a CSemVer ordered version integral value (UInt64) into a full <see cref="CSemVer"/></summary>
        /// <param name="orderedVersion">The ordered version value</param>
        /// <param name="isCIBuild">Flag to indicate whether this is a CI build or not [default: <see langword="false"/></param>
        /// <param name="buildMetaData">Optional build meta data value for the version</param>
        /// <returns><see cref="CSemVer"/> corresponding to the ordered version number provided</returns>
        public static CSemVer FromOrderedVersion(UInt64 orderedVersion, bool isCIBuild = false, string? buildMetaData = null)
        {
            // This effectively reverses the math used in computing the ordered version.
            buildMetaData ??= string.Empty;

            UInt64 accumulator = orderedVersion;
            UInt64 preRelPart = accumulator % MulPatch;
            PrereleaseVersion preRelVersion = default;
            if( preRelPart != 0)
            {
                preRelPart -= 1;

                Int32 index = (Int32)(preRelPart / MulName);
                preRelPart %= MulName;

                Int32 number = (Int32)(preRelPart / MulNum);
                preRelPart %= MulNum;

                Int32 fix = (Int32)preRelPart;
                preRelVersion = new PrereleaseVersion(index, number, fix);
            }
            else
            {
                accumulator -= MulPatch;
            }

            Int32 major = (Int32)(accumulator / MulMajor);
            accumulator %= MulMajor;

            Int32 minor = (Int32)(accumulator / MulMinor);
            accumulator %= MulMinor;

            Int32 patch = (Int32)(accumulator / MulPatch);

            return new CSemVer(major, minor, patch, isCIBuild, preRelVersion);
        }

        /// <summary>Factory method to create a <see cref="CSemVer"/> from information available as part of a build</summary>
        /// <param name="buildVersionXmlPath">Path to the BuildVersion XML data for the repository</param>
        /// <param name="timeStamp">TimeStamp of the build</param>
        /// <param name="ciBuildName">CI Build name for the build</param>
        /// <param name="buildMeta">Additional Build meta data for the build</param>
        /// <returns><see cref="CSemVer"/></returns>
        public static CSemVer From( string buildVersionXmlPath, DateTime timeStamp, string ciBuildName, string buildMeta )
        {
            string ciBuildIndex = timeStamp.ToBuildIndex( );
            var ciBuildInfo = new CiBuildInfo(ciBuildIndex, ciBuildName);
            var parsedBuildVersionXml = ParsedBuildVersionXml.ParseFile( buildVersionXmlPath );

            PrereleaseVersion preReleaseVersion = default;
            if(!string.IsNullOrWhiteSpace( parsedBuildVersionXml.PreReleaseName ))
            {
                preReleaseVersion = new PrereleaseVersion( parsedBuildVersionXml.PreReleaseName
                                                         , parsedBuildVersionXml.PreReleaseNumber
                                                         , parsedBuildVersionXml.PreReleaseFix
                                                         );
            }

            return new CSemVer( parsedBuildVersionXml.BuildMajor
                              , parsedBuildVersionXml.BuildMinor
                              , parsedBuildVersionXml.BuildPatch
                              , preReleaseVersion
                              , ciBuildInfo
                              , buildMeta
                              );
        }

        /// <summary>Initializes a new instance of the <see cref="CSemVer"/> class.</summary>
        /// <param name="major">Major version value [0-99999]</param>
        /// <param name="minor">Minor version value [0-49999]</param>
        /// <param name="patch">Patch version value [0-9999]</param>
        /// <param name="isCIBuild">Indicates whether this is a CI build</param>
        /// <param name="preRelVersion">Pre-release version information (if a pre-release build)</param>
        /// <param name="buildMetaData">[Optional]Additional build meta data [default: empty string]</param>
        /// <remarks>
        /// This is used internally when converting from a File Version as those only have a single bit
        /// to indicate they are a CI build. The rest of the information, which doesn't participate in sort
        /// ordering, is lost.
        /// </remarks>
        private CSemVer( int major
                       , int minor
                       , int patch
                       , bool isCIBuild
                       , PrereleaseVersion preRelVersion = default
                       , string buildMetaData = ""
                       )
        {
            Major = major.ThrowIfOutOfRange(0, 99999 );
            Minor = minor.ThrowIfOutOfRange(0, 49999 );
            Patch = patch.ThrowIfOutOfRange(0, 9999 );
            PrereleaseVersion = preRelVersion;
            BuildMetaData = buildMetaData.ThrowIfNullOrLongerThan(20);
            IsCIBuild = isCIBuild;
        }

        private const ulong MulNum = 100;
        private const ulong MulName = MulNum * 100;
        private const ulong MulPatch = (MulName * 8) + 1;
        private const ulong MulMinor = MulPatch * 10000;
        private const ulong MulMajor = MulMinor * 50000;
    }
}
