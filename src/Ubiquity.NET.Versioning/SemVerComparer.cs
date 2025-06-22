// -----------------------------------------------------------------------
// <copyright file="SemVerComparer.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;

namespace Ubiquity.NET.Versioning
{
    /// <summary>Static class to host comparison logic for various parts of a <see cref="SemVer"/></summary>
    public static class SemVerComparer
    {
        /// <summary>Case-sensitive comparison of AlphaNumeric IDs</summary>
        /// <remarks>
        /// Sadly, the SemVer specs are silent on the point of case sensitivity. Various implementations
        /// have taken different approaches to doing that. Generally, those choices were along OS platform
        /// lines. With multiple cross platform runtimes and support that becomes a real nightmare as ignoring
        /// the issue and how it impacts ordering can have VERY surprising results. Thus, this library requires
        /// explicit selection of the behavior for comparisons.
        /// <note type="important">
        /// There is NO support for mixed comparisons (Comparisons where one of the versions uses case-sensitivity
        /// but the other does not). Such comparisons are undefined. There is no way to know what the ordering is
        /// supposed to be for each. An application MUST ensure it is ordering versions in a consistent fashion
        /// and DOCUMENT what that is.
        /// </note>
        /// </remarks>
        [SuppressMessage( "Design", "CA1034:Nested types should not be visible", Justification = "Simpler to read, use and maintain this way" )]
        public static class CaseSensitive
        {
            /// <summary>Gets a comparer that compares the values of pre-release identifier</summary>
            /// <remarks>
            /// This comparison takes account of the rules for AlphaNumeric vs Numeric values and the
            /// combinations of them according to the rules of the SemVer spec §11.4. (case-insensitive)
            /// </remarks>
            private static IComparer<string> PrereleaseIdentifier { get; }
                = new PrereleaseIdentifierComparer( caseSensitive: true );

            /// <summary>Gets a comparer that compares the values of a pre-release list (case-insensitive)</summary>
            private static IComparer<IReadOnlyList<string>> PrereleaseIdentifierList { get; }
                = new PrereleaseIdentifierListComparer( PrereleaseIdentifier );

            /// <summary>Gets a comparer that compares two <see cref="Ubiquity.NET.Versioning.SemVer"/> instances using case sensitive comparison for AlphaNumeric Identifiers</summary>
            public static IComparer<SemVer> SemVer { get; }
                = new SemanticVersionComparer(PrereleaseIdentifierList);
        }

        /// <summary>Gets a comparer that compares the values of pre-release identifier</summary>
        /// <remarks>
        /// This comparison takes account of the rules for AlphaNumeric vs Numeric values and the
        /// combinations of them according to the rules of the SemVer spec §11.4. (case-insensitive)
        /// </remarks>
        private static IComparer<string> PrereleaseIdentifier { get; }
            = new PrereleaseIdentifierComparer();

        /// <summary>Gets a comparer that compares the values of a pre-release list (case-insensitive)</summary>
        private static IComparer<IReadOnlyList<string>> PrereleaseIdentifierList { get; }
            = new PrereleaseIdentifierListComparer( PrereleaseIdentifier );

        /// <summary>Gets a comparer that compares two <see cref="Ubiquity.NET.Versioning.SemVer"/> instances using case insensitive comparison for AlphaNumeric Identifiers</summary>
        public static IComparer<SemVer> SemVer { get; }
            = new SemanticVersionComparer(PrereleaseIdentifierList);
    }

    [SuppressMessage( "StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "DUH! It's file scoped" )]
    file class CoreVersionComparer
        : IComparer<SemVer>
    {
        public int Compare( SemVer? x, SemVer? y )
        {
            // account for easy cases first
            if(ReferenceEquals( x, y ))
            {
                return 0;
            }

            if(x is null)
            {
                return -1;
            }

            if(y is null)
            {
                return 1;
            }

            // SemVer.11.2
            int retVal = x.Major.CompareTo(y.Major);
            if(retVal != 0)
            {
                return retVal;
            }

            retVal = x.Minor.CompareTo( y.Minor );
            return retVal != 0 ? retVal : x.Patch.CompareTo( y.Patch );
        }

        /// <summary>Gets a comparer to compare only the core version parts (Major, Minor, Patch) of a <see cref="SemVer"/></summary>
        internal static IComparer<SemVer> Instance { get; }
            = new CoreVersionComparer();
    }

    [SuppressMessage( "StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "DUH! It's file scoped" )]
    file class PrereleaseIdentifierListComparer
        : IComparer<IReadOnlyList<string>>
    {
        public PrereleaseIdentifierListComparer( IComparer<string> elementComparer )
        {
            ElementComparer = elementComparer;
        }

        public int Compare( IReadOnlyList<string>? x, IReadOnlyList<string>? y )
        {
            // account for easy cases first
            if(ReferenceEquals( x, y ))
            {
                return 0;
            }

            if(x is null)
            {
                return -1;
            }

            if(y is null)
            {
                return 1;
            }

            // SemVer §11.3
            if(x.Count == 0 && y.Count != 0)
            {
                return 1;
            }

            if(x.Count != 0 && y.Count == 0)
            {
                return -1;
            }

            // SemVer §11.4.[1-3] (loop)
            int minCount = Math.Min(x.Count, y.Count);
            for(int i = 0; i < minCount; ++i)
            {
                int retVal = ElementComparer.Compare(x[i], y[i]);
                if(retVal != 0)
                {
                    return retVal;
                }
            }

            // SemVer.11.4.4 - longer set of pre-release ids has higher precedence
            return x.Count.CompareTo( y.Count );
        }

        private readonly IComparer<string> ElementComparer;
    }

    [SuppressMessage( "StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "DUH! It's file scoped" )]
    file class PrereleaseIdentifierComparer
        : IComparer<string>
    {
        public PrereleaseIdentifierComparer( bool caseSensitive = false )
        {
            CaseSensitive = caseSensitive;
        }

        public int Compare( string? x, string? y )
        {
            // account for easy cases first
            if(ReferenceEquals( x, y ))
            {
                return 0;
            }

            if(x is null)
            {
                return -1;
            }

            if(y is null)
            {
                return 1;
            }

            bool leftIsNumber = BigInteger.TryParse(x, NumberStyles.None, null, out BigInteger leftInt);
            bool rightIsNumber = BigInteger.TryParse(y, NumberStyles.None, null, out BigInteger rightInt);

            // SemVer §11.4.3 (If only one of them is a number; whichever one is ALWAYS lower precedence)
            if(leftIsNumber != rightIsNumber)
            {
                return leftIsNumber ? -1 : 1;
            }

            // Both are the same form compare based on that
            if(leftIsNumber && rightIsNumber)
            {
                return leftInt.CompareTo( rightInt ); // SemVer §11.4.1
            }

            // SemVer §11.4.2; CSemVer requires case insensitive; SemVer is non-specific
            return string.Compare( x, y, CaseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase );
        }

        private readonly bool CaseSensitive;
    }

    [SuppressMessage( "StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "DUH! It's file scoped" )]
    file class SemanticVersionComparer
        : IComparer<SemVer>
    {
        internal SemanticVersionComparer( IComparer<IReadOnlyList<string>> listComparer )
        {
            ListComparer = listComparer;
        }

        public int Compare( SemVer? x, SemVer? y )
        {
            int retVal = CoreVersionComparer.Instance.Compare(x, y);
            return x is null || y is null || retVal != 0
                 ? retVal
                 : ListComparer.Compare( x.PreRelease, y.PreRelease );
        }

        private readonly IComparer<IReadOnlyList<string>> ListComparer;
    }
}
