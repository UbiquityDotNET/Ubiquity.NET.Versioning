// -----------------------------------------------------------------------
// <copyright file="ParseResultExtensions.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using Sprache;

namespace Ubiquity.NET.Versioning
{
    internal static class ParseResultExtensions
    {
        internal static void ThrowIfFailed<T>([NotNull] this IResult<T> result, [CallerArgumentExpression(nameof(result))] string? exp = null)
        {
            if(result.Failed(out Exception? ex, exp))
            {
                throw ex;
            }
        }

        internal static bool Failed<T>(
            [NotNullWhen(false)] this IResult<T> result,
            [MaybeNullWhen(false)] out Exception ex,
            [CallerArgumentExpression(nameof(result))] string? exp = null
            )
        {
            if(result is null)
            {
                ex = new ArgumentNullException(exp);
                return true;
            }

            if(result.WasSuccessful)
            {
                ex = null;
                return false;
            }

            string[] expected = [ .. result.Expectations ];
            ex = expected.Length > 0
                ? new FormatException( $"Parsing Error: {result.Message} at position {result.Remainder.Column}; Expected: {string.Join(" | ", expected)}" )
                : new FormatException( $"Parsing Error: {result.Message} at position {result.Remainder.Column}" );

            return true;
        }
    }
}
