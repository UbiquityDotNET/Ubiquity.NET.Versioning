// -----------------------------------------------------------------------
// <copyright file="FormatProviderExtensions.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;

namespace Ubiquity.NET.Versioning
{
    internal static class FormatProviderExtensions
    {
        public static void ThrowIfCaseSensitive(this IFormatProvider? provider, [CallerArgumentExpression(nameof(provider))] string? exp = null)
        {
            if(provider is not null)
            {
                var ordering = (AlphaNumericOrdering?)provider.GetFormat(typeof(AlphaNumericOrdering));
                if(ordering is not null && ordering.Value == AlphaNumericOrdering.CaseSensitive)
                {
                    throw new ArgumentException("Format provider must be <null> or provide a CaseInsensitive comparison", exp);
                }
            }
        }

        public static AlphaNumericOrdering GetOrdering(this IFormatProvider provider)
        {
            ArgumentNullException.ThrowIfNull(provider);

            return (AlphaNumericOrdering?)provider.GetFormat(typeof(AlphaNumericOrdering)) ?? AlphaNumericOrdering.None;
        }
    }
}
