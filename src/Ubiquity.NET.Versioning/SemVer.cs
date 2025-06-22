// -----------------------------------------------------------------------
// <copyright file="SemVer.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

using Sprache;

using Ubiquity.NET.Versioning.Properties;

namespace Ubiquity.NET.Versioning
{
    /// <summary>Semantic Version value</summary>
    /// <remarks>
    /// <para>Officially a SemVer value does NOT limit the size of the numeric portions of a version
    /// so this implementation uses <see cref="BigInteger"/> values for each component. This
    /// allows all possible values.</para>
    /// <para>In practical terms any such component will likely "down convert" to an integer. If a
    /// version component in the real world exceeds the size of an integer, then there is probably
    /// something wrong with how the versioning is maintained.</para>
    /// <para>This type is intentionally NOT a value type or `record struct` etc... as the ONLY
    /// valid comparison that is always correct is reference equality. Any other comparison/ordering
    /// requires a specific comparer that not only understands the rules of a Semantic Version, but
    /// also deals with case sensitivity of those comparisons. Sadly, the SemVer spec is silent on
    /// the point of case comparisons and different major component repositories have chosen different
    /// interpretations of the spec as a result. Thus any consumer must explicitly decide which comparison
    /// to use.</para>
    /// <note type="note">
    /// Technically, the SemVer spec states that alphanumeric Identifiers are ordered lexicographically,
    /// which would make them case sensitive. However, since MAJOR framework repositories have chosen
    /// to use each approach the real world of ambiguity, sadly, wins.
    /// </note>
    /// </remarks>
    /// <seealso href="https://semver.org/"/>
    /// <seealso cref="SemVerComparer.CaseSensitive.SemVer"/>
    /// <seealso cref="SemVerComparer.SemVer"/>
    public sealed class SemVer
        : IParsable<SemVer>
    {
        /// <inheritdoc cref="SemVer.SemVer(BigInteger, BigInteger, BigInteger, IEnumerable{string}, IEnumerable{string})"/>
        public SemVer( BigInteger major, BigInteger minor, BigInteger patch )
            : this(major, minor, patch, null, null)
        {
        }

        /// <inheritdoc cref="SemVer.SemVer(BigInteger, BigInteger, BigInteger, IEnumerable{string}, IEnumerable{string})"/>
        public SemVer( BigInteger major, BigInteger minor, BigInteger patch, IEnumerable<string>? preRel )
            : this(major, minor, patch, preRel, null)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="SemVer"/> class.</summary>
        /// <param name="major">Major portion of the core version</param>
        /// <param name="minor">Minor portion of the core version</param>
        /// <param name="patch">Patch portion of the core version</param>
        /// <param name="preRel">PreRelease components</param>
        /// <param name="build">Build meta components</param>
        public SemVer( BigInteger major, BigInteger minor, BigInteger patch, IEnumerable<string>? preRel, IEnumerable<string>? build )
        {
            Major = major;
            Minor = minor;
            Patch = patch;

            if(preRel is not null)
            {
                PreRelease = ValidateElementsWithParser(preRel, SemVerGrammar.PreReleaseIdentifier);
            }

            if(build is not null)
            {
                BuildMeta = ValidateElementsWithParser(build, SemVerGrammar.BuildIdentifier);
            }
        }

        /// <summary>Gets the Major portion of the core version</summary>
        public BigInteger Major { get; } = 0;

        /// <summary>Gets the Minor portion of the core version</summary>
        public BigInteger Minor { get; } = 0;

        /// <summary>Gets the Patch portion of the core version</summary>
        public BigInteger Patch { get; } = 0;

        /// <summary>Gets the pre-release components of the version</summary>
        /// <remarks>
        /// Each component is either an alphanumeric identifier or a numeric identifier.
        /// This collection contains only the identifiers (no prefix or delimiters).
        /// </remarks>
        public ImmutableArray<string> PreRelease { get; } = [];

        /// <summary>Gets the build components of the version</summary>
        /// <remarks>
        /// Each component is either an alphanumeric identifier or a sequence of digits
        /// (including leading or all '0'). This collection contains only the identifiers
        /// (no prefix or delimiters).
        /// </remarks>
        public ImmutableArray<string> BuildMeta { get; } = [];

        /// <summary>Converts the version to a canonical SemVer string</summary>
        /// <returns>Formatted form of the version according to the rules of SemVer</returns>
        public override string ToString( )
        {
            var bldr = new StringBuilder($"{Major}.{Minor}.{Patch}");
            if( PreRelease.Length > 0 )
            {
                bldr.Append( '-' )
                    .AppendJoin( '.', PreRelease );
            }

            if( BuildMeta.Length > 0 )
            {
                bldr.Append( '+' )
                    .AppendJoin( '.', BuildMeta );
            }

            return bldr.ToString();
        }

        /// <summary>Gets a comparer for a <see cref="SemVer"/> value</summary>
        /// <param name="caseSensitive">Indicates if comparisons are case sensitive or not</param>
        /// <returns>Comparer for <see cref="SemVer"/></returns>
        /// <remarks>
        /// Ordering of semantic versions is complicated by the fact that the spec does NOT
        /// mention case sensitivity for comparing AlphaNumeric IDs. Worse, multiple real world
        /// implementations adopted policies based on assumptions of sensitivity. Originally, these
        /// were OS platform specific and not a general problem. However, as cross platform runtimes
        /// is now common the issue is a serious one as incorrect handling can lead to surprising
        /// results. <see cref="CSemVer"/> and <see cref="CSemVerCI"/> are explicit in the spec and
        /// always use case insensitive comparison. But <see cref="SemVer"/> is ambiguous. Thus, calling
        /// code MUST explicitly declare which it intends to use.
        /// </remarks>
        public static IComparer<SemVer> GetComparer(bool caseSensitive/* = false; No default value BY DESIGN - callers must be explicit on intent*/)
        {
            return caseSensitive ? SemVerComparer.CaseSensitive.SemVer : SemVerComparer.SemVer;
        }

        #region Parsing

        /// <inheritdoc/>
        public static SemVer Parse( string s, IFormatProvider? provider )
        {
            return TryParse( s, out SemVer? retVal, out Exception? ex ) ? retVal : throw ex;
        }

        /// <inheritdoc/>
        public static bool TryParse( [NotNullWhen( true )] string? s, IFormatProvider? provider, [MaybeNullWhen( false )] out SemVer result )
        {
            return TryParse( s, out result, out _ );
        }

        /// <summary>Tries to parse a string into a semantic version providing details of any failures</summary>
        /// <param name="s">Input string to parse</param>
        /// <param name="result">Resulting <see cref="SemVer"/> if parse succeeds</param>
        /// <param name="ex">Exception data for any errors or <see langword="null"/> if parse succeeded</param>
        /// <returns><see langword="true"/> if string successfully parsed or <see langword="false"/> if not</returns>
        internal static bool TryParse(
            [NotNull] string? s,
            [MaybeNullWhen( false )] out SemVer result,
            [MaybeNullWhen( true )] out Exception ex
            )
        {
            ArgumentNullException.ThrowIfNull( s );
            IResult<SemVer> parseResult = SemVerGrammar.SemanticVersion.TryParse(s);
            if(parseResult.Failed(out ex))
            {
                result = default;
                return false;
            }

            ex = null;
            result = parseResult.Value;
            return true;
        }
        #endregion

        private static ImmutableArray<string> ValidateElementsWithParser(
            IEnumerable<string>? value,
            Parser<string> parser,
            [CallerArgumentExpression(nameof(value))] string? exp = null
            )
        {
            return value is null ? [] : ValidateElementsWithParser( [.. value], parser, exp);
        }

        private static ImmutableArray<string> ValidateElementsWithParser(
            ImmutableArray<string>? value,
            Parser<string> parser,
            [CallerArgumentExpression(nameof(value))] string? exp = null
            )
        {
            if(value is null)
            {
                return [];
            }

            foreach(string part in value)
            {
                IResult<string> parseResult = parser.TryParse(part);
                if(!parseResult.WasSuccessful)
                {
                    throw new FormatException( Resources.exp_0_contains_invalid_value_1_2.Format(exp, part, parseResult.Message) );
                }
            }

            return value.Value;
        }
    }
}
