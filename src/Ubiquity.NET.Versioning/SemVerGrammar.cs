// -----------------------------------------------------------------------
// <copyright file="SemVerGrammar.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;

using Sprache;

using Ubiquity.NET.Versioning.Properties;

namespace Ubiquity.NET.Versioning
{
    // Sprache grammar for a Semantic version
    // See: https://semver.org
    internal static class SemVerGrammar
    {
        private static bool IsNonZeroAsciiDigit( char c )
        {
            return c != '0' && Char.IsAsciiDigit( c );
        }

        private static Parser<char> Dot => Parse.Char( '.' );

        private static Parser<char> Dash => Parse.Char( '-' );

        private static Parser<char> Plus => Parse.Char( '+' );

        private static Parser<char> Zero => Parse.Char( '0' );

        private static Parser<char> Letter => Parse.Char( Char.IsAsciiLetter, "<letter>" );

        private static Parser<char> PositiveDigit => Parse.Char( IsNonZeroAsciiDigit, "<positive digit>" );

        private static Parser<char> Digit => Parse.Char( Char.IsAsciiDigit, "<digit>");

        private static Parser<char> NonDigit => Letter.Or( Dash ).Named("<non-digit>");

        private static Parser<char> IdentifierChar => Digit.Or( NonDigit ).Named("<identifier character>");

        private static Parser<string> IdentifierChars => IdentifierChar.AtLeastOnce().Named("<Identifier characters>").Text();

        private static Parser<string> Digits => Digit.AtLeastOnce().Named("<digits>").Text();

        private static Parser<string> AlphaNumericIdentifier => AlphaNumericId().Named("<alphanumeric identifier>");

        // SemVer.org BNF form of this is... Confusing and circular.
        // Bottom line is - that as long as it consists of ALL IdentifierCharacters
        // AND at least one of them is not a digit then its an AlphaNumericIdentifier
        private static Parser<string> AlphaNumericId()
        {
            return (i) =>
            {
                IResult<string> r = IdentifierChars(i);
                return !r.WasSuccessful || r.Value.Any(c=>!Char.IsAsciiDigit(c))
                        ? r
                        : Result.Failure<string>(i, Resources.alphanumericIdentifier_is_missing_a_non_digit_character, ["<non-digit>"]);
            };
        }

        private static Parser<string> NumericIdentifier
            => Zero.Once()
               .Or(Parse.Identifier(PositiveDigit, Digit))
               .Named("<numeric identifier>")
               .Text();

        public static Parser<string> PreReleaseIdentifier => AlphaNumericIdentifier.Or( NumericIdentifier ).Named("<prerelease-identifier");

        public static Parser<string> BuildIdentifier => AlphaNumericIdentifier.Or(Digits).Named("<build-identifier");

        // Formally, SemVer has no limits on a "numeric identifier" so use a BigInteger
        // and worry about down conversion in consumers
        private static Parser<BigInteger> IntegralValue
            => from number in NumericIdentifier.Text()
               select BigInteger.Parse( number, NumberStyles.None, CultureInfo.InvariantCulture );

        private static Parser<IEnumerable<string>> DotSeparatedPreReleaseIdentifiers
            => PreReleaseIdentifier.DelimitedBy( Dot );

        private static Parser<IEnumerable<string>> PreRelease => DotSeparatedPreReleaseIdentifiers.Named( "<prerelease-identifier>(.<prerelease-identifier>)+" );

        private static Parser<IEnumerable<string>> DotSeparatedBuildIdentifiers
            => BuildIdentifier.DelimitedBy( Dot );

        private static Parser<IEnumerable<string>> Build => DotSeparatedBuildIdentifiers.Named( "<build-identifier>(.<build-identifier>)+" );

        private static Parser<IEnumerable<string>> PreReleaseData
            => from leading in Dash
               from preRel in PreRelease
               select preRel;

        private static Parser<IEnumerable<string>> BuildMetaData
            => from leading in Plus
               from build in Build
               select build;

        private static Parser<SemVer> VersionCore
            => from major in IntegralValue.Named("<major>")
               from sep1 in Dot
               from minor in IntegralValue.Named("<minor>")
               from sep2 in Dot
               from patch in IntegralValue.Named("<patch>")
               select new SemVer( major, minor, patch );

        public static Parser<SemVer> SemanticVersion
            => ( from vc in VersionCore.Named("<version core>")
                 from preRel in PreReleaseData.Named("<pre-release>").XOptional()
                 from build in BuildMetaData.Named("<build>").XOptional()
                 select new SemVer(vc.Major, vc.Minor, vc.Patch, preRel.GetOrElse([]), build.GetOrElse([]))
               ).End();
    }
}
