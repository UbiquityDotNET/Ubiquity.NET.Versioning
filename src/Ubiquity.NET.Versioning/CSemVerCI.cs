// -----------------------------------------------------------------------
// <copyright file="CSemVerCI.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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
            : this( new CSemVer( 0, 0, 0, null, buildMeta ), index, name )
        {
        }

        /// <summary>Initializes a new instance of the <see cref="CSemVerCI"/> class.</summary>
        /// <param name="baseBuild">Base build version this CI version is based on</param>
        /// <param name="index">Index for this CI build</param>
        /// <param name="name">Name for this CI build</param>
        /// <seealso href="https://csemver.org">CSemVer-CI §2-6</seealso>
        public CSemVerCI( CSemVer baseBuild, string index, string name )
            : this(ToPatchP1(baseBuild), baseBuild, GetCiSequence( baseBuild, index, name ) )
        {
            index.ThrowIfNotMatch( SemVerGrammar.IdentifierChars.End() );
            name.ThrowIfNotMatch( SemVerGrammar.IdentifierChars.End() );
        }

        /// <summary>Gets the base build for this CI version</summary>
        public CSemVer BaseBuild { get; }

        /// <summary>Gets the Build Index for this instance</summary>
        public string Index => PreRelease[ PrerelCiInfoIndex ];

        /// <summary>Gets the Build name of this instance</summary>
        public string Name => PreRelease[ PrerelCiInfoIndex + 1 ];

        /// <summary>Gets the <see cref="PrereleaseVersion"/> information for the build this is based on (if any)</summary>
        public PrereleaseVersion? PrereleaseVersion => BaseBuild.PrereleaseVersion;

        /// <summary>Gets the <see cref="FileVersionQuad"/> representation of this <see cref="CSemVer"/></summary>
        /// <remarks>
        /// <para>Since a <see cref="FileVersionQuad"/> is entirely numeric the conversion is "lossy" but does
        /// NOT lose relation to other released versions converted. That, is the loss includes the 'BuildIndex'
        /// and 'BuildName', but not that it was a CI build.</para>
        /// <note type="important">
        /// It is important to note that two CI builds with the same base version will produce the same value!
        /// CI builds are not intended for long term stability and this is not a bug but a design of how CSemVer
        /// (and CSemVer-CI) work to produce a <see cref="FileVersionQuad"/>. Such conversion LOSES the 'BuildIndex'
        /// and 'BuildName' properties and is not recoverable. Though consuming applications may chose to convey
        /// that information in some other form. [Definition of that is outside the scope of this library].
        /// </note>
        /// </remarks>
        public FileVersionQuad FileVersion => new( OrderedVersion, isCiBuild: true );

        /// <summary>Gets the CSemVer ordered version value of this version</summary>
        /// <remarks>
        /// This is the version itself, which is Patch+1, not the base build. If that is ever needed it
        /// is obtainable via the <see cref="CSemVer.OrderedVersion"/> property on the <see cref="BaseBuild"/>
        /// property.
        /// </remarks>
        public Int64 OrderedVersion => CSemVer.MakeOrderedVersion((UInt64)Major, (UInt64)Minor, (UInt64)Patch, PrereleaseVersion);

        /// <summary>Converts a <see cref="FileVersionQuad"/> to a <see cref="CSemVerCI"/></summary>
        /// <param name="quad">File version to convert</param>
        /// <param name="index">CI Build index</param>
        /// <param name="name">CI Build name</param>
        /// <returns>Converted version</returns>
        /// <remarks>
        /// <note type="important">
        /// It is NOT possible to create a CSemVerCI without a name as there is no defined
        /// format for such a thing as a string. Therefore, code receiving only a <see cref="FileVersionQuad"/>
        /// can use the <see cref="FileVersionQuad.IsCiBuild"/> property to test for a CI build AND rely
        /// on the ordering for that type to correctly honor the ordering of a CI build.
        /// </note>
        /// </remarks>
        public static CSemVerCI From( FileVersionQuad quad, string index, string name )
        {
            return TryFrom( quad, index, name, out CSemVerCI? retVal, out Exception? ex ) ? retVal : throw ex;
        }

        /// <summary>Tries to convert a <see cref="FileVersionQuad"/> into a <see cref="CSemVerCI"/></summary>
        /// <param name="quad">File version to build from</param>
        /// <param name="index">CI build index</param>
        /// <param name="name">CI build name</param>
        /// <param name="result">Resulting version</param>
        /// <param name="reason">Reason for failure to convert or <see langword="null"/> if not</param>
        /// <param name="exp">Expression for the <paramref name="quad"/> value [default: normally provided by compiler]</param>
        /// <returns>
        /// <see langword="true"/> if conversion is successful and <paramref name="result"/> contains a valid value.
        /// <see langword="false"/> if conversion is not successful and <paramref name="reason"/> contains the reason.
        /// </returns>
        public static bool TryFrom(
            FileVersionQuad quad,
            string index,
            string name,
            [MaybeNullWhen( false )] out CSemVerCI result,
            [MaybeNullWhen( true )] out Exception reason,
            [CallerArgumentExpression( nameof( quad ) )] string? exp = null
            )
        {
            ArgumentException.ThrowIfNullOrWhiteSpace( index );
            ArgumentException.ThrowIfNullOrWhiteSpace( name );

            result = default;
            reason = default;
            if(!quad.IsCiBuild)
            {
                reason = new ArgumentException( "FileVersionQuad indicates it is not for a CI build!", exp );
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
                reason = new ArgumentOutOfRangeException( nameof( quad ), "Base build version number exceeds the maximum allowed" );
                return false;
            }

            var baseBuild = CSemVer.FromOrderedVersion(baseBuildOrderedVersion);
            result = new(ToPatchP1(baseBuild),  baseBuild, GetCiSequence(baseBuild, index, name) );
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

            if(!HasCIPreReleaseData( ver ))
            {
                reason = new FormatException( "SemVer does not contain pre-release 'CI' marker in expected location" );
                return false;
            }

            ImmutableArray<string> nonCiPreRel = [];
            if(ver.PreRelease.Length == 6)
            {
                nonCiPreRel = ver.PreRelease[ ..3 ];
            }

            try
            {
                // TODO: Optimize this so it isn't creating so many versions to compute the baseBuild
                //       The constructor will do Patch+1 again, so there should be a way to eliminate
                //       the extra overhead and construct from both forms.

                var nonCiVer = new SemVer(ver.Major, ver.Minor, ver.Patch, AlphaNumericOrdering.CaseInsensitive, nonCiPreRel, ver.BuildMeta);
                if(!CSemVer.TryFrom( nonCiVer, out CSemVer? p1Build, out reason ))
                {
                    return false;
                }

                // create patch-1 base build version expected by constructor
                var baseBuild = CSemVer.FromOrderedVersion(p1Build.OrderedVersion - (Int64)CSemVer.MulPatch);

                // HasCIPreReleaseData already verified length as either 3 or 6
                // elements. 0 valued number and fix are required for CI versions
                // So the exact number of elements is limited to two cases.
                result = ver.PreRelease.Length == 3
                    ? new CSemVerCI( baseBuild, ver.PreRelease[ 1 ], ver.PreRelease[ 2 ] )
                    : new CSemVerCI( baseBuild, ver.PreRelease[ 4 ], ver.PreRelease[ 5 ] );
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
            return TryParse( s, out CSemVerCI? retVal, out Exception? ex ) ? retVal : throw ex;
        }

        /// <inheritdoc/>
        public static bool TryParse( [NotNullWhen( true )] string? s, IFormatProvider? provider, [MaybeNullWhen( false )] out CSemVerCI result )
        {
            // Throw if provider isn't one that is ignorable.
            provider.ThrowIfCaseSensitive();
            return TryParse( s, out result, out _ );
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

        // Private constructor to create from a base build
        // The provided ciSeq includes the index and name.
        // PatchP1 is the baseBuild with Patch+1 applied so that the
        // base constructor is callable using those values.
        private CSemVerCI( CSemVer patchP1, CSemVer baseBuild, ImmutableArray<string> ciSeq )
            : base(
                patchP1.ThrowIfNull().Major,
                patchP1.Minor,
                patchP1.Patch,
                AlphaNumericOrdering.CaseInsensitive,
                [ .. patchP1.PreRelease, .. ciSeq ],
                patchP1.BuildMeta
            )
        {
            BaseBuild = baseBuild;
        }

        // Gets the index into the PreRelease array that contains
        // the CI information (+0 => BuildIndex; +1 => BuildName)
        private int PrerelCiInfoIndex => PreRelease.Length switch
        {
            3 => 1,    // 'CiBuildIndex' is at index 1 ('CiBuildName' is at index 2)
            6 => 4,    // 'CiBuildIndex' is at index 4 ('CiBuildName' is at index 5)
            _ => throw new InvalidOperationException( "INTERNAL ERROR: Unexpected array length!" )
        };

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
            return ver is CSemVerCI
                || (ver.PreRelease.Length == 3 && ver.PreRelease[ 0 ] == "-ci")
                || (ver.PreRelease.Length == 6 && ver.PreRelease[ 3 ] == "ci");
        }

        private static ImmutableArray<string> GetCiSequence(
            CSemVer baseBuild,
            [NotNull] string index,
            [NotNull] string name,
            [CallerArgumentExpression( nameof( baseBuild ) )] string? baseBuildExp = null,
            [CallerArgumentExpression( nameof( index ) )] string? indexExp = null,
            [CallerArgumentExpression( nameof( name ) )] string? nameExp = null
            )
        {
            ArgumentNullException.ThrowIfNull( baseBuild, baseBuildExp );
            ArgumentException.ThrowIfNullOrWhiteSpace( index, indexExp );
            ArgumentException.ThrowIfNullOrWhiteSpace( name, nameExp );

            return [ baseBuild.IsPrerelease ? "ci" : "-ci", index, name ];
        }

        private static CSemVer ToPatchP1([NotNull] CSemVer baseBuild, [CallerArgumentExpression(nameof(baseBuild))] string? exp = null)
        {
            ArgumentNullException.ThrowIfNull(baseBuild, exp);

            // CSemVer-CI §2; Only zero timed gets build meta... [Go Figure!]
            // [Though the spec is rather ambiguous on the point.]
            // This seems pointless, so the default is to allow it, but switch can enforce strict compliance
            if(AppContextSwitches.CSemVerCIOnlySupportsBuildMetaOnZeroTimedVersions && !baseBuild.IsZero && !baseBuild.BuildMeta.IsDefaultOrEmpty)
            {
                throw new ArgumentException( "non-zero timed base build contains meta data", exp );
            }

            // CSemVer-CI §2
            if(baseBuild.IsZero && baseBuild.IsPrerelease)
            {
                throw new ArgumentException( Resources.csemver_ci_zerotimed_versions_cannot_use_a_pre_release_as_the_base_build, exp );
            }

            // IFF base build is not 0 timed, apply Patch + 1, this will roll over if patch is at max etc...
            // if the final version is too large, an exception is thrown in the factory method.
            Int64 offset = (baseBuild.IsZero ? 0L : (Int64)CSemVer.MulPatch);
            return CSemVer.FromOrderedVersion( baseBuild.OrderedVersion + offset, baseBuild.BuildMeta);
        }
    }
}
