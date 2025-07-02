// -----------------------------------------------------------------------
// <copyright file="SemVerFormatProvider.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace Ubiquity.NET.Versioning
{
    /// <summary>Format provider for a Semantic version that determines the ordering for parsing</summary>
    /// <remarks>
    /// SemVer has a culture neutral formatting when converted to a string and this is not needed for that
    /// scenario. However, for parsing a <see cref="SemVer"/> is created and that needs to know what ordering
    /// should apply to the resulting version. Thus, the static members of this class handle setting that
    /// based on whether the source is case sensitive or not.
    /// <note type="note">
    /// <see cref="CSemVer"/> and <see cref="CSemVerCI"/> ALWAYS use a Case Sensitive comparison and therefore
    /// parsing those, using this formatter is ignored.
    /// </note>
    /// </remarks>
    public sealed class SemVerFormatProvider
        : IFormatProvider
    {
        internal SemVerFormatProvider( AlphaNumericOrdering ordering )
        {
            Ordering = ordering;
        }

        internal AlphaNumericOrdering Ordering { get; }

        /// <inheritdoc/>
        object? IFormatProvider.GetFormat( Type? formatType )
        {
            return formatType == typeof(AlphaNumericOrdering) ? Ordering : null;
        }

        /// <summary>Gets a formatter that supports case sensitive comparisons of parsed <see cref="SemVer"/> values</summary>
        public static readonly IFormatProvider CaseSensitive = new SemVerFormatProvider(AlphaNumericOrdering.CaseSensitive);

        /// <summary>Gets a formatter that supports case insensitive comparisons of parsed <see cref="SemVer"/> values</summary>
        public static readonly IFormatProvider CaseInsensitive = new SemVerFormatProvider(AlphaNumericOrdering.CaseInsensitive);
    }
}
