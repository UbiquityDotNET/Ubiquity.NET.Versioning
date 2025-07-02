// -----------------------------------------------------------------------
// <copyright file="CSemVerCI.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

using Sprache;

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
    public sealed class CSemVerCI
        : SemVer
        , IParsable<CSemVerCI>
    {
        /// <summary>Initializes a new instance of the <see cref="CSemVerCI"/> class as a "CSemVer-CI ZeroTimed' value</summary>
        /// <param name="index">Index of this CI build</param>
        /// <param name="name">Name of this CI build</param>
        /// <param name="buildMeta">Optional Build meta</param>
        /// <seealso href="https://csemver.org">CSemVer-CI §2-5</seealso>
        public CSemVerCI( string index, string name, ImmutableArray<string> buildMeta = default )
            : this( new CSemVer(0, 0, 0, null, buildMeta), index, name)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="CSemVerCI"/> class.</summary>
        /// <param name="baseBuild">Base build version this CI version is based on</param>
        /// <param name="index">Index for this CI build</param>
        /// <param name="name">Name for this CI build</param>
        /// <remarks>
        /// The <paramref name="baseBuild"/> is assumed a lower sort order than any CI build and
        /// therefore should be at least Patch+1 of an actual release. This constructor does not
        /// (and cannot) VERIFY such a thing.
        /// </remarks>
        /// <seealso href="https://csemver.org">CSemVer-CI §2-6</seealso>
        public CSemVerCI( CSemVer baseBuild, string index, string name )
            : this(baseBuild, GetCiSequence(baseBuild, index, name))
        {
            Index = index.ThrowIfNotMatch( SemVerGrammar.IdentifierChars.End() );
            Name = name.ThrowIfNotMatch( SemVerGrammar.IdentifierChars.End() );
        }

        /// <summary>Gets the Build Index for this instance</summary>
        /// <remarks>
        /// This string may be empty if this instance was created from a <see cref="FileVersionQuad"/> as
        /// those do not include the index or name.
        /// </remarks>
        public string Index { get; }

        /// <summary>Gets the Build name of this instance</summary>
        /// <remarks>
        /// This string may be empty if this instance was created from a <see cref="FileVersionQuad"/> as
        /// those do not include the index or name.
        /// </remarks>
        public string Name { get; }

        /// <summary>Gets the <see cref="PrereleaseVersion"/> information for the build this is based on (if any)</summary>
        public PrereleaseVersion? PrereleaseVersion { get; }

        /// <summary>Converts a <see cref="FileVersionQuad"/> to a <see cref="CSemVerCI"/></summary>
        /// <param name="quad">File version to convert</param>
        /// <returns>Converted version</returns>
        public static CSemVerCI From(FileVersionQuad quad)
        {
            return TryFrom(quad, out CSemVerCI? retVal, out Exception? ex) ? retVal : throw ex;
        }

        /// <summary>Tries to convert a <see cref="FileVersionQuad"/> into a <see cref="CSemVerCI"/></summary>
        /// <param name="quad">File version to build from</param>
        /// <param name="result">Resulting version</param>
        /// <param name="reason">Reason for failure to convert or <see langword="null"/> if not</param>
        /// <param name="exp">Expression for the <paramref name="quad"/> value [default: normally provided by compiler]</param>
        /// <returns>
        /// <see langword="true"/> if conversion is successful and <paramref name="result"/> contains a valid value.
        /// <see langword="false"/> if conversion is not successful and <paramref name="reason"/> contains the reason.
        /// </returns>
        public static bool TryFrom(
            FileVersionQuad quad,
            [MaybeNullWhen( false )] out CSemVerCI result,
            [MaybeNullWhen( true )] out Exception reason,
            [CallerArgumentExpression(nameof(quad))] string? exp = null
            )
        {
            result = default;
            reason = default;
            if(!quad.IsCiBuild)
            {
                reason = new ArgumentException("FileVersionQuad indicates it is not for a CI build!", exp);
            }

            // base build is always patch+1 so that it is ordered AFTER the release it is based on
            // Note: if patch is already 9999 then this rolls over to next minor, etc... until it's
            // just too big.
            Int64 baseBuildOrderedVersion = quad.ToOrderedVersion() + (Int64)CSemVer.MulPatch;

            // While components of a FileVersion QUAD are constrained to the values defined in a CSemVer
            // it is possible to have a patch+1 version that is >= to the max value already, so no CI build
            // is plausible that can use it as the base.
            if(baseBuildOrderedVersion >= CSemVer.MaxOrderedVersion)
            {
                reason = new ArgumentOutOfRangeException(nameof(quad), "Base build version number exceeds the maximum allowed");
                return false;
            }

            var baseBuild = CSemVer.FromOrderedVersion(baseBuildOrderedVersion);
            result = new(baseBuild, baseBuild.PrereleaseVersion?.FormatElements().ToImmutableArray() ?? default);
            return true;
        }

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

            ImmutableArray<string> nonCiPreRel = [];
            if( ver.PreRelease.Length == 6)
            {
                nonCiPreRel = ver.PreRelease[ ..3 ];
            }

            var nonCiVer = new SemVer(ver.Major, ver.Minor, ver.Patch, AlphaNumericOrdering.CaseInsensitive, nonCiPreRel, ver.BuildMeta);
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
        public static new CSemVerCI Parse( string s, IFormatProvider? provider )
        {
            // Throw if provider isn't one that is ignorable.
            provider.ThrowIfCaseSensitive();
            return TryParse(s, out CSemVerCI? retVal, out Exception? ex) ? retVal : throw ex;
        }

        /// <inheritdoc/>
        public static bool TryParse( [NotNullWhen( true )] string? s, IFormatProvider? provider, [MaybeNullWhen( false )] out CSemVerCI result )
        {
            // Throw if provider isn't one that is ignorable.
            provider.ThrowIfCaseSensitive();
            return TryParse(s, out result, out _);
        }

        /// <summary>Applies try pattern to attempt parsing a <see cref="CSemVerCI"/> from a string</summary>
        /// <param name="s">Raw string to parse from</param>
        /// <param name="result">Resulting version or default if parse is successful</param>
        /// <param name="reason">Reason for failure to parse (as an <see cref="Exception"/>)</param>
        /// <returns><see langword="true"/> if parse is successful or <see langword="false"/> if not</returns>
        [SuppressMessage( "Style", "IDE0002:Simplify Member Access", Justification = "More explicit this way" )]
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

            if(!SemVer.TryParse( s, SemVerFormatProvider.CaseInsensitive, out SemVer? semVer, out reason))
            {
                result = default;
                return false;
            }

            // OK as a SemVer, so try and see if that conforms to a constrained form.
            return TryFrom(semVer, out result, out reason);
        }

        // Private constructor to create from a base build without index/name
        // This is used in the static conversion from a FileVersionQuad AND
        // from the constructor supporting a name/index
        private CSemVerCI( CSemVer baseBuild, ImmutableArray<string> preRel)
            : base(
                baseBuild.ThrowIfNull().Major,
                baseBuild.Minor,
                baseBuild.Patch,
                AlphaNumericOrdering.CaseInsensitive,
                preRel,
                baseBuild.BuildMeta
            )
        {
            // CSemVer-CI §2; Only zero timed gets build meta... [Go Figure!]
            // [Though the spec is rather ambiguous on the point.]
            // This seems pointless, so the default is to allow it, but switch can enforce strict compliance
            if (AppContextSwitches.CSemVerCIOnlySupportsBuildMetaOnZeroTimedVersions && !baseBuild.IsZero && !baseBuild.BuildMeta.IsDefaultOrEmpty)
            {
                throw new ArgumentException("non-zero timed base build contains meta data", nameof(baseBuild));
            }

            // CSemVer-CI §2
            if(baseBuild.IsZero && baseBuild.IsPrerelease)
            {
                throw new ArgumentException(Resources.csemver_ci_zerotimed_versions_cannot_use_a_pre_release_as_the_base_build, nameof(baseBuild));
            }

            PrereleaseVersion = baseBuild.PrereleaseVersion;
            Index = string.Empty;
            Name = string.Empty;
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

        private static ImmutableArray<string> GetCiSequence(
            CSemVer baseBuild,
            [NotNull] string index,
            [NotNull] string name,
            [CallerArgumentExpression(nameof(baseBuild))] string? baseBuildExp = null,
            [CallerArgumentExpression(nameof(index))] string? indexExp = null,
            [CallerArgumentExpression(nameof(name))] string? nameExp = null
            )
        {
            ArgumentNullException.ThrowIfNull(baseBuild, baseBuildExp);
            ArgumentException.ThrowIfNullOrWhiteSpace(index, indexExp);
            ArgumentException.ThrowIfNullOrWhiteSpace(name, nameExp);

            string[] ciSequence = [baseBuild.IsPrerelease ? "ci" : "-ci", index, name ];
            var seq = baseBuild.PrereleaseVersion?.FormatElements().Concat( ciSequence ) ?? ciSequence;
            return [ .. seq ];
        }
    }
}
