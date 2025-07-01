// -----------------------------------------------------------------------
// <copyright file="CSemVer.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

using Sprache;

using Ubiquity.NET.Versioning.Properties;

namespace Ubiquity.NET.Versioning
{
    /// <summary>Holds a Constrained Semantic Version (CSemVer) value</summary>
    /// <remarks>Based on CSemVer v1.0.0-rc.1</remarks>
    /// <seealso href="https://csemver.org/"/>
    public sealed class CSemVer
        : SemVer
        , IParsable<CSemVer>
    {
        /// <summary>Initializes a new instance of the <see cref="CSemVer"/> class.</summary>
        /// <remarks>
        /// Default constructs a <see cref="CSemVer"/> with a version of 0.0.0 no pre-release nor build meta data
        /// </remarks>
        public CSemVer( )
            : this( 0, 0, 0 )
        {
        }

        /// <summary>Initializes a new instance of the <see cref="CSemVer"/> class.</summary>
        /// <param name="major">Major version value [0-99999]</param>
        /// <param name="minor">Minor version value [0-49999]</param>
        /// <param name="patch">Patch version value [0-9999]</param>
        /// <param name="preRelVersion">Pre-release version information (if a pre-release build)</param>
        /// <param name="buildMetaData">Any additional build meta data [default: empty string]</param>
        public CSemVer( int major
                      , int minor
                      , int patch
                      , PrereleaseVersion? preRelVersion = null
                      , ImmutableArray<string> buildMetaData = default
                      )
            : base(
                major.ThrowIfOutOfRange( 0, 99999 ),
                minor.ThrowIfOutOfRange( 0, 49999 ),
                patch.ThrowIfOutOfRange( 0, 9999 ),
                AlphaNumericOrdering.CaseInsensitive,
                preRelVersion?.FormatElements().ToImmutableArray() ?? default,
                buildMetaData
                )
        {
            PrereleaseVersion = preRelVersion;
        }

        /// <summary>Gets the Pre-Release version value (if any)</summary>
        public PrereleaseVersion? PrereleaseVersion { get; }

        /// <summary>Gets the <see cref="FileVersionQuad"/> representation of this <see cref="CSemVer"/></summary>
        /// <remarks>
        /// Since a <see cref="FileVersionQuad"/> is entirely numeric the conversion is somewhat "lossy" but does
        /// NOT lose any relation to other released versions converted. That, is the loss does not include any CI
        /// information, only that it was a CI build. Two CI builds with the same base version will produce the
        /// same value! CI builds are not intended for long term stability and this is not a bug but a design of
        /// how CSemVer (and CSemVer-CI) work to produce a <see cref="FileVersionQuad"/>.
        /// </remarks>
        public FileVersionQuad FileVersion => new( OrderedVersion, isCiBuild: false );

        /// <summary>Gets the CSemVer ordered version value of the version</summary>
        /// <remarks>
        /// This is similar to an integral representation of the <see cref="FileVersion"/>
        /// except that it does NOT include any information about whether it is a CI build
        /// or not.
        /// </remarks>
        public Int64 OrderedVersion
        {
            get
            {
                UInt64 retVal = ((UInt64)Major * MulMajor) + ((UInt64)Minor * MulMinor) + (((UInt64)Patch + 1) * MulPatch);

                if(PrereleaseVersion.HasValue)
                {
                    retVal -= MulPatch - 1; // Remove the fix+1 multiplier
                    retVal += PrereleaseVersion.Value.Index * MulName;
                    retVal += PrereleaseVersion.Value.Number * MulNum;
                    retVal += PrereleaseVersion.Value.Fix;
                }

                return checked((Int64)retVal);
            }
        }

        /// <summary>Gets a value indicating whether this is a pre-release version</summary>
        public bool IsPrerelease => PrereleaseVersion.HasValue;

        /// <summary>Gets a value indicating whether this is a zero based version</summary>
        public bool IsZero => Major == 0 && Minor == 0 && Patch == 0;

        /// <summary>Tries to parse a <see cref="SemVer"/> as a <see cref="CSemVer"/></summary>
        /// <param name="ver">Version to convert</param>
        /// <param name="result">Result or default if not convertible</param>
        /// <param name="reason">Reason that conversion is not allowed (or <see langword="null"/> if it is)</param>
        /// <returns><see langword="true"/> if the conversion is performed or <see langword="false"/> if not (<paramref name="reason"/> will hold reason it is not successful)</returns>
        /// <remarks>
        /// While EVERY <see cref="CSemVer"/> conforms to valid <see cref="SemVer"/> the reverse is not always true.
        /// This method attempts to make a conversion using the classic try pattern with the inclusion of an exception
        /// that explains the reason for any failures. This is useful in debugging or for creating wrappers that will
        /// throw the exception.
        /// </remarks>
        public static bool TryFrom(
            SemVer ver,
            [MaybeNullWhen( false )] out CSemVer result,
            [MaybeNullWhen( true )] out Exception reason
            )
        {
            result = default;
            reason = default;

            // CSemVer.1 - covered by input version
            // CSemVer.2 (Partial: max is 3 components)
            if(ver.PreRelease.Length.IsOutOfRange( 0, 3 ))
            {
                reason = new FormatException( Resources.CSemVer_pre_release_supports_no_more_than_3_components_0.Format( "[CSemVer.2]" ) );
                return false;
            }

            // CSemVer.3 unchecked here as SemVer is already parsed or constructed so not relevant
            // CSemVer.4
            if(ver.Major.IsOutOfRange( 0, 99999 ))
            {
                reason = new FormatException( Resources.value_0_must_be_in_range_1_2.Format( "CSemVer.Major", "[0-99999]", "[CSemVer.4]" ) );
                return false;
            }

            // CSemVer.5
            if(ver.Minor.IsOutOfRange( 0, 49999 ))
            {
                reason = new FormatException( Resources.value_0_must_be_in_range_1_2.Format( "CSemVer.Minor", "[0-49999]", "[CSemVer.5]" ) );
                return false;
            }

            if(ver.Patch.IsOutOfRange( 0, 9999 ))
            {
                reason = new FormatException( Resources.value_0_must_be_in_range_1_2.Format( "CSemVer.Patch", "[0-9999]", "[CSemVer.6]" ) );
                return false;
            }

            IResult<PrereleaseVersion> preRel = Versioning.PrereleaseVersion.TryParseFrom( ver.PreRelease );
            if(preRel.Failed( out reason ))
            {
                return false;
            }

            try
            {
                result = new CSemVer( (int)ver.Major, (int)ver.Minor, (int)ver.Patch, preRel.Value );
                return true;
            }
            catch(ArgumentException ex)
            {
                reason = ex;
                return false;
            }
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
        /// left by one bit and the LSB indicates if it is a CI build or release</para>
        /// </remarks>
        public static CSemVer From( FileVersionQuad fileVersion, ImmutableArray<string> buildMetaData = default )
        {
            // Drop the CI bit to get an ordered version
            return !fileVersion.IsCiBuild
                ? FromOrderedVersion( fileVersion.ToOrderedVersion(), buildMetaData )
                : throw new ArgumentException( "FileVersionQuad for a CI build cannot be used to create a non CI version", nameof( fileVersion ) );
        }

        /// <summary>Converts a CSemVer ordered version integral value (UInt64) into a full <see cref="CSemVer"/></summary>
        /// <param name="orderedVersion">The ordered version value</param>
        /// <param name="buildMetaData">Optional build meta data value for the version</param>
        /// <returns>Version corresponding to the ordered version number provided</returns>
        public static CSemVer FromOrderedVersion( Int64 orderedVersion, ImmutableArray<string> buildMetaData = default )
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan( orderedVersion, MaxOrderedVersion );

            // This effectively reverses the math used in computing the ordered version.
            UInt64 accumulator = (UInt64)orderedVersion;
            UInt64 preRelPart = accumulator % MulPatch;
            PrereleaseVersion? preRelVersion = null;
            if(preRelPart != 0)
            {
                preRelPart -= 1;

                Int32 index = (Int32)(preRelPart / MulName);
                preRelPart %= MulName;

                Int32 number = (Int32)(preRelPart / MulNum);
                preRelPart %= MulNum;

                Int32 fix = (Int32)preRelPart;
                preRelVersion = new PrereleaseVersion( checked((byte)index), checked((byte)number), checked((byte)fix) );
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

            return new CSemVer( major, minor, patch, preRelVersion, buildMetaData );
        }

        /// <summary>Factory method to create a <see cref="CSemVer"/> from information available as part of a build</summary>
        /// <param name="buildVersionXmlPath">Path to the BuildVersion XML data for the repository</param>
        /// <param name="buildMeta">Additional Build meta data for the build</param>
        /// <returns>Version information parsed from the build XML</returns>
        public static CSemVer From( string buildVersionXmlPath, ImmutableArray<string> buildMeta )
        {
            var parsedBuildVersionXml = ParsedBuildVersionXml.ParseFile( buildVersionXmlPath );

            PrereleaseVersion preReleaseVersion = default;
            if(!string.IsNullOrWhiteSpace( parsedBuildVersionXml.PreReleaseName ))
            {
                preReleaseVersion = new PrereleaseVersion( parsedBuildVersionXml.PreReleaseName
                                                         , checked((byte)parsedBuildVersionXml.PreReleaseNumber)
                                                         , checked((byte)parsedBuildVersionXml.PreReleaseFix)
                                                         );
            }

            return new CSemVer( parsedBuildVersionXml.BuildMajor
                              , parsedBuildVersionXml.BuildMinor
                              , parsedBuildVersionXml.BuildPatch
                              , preReleaseVersion
                              , buildMeta
                              );
        }

        /// <inheritdoc/>
        public static new CSemVer Parse( string s, IFormatProvider? provider )
        {
            provider.ThrowIfCaseSensitive();
            return TryParse( s, out CSemVer? retVal, out Exception? ex ) ? retVal : throw ex;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// <paramref name="provider"/> is ALWAYS ignored by this implementation. The format is defined by the spec and independent of culture.
        /// </remarks>
        public static bool TryParse( [NotNullWhen( true )] string? s, IFormatProvider? provider, [MaybeNullWhen( false )] out CSemVer result )
        {
            provider.ThrowIfCaseSensitive();
            return TryParse( s, out result, out _ );
        }

        /// <summary>Applies try pattern to attempt parsing a <see cref="CSemVer"/> from a string</summary>
        /// <param name="s">Raw string to parse from</param>
        /// <param name="result">Resulting version or default if parse is successful</param>
        /// <param name="reason">Reason for failure to parse (as an <see cref="Exception"/>)</param>
        /// <returns><see langword="true"/> if parse is successful or <see langword="false"/> if not</returns>
        [SuppressMessage( "Style", "IDE0002:Simplify Member Access", Justification = "More explicit this way" )]
        public static bool TryParse(
            [NotNullWhen( true )] string? s,
            [MaybeNullWhen( false )] out CSemVer result,
            [MaybeNullWhen( true )] out Exception reason
            )
        {
            if(string.IsNullOrWhiteSpace( s ))
            {
                result = default;
                reason = new ArgumentException( Resources.value_is_null_or_whitespace, nameof( s ) );
                return false;
            }

            if(!SemVer.TryParse( s, SemVerFormatProvider.CaseInsensitive, out SemVer? semVer, out reason ))
            {
                result = default;
                return false;
            }

            // OK as a SemVer, so try and see if that conforms to a constrained form.
            return TryFrom( semVer, out result, out reason );
        }

        /// <summary>Maximum value of an ordered version number</summary>
        /// <remarks>
        /// This represents a version of v99999.49999.9999. No CSemVer greater than
        /// this value is possible. Thus, no CI build is based on this version either.
        /// </remarks>
        public const Int64 MaxOrderedVersion = 4000050000000000000L;

        // v5.0.4 => 200002500400005;
        // v5.0.5 => 200002500480006;  (previous + MulPatch!)

        internal const ulong MulNum = 100;
        internal const ulong MulName = MulNum * 100;        // 10,000
        internal const ulong MulPatch = (MulName * 8) + 1;  // 80,001
        internal const ulong MulMinor = MulPatch * 10000;   // 800,010,000
        internal const ulong MulMajor = MulMinor * 50000;   // 4,000,050,000,000
    }
}
