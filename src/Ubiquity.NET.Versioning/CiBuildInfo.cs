// -----------------------------------------------------------------------
// <copyright file="CiBuildInfo.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Text.RegularExpressions;

namespace Ubiquity.NET.Versioning
{
    /// <summary>Represents the Continuous Integration (CI) state for a CSemVer-CI value</summary>
    /// <seealso href="https://csemver.org/"/>
    public readonly partial record struct CiBuildInfo
        : IFormattable
    {
        /// <summary>Initializes a new instance of the <see cref="CiBuildInfo"/> struct.</summary>
        /// <param name="index">Build index for the build</param>
        /// <param name="name">Build Name for the build</param>
        /// <remarks>
        /// The syntax for the <paramref name="index"/> and <paramref name="name"/> are restricted to only values that
        /// match the regular expression '\A[0-9a-zA-Z\-]+\Z' (ASCII alphanumeric plus a Hyphen).
        /// </remarks>
        public CiBuildInfo( string index, string name )
        {
            index.ThrowIfNullOrWhiteSpace();
            name.ThrowIfNullOrWhiteSpace();
            index.ThrowIfNotMatch( CiBuildIdRegEx );
            name.ThrowIfNotMatch( CiBuildIdRegEx );

            BuildIndex = index;
            BuildName = name;
        }

        /// <summary>Gets the CI build information based on the rules for CSemVer-CI</summary>
        /// <returns>String format of the build information</returns>
        /// <remarks>
        /// This API Always uses the pre-release "P" form as that is the most common. Direct use
        /// of this type without explicit specification of the formatting is NOT recommended. This
        /// mostly exists to support debugger visibility.
        /// </remarks>
        public override string ToString( )
        {
            return ToString( "P", null );
        }

        /// <summary>Gets the CI build information based on the rules for CSemVer-CI</summary>
        /// <param name="format">Format specifier to use for formatting the string</param>
        /// <param name="formatProvider">[ignored] formatting uses a well-known pattern that is independent of any localization</param>
        /// <returns>String format of the build information</returns>
        /// <remarks>
        /// This API Always supports two distinct format options based on the CSemVer-CI spec.
        /// <list>
        /// <item><term>P</term><description>Pre-release format using a single '.' as the leading delimiter</description></item>
        /// <item><term>R</term><description>Release format using a double dash '--' as the leading delimiter</description></item>
        /// </list>
        /// </remarks>
        public string ToString( string? format, IFormatProvider? formatProvider )
        {
            format ??= "P";
            return format switch
            {
                "P" => $".ci.{BuildIndex}.{BuildName}",
                "R" => $"--ci.{BuildIndex}.{BuildName}",
                _ => throw new ArgumentException( "Unknown format specifier", nameof( format ) ),
            };
        }

        /// <summary>Gets a value indicating whether this instance contains valid information</summary>
        /// <remarks>
        /// Default constructed structs contain all zeros, thus even the properties annotated as not <see langword="null"/>
        /// are, in fact, <see langword="null"/>. This property is used to validate that the structure was created via
        /// the constructor that provides valid values. (That is, a default constructed instance, is NOT considered valid)
        /// </remarks>
        public bool IsValid =>
            !string.IsNullOrWhiteSpace(BuildIndex)
          && !string.IsNullOrWhiteSpace(BuildName);

        /// <summary>Gets the Build Index for this build</summary>
        public string BuildIndex { get; } = string.Empty;

        /// <summary>Gets the Build name for this build</summary>
        public string BuildName { get; } = string.Empty;

        private static readonly Regex CiBuildIdRegEx = GetGeneratedBuildIdRegex();

        [GeneratedRegex( @"\A[0-9a-zA-Z\-]+\Z" )]
        private static partial Regex GetGeneratedBuildIdRegex( );
    }
}
