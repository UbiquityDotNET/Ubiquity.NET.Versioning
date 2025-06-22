// -----------------------------------------------------------------------
// <copyright file="CSemVerPrereleaseGrammar.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;

using Sprache;

namespace Ubiquity.NET.Versioning
{
    internal static class CSemVerPrereleaseGrammar
    {
        internal static Parser<byte> Index =>
            from s in SemVerGrammar.PreReleaseIdentifier
            let result = (Found: TryGetIndexFromName(s, out byte i), Index: i)
            where result.Found
            select result.Index;

        internal static Parser<byte> ByteDigits
            => from digits in Parse.Char(char.IsAsciiDigit, "<digit>").Repeat(1, 2).Text()
               select byte.Parse( digits, CultureInfo.InvariantCulture );

        internal static Parser<byte> DelimitedDigits
            => from deilim1 in Parse.Char('.')
               from val in ByteDigits
               select val;

        internal static Parser<(byte Number, byte Fix)> NumberAndFix
            => from number in DelimitedDigits
               from fix in DelimitedDigits.Optional()
               select (number, fix.GetOrDefault());

        internal static Parser<PrereleaseVersion> Prerelease
            => from index in Index
               from numFix in NumberAndFix
               select new PrereleaseVersion(index, numFix.Number, numFix.Fix);

        internal static bool TryGetIndexFromName(
            [NotNull] string preRelName,
            out byte index,
            [CallerArgumentExpression( nameof( preRelName ) )] string? exp = null
            )
        {
            ArgumentException.ThrowIfNullOrWhiteSpace( preRelName, exp );

            // CSemVer.7 - 'pre' and 'prerelease' are equivalent
            // so convert to canonical form here to simplify the determination of an index
            if(preRelName == "prerelease")
            {
                preRelName = "pre";
            }

            for(index = 0; index < ValidPrereleaseNames.Length; ++index)
            {
                string currentName = ValidPrereleaseNames[index];
                if( string.Equals( currentName, preRelName, StringComparison.OrdinalIgnoreCase ))
                {
                    return true;
                }
            }

            index = 0;
            return false;
        }

        internal static readonly string[] ValidPrereleaseNames = ["alpha", "beta", "delta", "epsilon", "gamma", "kappa", "pre", "rc"];
    }
}
