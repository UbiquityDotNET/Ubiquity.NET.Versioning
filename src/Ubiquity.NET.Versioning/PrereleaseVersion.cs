// -----------------------------------------------------------------------
// <copyright file="PrereleaseVersion.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Ubiquity.NET.Versioning
{
    /// <summary>Pre-Release portion of a Constrained Semantic Version</summary>
    /// <remarks>Based on CSemVer v1.0.0-rc.1</remarks>
    /// <seealso href="https://csemver.org/"/>
    public readonly record struct PrereleaseVersion
        : IFormattable
    {
        /// <summary>Initializes a new instance of the <see cref="PrereleaseVersion"/> struct</summary>
        /// <param name="index">index number (Name of the pre-release expressed as an integral index) [0-7]</param>
        /// <param name="number">Pre-release number for this build [0-99]</param>
        /// <param name="fix">Pre-release fix for this build [0-99]</param>
        public PrereleaseVersion( int index, int number, int fix )
        {
            index.ThrowIfOutOfRange(0, 7);
            number.ThrowIfOutOfRange(0, 99);
            fix.ThrowIfOutOfRange(0, 99);

            Index = index;
            Number = number;
            Fix = fix;
            IsValid = true;
        }

        /// <summary>Initializes a new instance of the <see cref="PrereleaseVersion"/> struct</summary>
        /// <param name="preRelName">name of the pre-release. (see remarks)</param>
        /// <param name="preRelNumber">Pre-release number for this build [0-99]</param>
        /// <param name="preRelFix">Pre-release fix for this build [0-99]</param>
        /// <exception cref="ArgumentException">Argument does not match expectations</exception>
        /// <remarks>
        /// The <paramref name="preRelName"/> must match one of the 8 well-known pre-release names. It is
        /// compared using <see cref="StringComparison.OrdinalIgnoreCase"/>.
        /// </remarks>
        public PrereleaseVersion( string preRelName, int preRelNumber, int preRelFix )
        {
            preRelName.ThrowIfNullOrWhiteSpace();
            preRelNumber.ThrowIfOutOfRange(0, 99);
            preRelFix.ThrowIfOutOfRange(0, 99);

            var q = from name in PreReleaseNames.Select( ( n, i ) => new { Name = n, Index = i } )
                    where string.Equals( name.Name, preRelName, StringComparison.OrdinalIgnoreCase )
                    select name;

            var nameIndex = q.FirstOrDefault( ) ?? throw new ArgumentException("Invalid pre-release name", nameof(preRelName));
            Index = nameIndex.Name == null ? -1 : nameIndex.Index;

            if(Index < 0 || Index > 7)
            {
                throw new ArgumentException( "Expected value in range [0-7]", nameof( preRelName ) );
            }

            Number = preRelNumber;
            Fix = preRelFix;
            IsValid = true;
        }

        /// <summary>Gets a value indicating whether this instance is valid or not</summary>
        /// <remarks>
        /// Default construction will set ALL members to 0, and therefore this is false unless
        /// one of the true initializing constructors is used.
        /// </remarks>
        public bool IsValid { get; }

        /// <summary>Gets the name of this pre-release</summary>
        public string Name => Index >= 0 ? PreReleaseNames[ Index ] : string.Empty;

        /// <summary>Gets the short name of this pre-release</summary>
        public string ShortName => Index >= 0 ? PreReleaseShortNames[ Index ] : string.Empty;

        /// <summary>Gets the index value of this pre-release</summary>
        /// <remarks>
        /// The index value is a numeric index into a set of 8 well-known names. Thus, it has the
        /// range [0-7].
        /// </remarks>
        public int Index { get; }

        /// <summary>Gets the Pre-Release number [0-99]</summary>
        public int Number { get; }

        /// <summary>Gets the Pre-Release fix [0-99]</summary>
        public int Fix { get; }

        /// <summary>Formats this instance as a string according to the rules of a Constrained Semantic Version</summary>
        /// <remarks>
        /// This assumes the Full (F) format of the pre-release information
        /// </remarks>
        /// <returns>The formatted version information</returns>
        public override string ToString( ) => ToString( "F", CultureInfo.InvariantCulture );

        /// <summary>Formats this instance as a string according to the rules of a Constrained Semantic Version</summary>
        /// <param name="format">Format string to use. "F" is assumed if not provided</param>
        /// <param name="formatProvider">Format provider [ignored; formatting of CSemver strings is well-defined externally and NOT subject to localization]</param>
        /// <returns>Formatted string version of this pre-release</returns>
        /// <exception cref="ArgumentException">Invalid format specifier</exception>
        /// <remarks>
        /// The supported formats are:
        /// <list>
        /// <item><term>F</term><description>Use full pre-release name</description></item>
        /// <item><term>S</term><description>Use short form of pre-release name</description></item>
        /// <item><term>Z</term><description>Assume a CI build and ALWAYs include 0 values for <see cref="Number"/> and <see cref="Fix"/></description></item>
        /// </list>
        /// The 'F' and 'S' are mutually exclusive values, only ONE of them at a time is valid. 'Z' is allowed in combination with either or alone.
        /// </remarks>
        public string ToString( string? format, IFormatProvider? formatProvider )
        {
            format ??= "F";
            bool useShortForm = false;
            bool alwaysIncludeZero = false;
            switch(format)
            {
            case "F":
                break;

            case "S":
                useShortForm = true;
                break;

            case "Z":
                alwaysIncludeZero = true;
                break;

            case "SZ":
            case "ZS":
                useShortForm = true;
                alwaysIncludeZero = true;
                break;

            default:
                throw new ArgumentException( $"Invalid format specifier: '{format}'", nameof( format ) );
            }

            // Format provider is ignored; string form is well defined externally based on invariant culture
            // formatProvider ??= CultureInfo.InvariantCulture;
            var bldr = new StringBuilder( );

            bldr.Append( '-' )
                .Append( useShortForm ? ShortName : Name );

            if(Number > 0 || Fix > 0 || alwaysIncludeZero)
            {
                bldr.AppendFormat( CultureInfo.InvariantCulture, ".{0}", Number );
                if(Fix > 0 || alwaysIncludeZero)
                {
                    bldr.AppendFormat( CultureInfo.InvariantCulture, ".{0}", Fix );
                }
            }

            return bldr.ToString();
        }

        private static readonly string[] PreReleaseNames = ["alpha", "beta", "delta", "epsilon", "gamma", "kappa", "prerelease", "rc"];
        private static readonly string[] PreReleaseShortNames = ["a", "b", "d", "e", "g", "k", "p", "r"];
    }
}
