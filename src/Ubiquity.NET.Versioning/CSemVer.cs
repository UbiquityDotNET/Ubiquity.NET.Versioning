// -----------------------------------------------------------------------
// <copyright file="CSemVer.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using Sprache;

using Ubiquity.NET.Versioning.Properties;

namespace Ubiquity.NET.Versioning
{
    /// <summary>Holds a Constrained Semantic Version (CSemVer) value</summary>
    /// <remarks>Based on CSemVer v1.0.0-rc.1</remarks>
    /// <seealso href="https://csemver.org/"/>
    public sealed class CSemVer
        : IParsable<CSemVer>
        , IComparable<CSemVer>
        , IComparisonOperators<CSemVer, CSemVer, bool>
        , IEquatable<CSemVer>
    {
        /// <summary>Initializes a new instance of the <see cref="CSemVer"/> class.</summary>
        public CSemVer( )
            : this( 0, 0, 0 )
        {
        }

        /// <summary>Initializes a new instance of the <see cref="CSemVer"/> class.</summary>
        /// <param name="major">Major version value [0-99999]</param>
        /// <param name="minor">Minor version value [0-49999]</param>
        /// <param name="patch">Patch version value [0-9999]</param>
        /// <param name="preRelVersion">Pre-release version information (if a pre-release build)</param>
        /// <param name="buildMetaData">[Optional]Additional build meta data [default: empty string]</param>
        /// <remarks>
        /// This is used internally when converting from a File Version as those only have a single bit
        /// to indicate if they are a Release/CI build. The rest of the information is lost and therefore
        /// does not participate in ordering.
        /// </remarks>
        public CSemVer( int major
                      , int minor
                      , int patch
                      , PrereleaseVersion? preRelVersion = null
                      , IEnumerable<string>? buildMetaData = null
                      )
        {
            ConstrainedVersion = new SemVer(
                major.ThrowIfOutOfRange( 0, 99999 ),
                minor.ThrowIfOutOfRange( 0, 49999 ),
                patch.ThrowIfOutOfRange( 0, 9999 ),
                preRelVersion?.FormatElements(),
                buildMetaData
                );

            PrereleaseVersion = preRelVersion;
        }

        /// <summary>Gets the Major portion of the core version</summary>
        public int Major => unchecked((int)ConstrainedVersion.Major); // explicitly unchecked as constructor guarantees success

        /// <summary>Gets the Minor portion of the core version</summary>
        public int Minor => unchecked((int)ConstrainedVersion.Minor);

        /// <summary>Gets the Patch portion of the core version</summary>
        public int Patch => unchecked((int)ConstrainedVersion.Patch);

        /// <summary>Gets the Pre-Release version value (if any)</summary>
        public PrereleaseVersion? PrereleaseVersion { get; }

        /// <summary>Gets the build components of the version</summary>
        /// <remarks>
        /// Each component is either an alphanumeric identifier or a sequence of digits
        /// (including leading or all '0'). This collection contains only the identifiers
        /// (no prefix or delimiters).
        /// </remarks>
        public ImmutableArray<string> BuildMeta => ConstrainedVersion.BuildMeta;

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
                return FileVersionQuad.From( orderedNum + 1 ); // ODD numbers reserved for CI versions that are always "post-build"
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

                if(PrereleaseVersion.HasValue)
                {
                    retVal -= MulPatch - 1; // Remove the fix+1 multiplier
                    retVal += PrereleaseVersion.Value.Index * MulName;
                    retVal += PrereleaseVersion.Value.Number * MulNum;
                    retVal += PrereleaseVersion.Value.Fix;
                }

                return retVal;
            }
        }

        /// <summary>Gets a value indicating whether this is a pre-release version</summary>
        public bool IsPrerelease => PrereleaseVersion.HasValue;

        /// <summary>Gets a value indicating whether this is a zero based version</summary>
        public bool IsZero => Major == 0 && Minor == 0 && Patch == 0;

        /// <inheritdoc/>
        public override string ToString( )
        {
            return ConstrainedVersion.ToString();
        }

        /// <inheritdoc/>
        /// <remarks>
        /// <see cref="CSemVer"/> follows ALL of the rules of SemVer ordering EXCEPT
        /// that it is EXPLICITLY using case insensitive comparison for the AlphaNumeric
        /// identifiers in a pre-release list.
        /// </remarks>
        public int CompareTo( CSemVer? other )
        {
            if(ReferenceEquals(this, other))
            {
                return 0;
            }

            if(other is null)
            {
                return 1;
            }

            // CSemVer always uses case insensitive comparisons, but otherwise follows the
            // ordering rules of SemVer.
            return SemVerComparer.SemVer.Compare(ConstrainedVersion, other.ConstrainedVersion);
        }

        /// <inheritdoc/>
        public bool Equals( CSemVer? other )
        {
            return ReferenceEquals(this, other)
                || (other is not null && SemVerComparer.SemVer.Compare(ConstrainedVersion, other.ConstrainedVersion) == 0);
        }

        /// <inheritdoc/>
        public override bool Equals( object? obj )
        {
            return Equals(obj as CSemVer);
        }

        /// <inheritdoc/>
        public override int GetHashCode( )
        {
            return ConstrainedVersion.GetHashCode();
        }

        /// <inheritdoc/>
        public static bool operator >( CSemVer left, CSemVer right ) => left.CompareTo(right) > 0;

        /// <inheritdoc/>
        public static bool operator >=( CSemVer left, CSemVer right ) => left.CompareTo(right) >= 0;

        /// <inheritdoc/>
        public static bool operator <( CSemVer left, CSemVer right ) => left.CompareTo(right) < 0;

        /// <inheritdoc/>
        public static bool operator <=( CSemVer left, CSemVer right ) => left.CompareTo(right) <= 0;

        /// <inheritdoc/>
        public static bool operator ==( CSemVer? left, CSemVer? right ) => Equals(left, right);

        /// <inheritdoc/>
        public static bool operator !=( CSemVer? left, CSemVer? right ) => !Equals(left, right);

        private readonly SemVer ConstrainedVersion;

        /// <summary>Tries to parse a <see cref="SemVer"/> as a <see cref="CSemVer"/></summary>
        /// <param name="ver">Version to convert</param>
        /// <param name="result">Result or default if not convertible</param>
        /// <param name="reason">Reason that conversion is not allowed (or <see langword="null"/> if it is)</param>
        /// <returns><see langword="true"/> if the conversion is performed or <see langword="false"/> if not (<paramref name="reason"/> will hold reason it is not successful)</returns>
        /// <remarks>
        /// While EVERY <see cref="CSemVer"/> conforms to valid <see cref="SemVer"/> the reverse is not always true.
        /// This method attempts to make a conversion using the classic try pattern with the inclusion of a string
        /// that explains the reason for any failures. This is useful in debugging or for creating wrappers that will
        /// throw an exception.
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
                reason = new FormatException(Resources.CSemVer_pre_release_supports_no_more_than_3_components_0.Format( "[CSemVer.2]" ));
                return false;
            }

            // CSemVer.3 unchecked here as SemVer is already parsed or constructed so not relevant
            // CSemVer.4
            if(ver.Major.IsOutOfRange( 0, 99999 ))
            {
                reason = new FormatException(Resources.value_0_must_be_in_range_1_2.Format( "CSemVer.Major", "[0-99999]", "[CSemVer.4]" ));
                return false;
            }

            // CSemVer.5
            if(ver.Minor.IsOutOfRange( 0, 49999 ))
            {
                reason = new FormatException(Resources.value_0_must_be_in_range_1_2.Format( "CSemVer.Minor", "[0-49999]", "[CSemVer.5]" ));
                return false;
            }

            if(ver.Patch.IsOutOfRange( 0, 9999 ))
            {
                reason = new FormatException(Resources.value_0_must_be_in_range_1_2.Format( "CSemVer.Patch", "[0-9999]", "[CSemVer.6]" ));
                return false;
            }

            IResult<PrereleaseVersion> preRel = Versioning.PrereleaseVersion.TryParseFrom( ver.PreRelease );
            if(preRel.Failed(out reason))
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
        public static CSemVer From( UInt64 fileVersion, IReadOnlyCollection<string>? buildMetaData = null )
        {
            return (fileVersion & 1) == 1
                ? FromOrderedVersion( fileVersion >> 1, buildMetaData )
                : throw new ArgumentException( Resources.odd_file_versions_are_reserved_for_CI );
        }

        /// <summary>Converts a CSemVer ordered version integral value (UInt64) into a full <see cref="CSemVer"/></summary>
        /// <param name="orderedVersion">The ordered version value</param>
        /// <param name="buildMetaData">Optional build meta data value for the version</param>
        /// <returns><see cref="CSemVer"/> corresponding to the ordered version number provided</returns>
        public static CSemVer FromOrderedVersion( UInt64 orderedVersion, IReadOnlyCollection<string>? buildMetaData = null )
        {
            buildMetaData ??= [];

            // This effectively reverses the math used in computing the ordered version.
            UInt64 accumulator = orderedVersion;
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
        /// <returns><see cref="CSemVer"/></returns>
        public static CSemVer From( string buildVersionXmlPath, IReadOnlyCollection<string>? buildMeta )
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
        public static CSemVer Parse( string s, IFormatProvider? provider )
        {
            return TryParse(s, out CSemVer? retVal, out Exception? ex) ? retVal : throw ex;
        }

        /// <inheritdoc/>
        public static bool TryParse( [NotNullWhen( true )] string? s, IFormatProvider? provider, [MaybeNullWhen( false )] out CSemVer result )
        {
            return TryParse(s, out result, out _);
        }

        /// <summary>Applies try pattern to attempt parsing a <see cref="CSemVer"/> from a string</summary>
        /// <param name="s">Raw string to parse from</param>
        /// <param name="result">Resulting version or default if parse is successful</param>
        /// <param name="reason">Reason for failure to parse (as an <see cref="Exception"/>)</param>
        /// <returns><see langword="true"/> if parse is successful or <see langword="false"/> if not</returns>
        public static bool TryParse(
            [NotNullWhen( true )] string? s,
            [MaybeNullWhen( false )] out CSemVer result,
            [MaybeNullWhen(true)] out Exception reason
            )
        {
            if(string.IsNullOrWhiteSpace(s))
            {
                result = default;
                reason = new ArgumentException(Resources.value_is_null_or_whitespace, nameof(s));
                return false;
            }

            if(!SemVer.TryParse( s, out SemVer? semVer, out reason))
            {
                result = default;
                return false;
            }

            // OK as a SemVer, so try and see if that conforms to a constrained form.
            return TryFrom(semVer, out result, out reason);
        }

        private const ulong MulNum = 100;
        private const ulong MulName = MulNum * 100;
        private const ulong MulPatch = (MulName * 8) + 1;
        private const ulong MulMinor = MulPatch * 10000;
        private const ulong MulMajor = MulMinor * 50000;
    }
}
