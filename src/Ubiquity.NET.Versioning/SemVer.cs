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
    /// <para>The comparison/ordering of this type is based on the rules for Semantic Versioning with
    /// the extension of <see cref="AlphaNumericOrdering"/>. Sadly, the SemVer spec is silent on
    /// the point of case comparisons and different major component repositories have chosen different
    /// interpretations of the spec as a result. Thus any consumer must explicitly decide which comparison
    /// a version expects and that is part of the version.</para>
    /// <note type="note">Technically, the SemVer spec states that alphanumeric Identifiers are ordered
    /// lexicographically, which would make them case sensitive. However, since MAJOR framework repositories
    /// have chosen to use each approach the real world of ambiguity, sadly, wins.</note>
    /// </remarks>
    /// <seealso href="https://semver.org/"/>
    public class SemVer
        : IParsable<SemVer>
        , IComparable<SemVer>
        , IEquatable<SemVer>
        , IComparisonOperators<SemVer?, SemVer?, bool>
    {
        /// <summary>Initializes a new instance of the <see cref="SemVer"/> class.</summary>
        /// <param name="major">Major portion of the core version</param>
        /// <param name="minor">Minor portion of the core version</param>
        /// <param name="patch">Patch portion of the core version</param>
        /// <param name="sortOrdering">Sort ordering to use for any ordering comparisons</param>
        /// <param name="preRel">PreRelease components</param>
        /// <param name="build">Build meta components</param>
        public SemVer(
            BigInteger major,
            BigInteger minor,
            BigInteger patch,
            AlphaNumericOrdering sortOrdering = AlphaNumericOrdering.CaseSensitive,
            ImmutableArray<string> preRel = default,
            ImmutableArray<string> build = default
            )
        {
            Major = major;
            Minor = minor;
            Patch = patch;

            if(!preRel.IsDefaultOrEmpty)
            {
                PreRelease = ValidateElementsWithParser(preRel, SemVerGrammar.PreReleaseIdentifier);
            }

            if(!build.IsDefaultOrEmpty)
            {
                BuildMeta = ValidateElementsWithParser(build, SemVerGrammar.BuildIdentifier);
            }

            if( sortOrdering == AlphaNumericOrdering.None)
            {
                throw new ArgumentException("Sort ordering of 'None' is an invalid value", nameof(sortOrdering));
            }

            AlphaNumericOrdering = sortOrdering;
        }

        /// <summary>Gets the sort ordering applied to this version</summary>
        public AlphaNumericOrdering AlphaNumericOrdering { get; } = AlphaNumericOrdering.CaseSensitive;

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

        /// <inheritdoc/>
        /// <exception cref="InvalidOperationException">The <see cref="AlphaNumericOrdering"/> for both sides does not match</exception>
        /// <remarks>
        /// The <see cref="AlphaNumericOrdering"/> of <paramref name="other"/> must match the value of this instance for direct comparison.
        /// If they do not match an <see cref="InvalidOperationException"/> is thrown. To override this callers can use the explicit
        /// comparisons provided in <see cref="Comparison.CaseSensitive"/> or <see cref="Comparison.CaseInsensitive"/>. Those comparisons
        /// will ignore the value of the <see cref="AlphaNumericOrdering"/> property for either side and use their own ordering.
        /// </remarks>
        [SuppressMessage( "Style", "IDE0046:Convert to conditional expression", Justification = "Nested conditionals are NOT simpler" )]
        public int CompareTo( SemVer? other )
        {
            if(other is null)
            {
                // By definition null is ordered before any non-null value
                return 1;
            }

            if (AlphaNumericOrdering != other.AlphaNumericOrdering)
            {
                throw new InvalidOperationException("SemVer values have different AlphaNumericOrdering, direct comparison is not supported.");
            }

            return AlphaNumericOrdering == AlphaNumericOrdering.CaseSensitive
                 ? Comparison.CaseSensitive.SemVer.Compare(this, other)
                 : Comparison.CaseInsensitive.SemVer.Compare(this, other);
        }

        #region Relational operators

        /// <inheritdoc/>
        public override bool Equals( object? obj )
        {
            return obj is SemVer v && Equals(v);
        }

        /// <inheritdoc/>
        public override int GetHashCode( )
        {
            // NOTE: Build meta does not contribute to ordering and therefore does not contribute to the hash
            return HashCode.Combine(AlphaNumericOrdering, Major, Minor, Patch, PreRelease);
        }

        /// <inheritdoc/>
        public bool Equals( SemVer? other )
        {
            return CompareTo(other) == 0;
        }

        /// <inheritdoc/>
        public static bool operator ==( SemVer? left, SemVer? right )
        {
            return left is null ? right is null : left.Equals( right );
        }

        /// <inheritdoc/>
        public static bool operator !=( SemVer? left, SemVer? right )
        {
            return !(left == right);
        }

        /// <inheritdoc/>
        public static bool operator <( SemVer? left, SemVer? right )
        {
            return left is null ? right is not null : left.CompareTo( right ) < 0;
        }

        /// <inheritdoc/>
        public static bool operator <=( SemVer? left, SemVer? right )
        {
            return left is null || left.CompareTo( right ) <= 0;
        }

        /// <inheritdoc/>
        public static bool operator >( SemVer? left, SemVer? right )
        {
            return left is not null && left.CompareTo( right ) > 0;
        }

        /// <inheritdoc/>
        public static bool operator >=( SemVer? left, SemVer? right )
        {
            return left is null ? right is null : left.CompareTo( right ) >= 0;
        }
        #endregion

        #region Parsing

        /// <summary>Parses a string into a <see cref="SemVer"/></summary>
        /// <param name="s">Input string to parse</param>
        /// <param name="provider">Provider to use for parsing (see remarks)</param>
        /// <returns>Parsed <see cref="SemVer"/> value</returns>
        /// <remarks>
        /// The <paramref name="provider"/> is used to specify the sort ordering of
        /// Alphanumeric IDs for the version. The default used if <see langword="null"/>
        /// is provided is <see cref="SemVerFormatProvider.CaseSensitive"/> which provides
        /// case sensitive comparison versions. If the source of the string requires
        /// insensitive comparison, then callers should use <see cref="SemVerFormatProvider.CaseInsensitive"/>.
        /// That is, unless explicitly provided the <see cref="AlphaNumericOrdering"/>
        /// value of all parsed versions is <see cref="AlphaNumericOrdering.CaseSensitive"/>
        /// </remarks>
        public static SemVer Parse( string s, IFormatProvider? provider )
        {
            return TryParse( s, provider, out SemVer? retVal, out Exception? ex ) ? retVal : throw ex;
        }

        /// <summary>Tries to parse a string into a <see cref="SemVer"/></summary>
        /// <param name="s">Input string to parse</param>
        /// <param name="provider">Provider to use for parsing (see remarks)</param>
        /// <param name="result">Resulting version if parsed successfully</param>
        /// <returns><see langword="true"/> if the version is parsed and false if it is not</returns>
        /// <inheritdoc cref="Parse(string, IFormatProvider?)" path="/remarks"/>
        public static bool TryParse( [NotNullWhen( true )] string? s, IFormatProvider? provider, [MaybeNullWhen( false )] out SemVer result )
        {
            return TryParse( s, provider, out result, out _ );
        }

        /// <summary>Tries to parse a string into a semantic version providing details of any failures</summary>
        /// <param name="s">Input string to parse</param>
        /// <param name="provider">Formatting provider to use</param>
        /// <param name="result">Resulting <see cref="SemVer"/> if parse succeeds</param>
        /// <param name="ex">Exception data for any errors or <see langword="null"/> if parse succeeded</param>
        /// <returns><see langword="true"/> if string successfully parsed or <see langword="false"/> if not</returns>
        internal static bool TryParse(
            [NotNull] string? s,
            IFormatProvider? provider,
            [MaybeNullWhen( false )] out SemVer result,
            [MaybeNullWhen( true )] out Exception ex
            )
        {
            ArgumentNullException.ThrowIfNull( s );

            provider ??= SemVerFormatProvider.CaseSensitive;
            IResult<SemVer> parseResult = SemVerGrammar.SemanticVersion(provider.GetOrdering()).TryParse(s);
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
