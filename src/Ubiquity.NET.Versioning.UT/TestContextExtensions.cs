// -----------------------------------------------------------------------
// <copyright file="TestContextExtensions.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ubiquity.NET.Versioning.UT
{
    internal static class TestContextExtensions
    {
        /// <summary>Determines the repository root from the test context</summary>
        /// <param name="ctx">Test context to use for determining the repository root</param>
        /// <returns>Root of the repository</returns>
        /// <exception cref="InvalidOperationException">If the root is indeterminable from the <paramref name="ctx"/></exception>
        /// <remarks>
        /// The repository root is generally not something the test needs or should care about. However, the test case that validates
        /// the assembly version itself requires access to the BuildVersion XML file used to validate against.
        /// </remarks>
        internal static string GetRepoRoot( this TestContext ctx )
        {
            // Currently this assumes a fixed relationship between the "TestRunDirectory" and the actual root
            // If that ever changes, this is the one place to change it.
            return !string.IsNullOrWhiteSpace( ctx.TestRunDirectory )
                   ? Path.GetFullPath( Path.Combine( ctx.TestRunDirectory, "..", "..", ".." ) )
                   : throw new InvalidOperationException( "Context.TestRunDirectory is not available" );
        }
    }
}
