// -----------------------------------------------------------------------
// <copyright file="CSemVerCI.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;

using Ubiquity.NET.Versioning.Properties;

namespace Ubiquity.NET.Versioning
{
    /// <summary>Represents a CSemVer-CI value</summary>
    /// <remarks>
    /// This class represents a CSemVer-CI which is a specialized form
    /// of <see cref="SemVer"/>. A CSemVer-CI is Continuous Integration (CI)
    /// information attached to a base build number. Thus, it is a post-release
    /// number unless the base is (0.0.0) where it is a pre-release as no release of the
    /// product exists to base it on.
    /// </remarks>
    /// <seealso href="https://csemver.org"/>
    public sealed partial class CSemVerCI
        : IParsable<CSemVerCI>
        , IComparable<CSemVerCI>
        , IComparisonOperators<CSemVerCI, CSemVerCI, bool>
        , IEquatable<CSemVerCI>
    {
        /// <summary>Initializes a new instance of the <see cref="CSemVerCI"/> class as a "CSemVer-CI ZeroTimed' value</summary>
        /// <param name="index">Index of this CI build</param>
        /// <param name="name">Name of this CI build</param>
        /// <param name="buildMeta">Optional Build meta</param>
        /// <seealso href="https://csemver.org">CSemVer-CI §2-5</seealso>
        public CSemVerCI( string index, string name, IEnumerable<string>? buildMeta = null )
            : this( new CSemVer(0, 0, 0, null, buildMeta), index, name)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="CSemVerCI"/> class.</summary>
        /// <param name="baseBuild">Base build version this CI version is based on</param>
        /// <param name="index">Index for this CI build</param>
        /// <param name="name">Name for this CI build</param>
        /// <remarks>
        /// The <paramref name="baseBuild"/> is assumed a lower sort order than any CI build and
        /// therefore should be at least Revision+1 of an actual release. This constructor does not
        /// (and cannot) VERIFY such a thing.
        /// </remarks>
        /// <seealso href="https://csemver.org">CSemVer-CI §2-6</seealso>
        public CSemVerCI( CSemVer baseBuild, string index, string name )
        {
            ArgumentNullException.ThrowIfNull(baseBuild);
            ArgumentException.ThrowIfNullOrWhiteSpace(index);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            string[] ciSequence = [baseBuild.IsPrerelease ? "ci" : "-ci", index, name ];

            // CSemVer-CI §2; Only zero timed gets build meta... [Go Figure!]
            // [Though the spec is rather ambiguous on the point.]
            // This seems pointless, so the default is to allow it, but switch can enforce strict compliance
            if (AppContextSwitches.CSemVerCIOnlySupportsBuildMetaOnZeroTimedVersions && !baseBuild.IsZero && !baseBuild.BuildMeta.IsDefaultOrEmpty)
            {
                throw new ArgumentException("non-zero timed base build contains meta data", nameof(baseBuild));
            }

            ConstrainedVersion = new SemVer(
                baseBuild.Major,
                baseBuild.Minor,
                baseBuild.Patch,
                baseBuild.PrereleaseVersion?.FormatElements().Concat( ciSequence ) ?? ciSequence,
                baseBuild.BuildMeta
                );

            // CSemVer-CI §2
            if(baseBuild.IsZero && baseBuild.IsPrerelease)
            {
                throw new ArgumentException(Resources.csemver_ci_zerotimed_versions_cannot_use_a_pre_release_as_the_base_build, nameof(baseBuild));
            }

            PrereleaseVersion = baseBuild.PrereleaseVersion;
            Index = index.ThrowIfNotMatch( CiBuildIdRegex );
            Name = name.ThrowIfNotMatch( CiBuildIdRegex );
        }

        /// <summary>Gets the Major portion of the core version</summary>
        public int Major => unchecked((int)ConstrainedVersion.Major); // explicitly unchecked as constructor guarantees success

        /// <summary>Gets the Minor portion of the core version</summary>
        public int Minor => unchecked((int)ConstrainedVersion.Minor);

        /// <summary>Gets the Patch portion of the core version</summary>
        public int Patch => unchecked((int)ConstrainedVersion.Patch);

        /// <summary>Gets the pre-release components of the version</summary>
        /// <remarks>
        /// Each component is either an alphanumeric identifier or a numeric identifier.
        /// This collection contains only the identifiers (no prefix or delimiters).
        /// </remarks>
        public ImmutableArray<string> PreRelease => ConstrainedVersion.PreRelease;

        /// <summary>Gets the build components of the version</summary>
        /// <remarks>
        /// Each component is either an alphanumeric identifier or a sequence of digits
        /// (including leading or all '0'). This collection contains only the identifiers
        /// (no prefix or delimiters).
        /// </remarks>
        public ImmutableArray<string> BuildMeta => ConstrainedVersion.BuildMeta;

        /// <summary>Gets the Build Index for this instance</summary>
        public string Index { get; }

        /// <summary>Gets the Build name of this instance</summary>
        public string Name { get; }

        /// <summary>Gets the <see cref="PrereleaseVersion"/> information for the build this is based on (if any)</summary>
        public PrereleaseVersion? PrereleaseVersion { get; }

        /// <inheritdoc/>
        public override string ToString( )
        {
            return ConstrainedVersion.ToString();
        }

        /// <inheritdoc/>
        /// <remarks>
        /// <see cref="CSemVerCI"/> follows ALL of the rules of SemVer ordering EXCEPT
        /// that it is EXPLICITLY using case insensitive comparison for the AlphaNumeric
        /// identifiers in a pre-release list.
        /// </remarks>
        public int CompareTo( CSemVerCI? other )
        {
            if(ReferenceEquals(this, other))
            {
                return 0;
            }

            if(other is null)
            {
                return 1;
            }

            // CSemVerCI always uses case insensitive comparisons, but otherwise follows the
            // ordering rules of SemVer.
            return SemVerComparer.SemVer.Compare(ConstrainedVersion, other.ConstrainedVersion);
        }

        /// <inheritdoc/>
        public bool Equals( CSemVerCI? other )
        {
            return ReferenceEquals(this, other)
                || (other is not null && SemVerComparer.SemVer.Compare(ConstrainedVersion, other.ConstrainedVersion) == 0);
        }

        /// <inheritdoc/>
        public override bool Equals( object? obj )
        {
            return Equals(obj as CSemVerCI);
        }

        /// <inheritdoc/>
        public override int GetHashCode( )
        {
            return ConstrainedVersion.GetHashCode();
        }

        /// <summary>Converts this instance to a <see cref="SemVer"/> instance</summary>
        /// <returns>Converted value</returns>
        /// <remarks>
        /// Since this version came from a <see cref="CSemVerCI"/> the ONLY valid ordering
        /// is a case insensitive one. The specifications are explicit on the use of case insensitive
        /// comparisons, while the spec for SemVer is silent on the point, which leads to ambiguities.
        /// </remarks>
        public SemVer ToSemVer()
        {
            return ConstrainedVersion;
        }

        /// <inheritdoc cref="ToSemVer"/>
        /// <param name="val">Value to convert</param>
        public static implicit operator SemVer(CSemVerCI val)
        {
            return val.ToSemVer();
        }

        /// <inheritdoc/>
        public static bool operator >( CSemVerCI left, CSemVerCI right ) => left.CompareTo(right) > 0;

        /// <inheritdoc/>
        public static bool operator >=( CSemVerCI left, CSemVerCI right ) => left.CompareTo(right) >= 0;

        /// <inheritdoc/>
        public static bool operator <( CSemVerCI left, CSemVerCI right ) => left.CompareTo(right) < 0;

        /// <inheritdoc/>
        public static bool operator <=( CSemVerCI left, CSemVerCI right ) => left.CompareTo(right) <= 0;

        /// <inheritdoc/>
        public static bool operator ==( CSemVerCI? left, CSemVerCI? right ) => Equals(left, right);

        /// <inheritdoc/>
        public static bool operator !=( CSemVerCI? left, CSemVerCI? right ) => !Equals(left, right);

        private readonly SemVer ConstrainedVersion;

        /// <summary>Tries to convert a <see cref="SemVer"/> to a <see cref="CSemVerCI"/></summary>
        /// <param name="ver">Version to convert</param>
        /// <param name="result">Resulting <see cref="CSemVerCI"/> if conversion is possible</param>
        /// <param name="reason">Reason why <paramref name="ver"/> is not convertible to a <see cref="CSemVerCI"/></param>
        /// <returns>
        /// <see langword="true"/> if conversion is successful and <paramref name="result"/> contains a valid value.
        /// <see langword="false"/> if conversion is not successful and <paramref name="reason"/> contains the reason.
        /// </returns>
        public static bool TryFrom(
            SemVer ver,
            [MaybeNullWhen( false )] out CSemVerCI result,
            [MaybeNullWhen( true )] out Exception reason
            )
        {
            result = default;
            reason = default;

            if(!HasCIPreReleaseData(ver))
            {
                reason = new FormatException("SemVer does not contain 'ci' pre-release in expected location");
            }

            IEnumerable<string> nonCiPreRel = [];
            if( ver.PreRelease.Length == 6)
            {
                nonCiPreRel = ver.PreRelease.Take(3);
            }

            var nonCiVer = new SemVer(ver.Major, ver.Minor, ver.Patch, nonCiPreRel, ver.BuildMeta);
            if(!CSemVer.TryFrom(nonCiVer, out CSemVer? baseBuild, out reason))
            {
                return false;
            }

            try
            {
                // HasCIPreReleaseData already verified length as either 3 or 6
                // elements. 0 valued number and fix are required for CI versions
                // So the exact number of elements is limited to two cases.
                result = ver.PreRelease.Length == 3
                    ? new CSemVerCI(baseBuild, ver.PreRelease[1], ver.PreRelease[2])
                    : new CSemVerCI(baseBuild, ver.PreRelease[4], ver.PreRelease[5]);
                return true;
            }
            catch(ArgumentException ex)
            {
                reason = ex;
                return false;
            }
        }

        /// <inheritdoc/>
        public static CSemVerCI Parse( string s, IFormatProvider? provider )
        {
            return TryParse(s, out CSemVerCI? retVal, out Exception? ex) ? retVal : throw ex;
        }

        /// <inheritdoc/>
        public static bool TryParse( [NotNullWhen( true )] string? s, IFormatProvider? provider, [MaybeNullWhen( false )] out CSemVerCI result )
        {
            return TryParse(s, out result, out _);
        }

        /// <summary>Applies try pattern to attempt parsing a <see cref="CSemVerCI"/> from a string</summary>
        /// <param name="s">Raw string to parse from</param>
        /// <param name="result">Resulting version or default if parse is successful</param>
        /// <param name="reason">Reason for failure to parse (as an <see cref="Exception"/>)</param>
        /// <returns><see langword="true"/> if parse is successful or <see langword="false"/> if not</returns>
        public static bool TryParse(
            [NotNullWhen( true )] string? s,
            [MaybeNullWhen( false )] out CSemVerCI result,
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

        /// <summary>Static method to test a set of release parts of a <see cref="SemVer"/> to determine if it represents a <see cref="CSemVerCI"/></summary>
        /// <param name="ver">Base version to test if it might represent a <see cref="CSemVerCI"/></param>
        /// <returns><see langword="true"/> if the pre-release represent a <see cref="CSemVerCI"/></returns>
        /// <remarks>
        /// <note type="important">
        /// This method only looks at the length of the full set and contents of specific entries of the <see cref="SemVer.PreRelease"/>.
        /// This is legit as ALL <see cref="CSemVerCI"/> instances are valid <see cref="SemVer"/>s.
        /// </note>
        /// </remarks>
        private static bool HasCIPreReleaseData( SemVer ver )
        {
            return (ver.PreRelease.Length == 3 && ver.PreRelease[ 0 ] == "-ci")
                || (ver.PreRelease.Length == 6 && ver.PreRelease[ 3 ] == "ci");
        }

        private static readonly Regex CiBuildIdRegex = GetGeneratedBuildIdRegex();

        [GeneratedRegex( @"\A[0-9a-zA-Z\-]+\Z" )]
        private static partial Regex GetGeneratedBuildIdRegex( );
    }
}
