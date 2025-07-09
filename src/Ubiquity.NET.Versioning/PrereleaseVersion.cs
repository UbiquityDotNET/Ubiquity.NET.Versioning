// -----------------------------------------------------------------------
// <copyright file="PrereleaseVersion.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

using Sprache;

namespace Ubiquity.NET.Versioning
{
    /// <summary>Pre-Release portion of a Constrained Semantic Version (CSemVer/CSemVer-CI)</summary>
    /// <remarks>
    /// Based on CSemVer v1.0.0-rc.1. This contains the PreRelease version details.
    /// <note type="important">
    /// The default constructor for this value type is a valid pre-release alpha
    /// version. If an optional pre-release is intended then nullability should be
    /// used to indicate that and test for a null value.
    /// </note>
    /// </remarks>
    /// <seealso href="https://csemver.org/"/>
    public readonly record struct PrereleaseVersion
    {
        /// <summary>Initializes a new instance of the <see cref="PrereleaseVersion"/> struct</summary>
        /// <param name="index">index number (Name of the pre-release expressed as an integral index) [0-7]</param>
        /// <param name="number">Pre-release number for this build [0-99]</param>
        /// <param name="fix">Pre-release fix for this build [0-99]</param>
        public PrereleaseVersion( byte index, byte number, byte fix )
        {
            Index = index.ThrowIfOutOfRange( (byte)0, (byte)7 );
            Number = number.ThrowIfOutOfRange( (byte)0, (byte)99 );
            Fix = fix.ThrowIfOutOfRange( (byte)0, (byte)99 );
        }

        /// <summary>Initializes a new instance of the <see cref="PrereleaseVersion"/> struct</summary>
        /// <param name="preRelName">indexedName of the pre-release. (see remarks)</param>
        /// <param name="preRelNumber">Pre-release number for this build [0-99]</param>
        /// <param name="preRelFix">Pre-release fix for this build [0-99]</param>
        /// <exception cref="ArgumentException">Argument does not match expectations</exception>
        /// <remarks>
        /// The <paramref indexedName="preRelName"/> must match one of the 8 well-known pre-release names. It is
        /// compared using <see cref="StringComparison.OrdinalIgnoreCase"/>.
        /// </remarks>
        public PrereleaseVersion( string preRelName, byte preRelNumber, byte preRelFix )
            : this( IndexFromName( preRelName ), preRelNumber, preRelFix )
        {
        }

        /// <summary>Gets the index value of this pre-release</summary>
        /// <remarks>
        /// The index value is a numeric index into a set of 8 well-known names. Thus, it has the
        /// range [0-7].
        /// </remarks>
        public byte Index { get; }

        /// <summary>Gets the Pre-Release number [0-99]</summary>
        public byte Number { get; }

        /// <summary>Gets the Pre-Release fix [0-99]</summary>
        public byte Fix { get; }

        /// <summary>Gets the indexedName of this pre-release</summary>
        public string Name => CSemVerPrereleaseGrammar.ValidPrereleaseNames[ Index ];

        /// <summary>Gets this <see cref="PrereleaseVersion"/> as a sequence of strings</summary>
        /// <param name="alawaysIncludeZero">Indicates whether the result will always include zero values</param>
        /// <returns>Sequence of strings that represent this instance</returns>
        /// <remarks>
        /// The default behavior is to skip the <see cref="Number"/> value if it and the <see cref="Fix"/>
        /// value are zero. <paramref name="alawaysIncludeZero"/> is used to override this and show the
        /// zero value always. (Normally this is only used for a <see cref="CSemVerCI"/>)
        /// </remarks>
        /// <seealso cref="SemVer.PreRelease"/>
        /// <seealso cref="CSemVer"/>
        /// <seealso cref="CSemVerCI"/>
        public IEnumerable<string> FormatElements( bool alawaysIncludeZero = false )
        {
            yield return Name;
            if(Number > 0 || Fix > 0 || alawaysIncludeZero)
            {
                yield return Number.ToString( CultureInfo.InvariantCulture );
                if(Fix >= 1 || alawaysIncludeZero)
                {
                    yield return Fix.ToString( CultureInfo.InvariantCulture );
                }
            }
        }

        /// <summary>Formats this instance as a string according to the rules of a Constrained Semantic Version</summary>
        /// <remarks>
        /// This assumes the Full format of the pre-release information
        /// </remarks>
        /// <returns>The formatted version information</returns>
        public override string ToString( )
        {
            return string.Join( '.', FormatElements() );
        }

        /// <summary>Tries to parse a <see cref="PrereleaseVersion"/> from a set of pre release parts from a version string</summary>
        /// <param name="preRelParts">Parts of the prerelease components to convert to a CSemVer(-CI) pre-release version</param>
        /// <returns>Result of the parse.</returns>
        /// <remarks>
        /// The <paramref name="preRelParts"/> must only contain the relevant parts of a pre-release version for CSemVer(-CI).
        /// Therefore it must contain at least one entry for the name, and optionally up to two additional entries for the
        /// number and fix values. If present, the number and fix values must parse to an integer in the range 0-99 using the
        /// <see cref="CultureInfo.InvariantCulture"/>.
        /// </remarks>
        public static IResult<PrereleaseVersion> TryParseFrom( IReadOnlyList<string> preRelParts )
        {
            // While it might seem like this is inefficient/redundant, the jury is still out on that.
            // This does incur the overhead of the allocation for and combination of the input string
            // it avoids the complexity of rolling it all out by hand, which triggers multiple heap
            // allocations and conversions of result types etc... so the advantages of eliminating
            // the overhead of the join are somewhat murky and come at the cost of significantly
            // increased code complexity/maintenance costs - deemed not worth it at this point.
            string elements = string.Join('.', preRelParts);
            return CSemVerPrereleaseGrammar.Prerelease.End().TryParse(elements);
        }

        private static byte IndexFromName( [NotNull] string preRelName, [CallerArgumentExpression( nameof( preRelName ) )] string? exp = null )
        {
            ArgumentException.ThrowIfNullOrWhiteSpace( preRelName, exp );
            return CSemVerPrereleaseGrammar.TryGetIndexFromName( preRelName, out byte retVal )
                   ? retVal
                   : throw new ArgumentException( "Invalid pre-release name", exp );
        }

        private static IResult<Tto> Convert<Tto, Tfrom>(IResult<Tfrom> from)
        {
            return Result.Failure<Tto>(from.Remainder, from.Message, from.Expectations);
        }
    }
}
