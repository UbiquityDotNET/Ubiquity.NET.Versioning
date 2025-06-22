// -----------------------------------------------------------------------
// <copyright file="ResourcesExtensions.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace Ubiquity.NET.Versioning.Properties
{
    internal static class ResourcesExtensions
    {
        internal static CompositeFormat AsFormat([NotNull] this string? self)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(self);
            return CompositeFormat.Parse(self);
        }

        internal static string Format<TArg0>([NotNull]this CompositeFormat? self, TArg0 arg0)
        {
            ArgumentNullException.ThrowIfNull(self);
            return string.Format(CultureInfo.CurrentCulture, self, arg0);
        }

        internal static string Format<TArg0, TArg1>([NotNull]this CompositeFormat? self, TArg0 arg0, TArg1 arg1)
        {
            ArgumentNullException.ThrowIfNull(self);
            return string.Format(CultureInfo.CurrentCulture, self, arg0, arg1);
        }

        internal static string Format<TArg0, TArg1, TArg3>([NotNull]this CompositeFormat? self, TArg0 arg0, TArg1 arg1, TArg3 arg3)
        {
            ArgumentNullException.ThrowIfNull(self);
            return string.Format(CultureInfo.CurrentCulture, self, arg0, arg1, arg3);
        }

        internal static string Format<TArg0>([NotNull]this string? self, TArg0 arg0)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(self);
            return string.Format(CultureInfo.CurrentCulture, self.AsFormat(), arg0);
        }

        internal static string Format<TArg0, TArg1>([NotNull]this string? self, TArg0 arg0, TArg1 arg1)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(self);
            return string.Format(CultureInfo.CurrentCulture, self.AsFormat(), arg0, arg1);
        }

        internal static string Format<TArg0, TArg1, TArg3>([NotNull]this string? self, TArg0 arg0, TArg1 arg1, TArg3 arg3)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(self);
            return string.Format(CultureInfo.CurrentCulture, self.AsFormat(), arg0, arg1, arg3);
        }
    }
}
