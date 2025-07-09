// -----------------------------------------------------------------------
// <copyright file="FormatProviderExtensions.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Ubiquity.NET.Versioning
{
    internal static class FormatProviderExtensions
    {
        public static bool IsCaseSensitive([NotNullWhen(true)]this IFormatProvider? provider, [CallerArgumentExpression(nameof(provider))] string? exp = null)
        {
            var ordering = (AlphaNumericOrdering?)provider?.GetFormat(typeof(AlphaNumericOrdering));
            return ordering is not null && ordering.Value == AlphaNumericOrdering.CaseSensitive;
        }

        public static void ThrowIfCaseSensitive(this IFormatProvider? provider, [CallerArgumentExpression(nameof(provider))] string? exp = null)
        {
            if(provider is not null && provider.IsCaseSensitive())
            {
                throw new ArgumentException("Format provider must be <null> or provide a CaseInsensitive comparison", exp);
            }
        }
    }
}
