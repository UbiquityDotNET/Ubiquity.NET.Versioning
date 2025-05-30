// -----------------------------------------------------------------------
// <copyright file="ModuleInitializer.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Runtime.CompilerServices;

using Microsoft.Build.Utilities.ProjectCreation;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    internal static void InitializeMSBuild()
    {
        MSBuildAssemblyResolver.Register();
    }
}
