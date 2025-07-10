// -----------------------------------------------------------------------
// <copyright file="AlphaNumericOrdering.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Ubiquity.NET.Versioning
{
    /// <summary>Identifies the sort ordering expected for a given version</summary>
    public enum AlphaNumericOrdering
    {
        /// <summary>Indicates an invalid sort ordering</summary>
        /// <remarks>
        /// This value is the default for this type and is ALWAYS invalid.
        /// </remarks>
        None = 0,

        /// <summary>Ordering of pre-release AlphaNumeric Identifiers uses case sensitive ordering</summary>
        CaseSensitive,

        /// <summary>Ordering of pre-release AlphaNumeric Identifiers uses case insensitive ordering</summary>
        CaseInsensitive
    }
}
