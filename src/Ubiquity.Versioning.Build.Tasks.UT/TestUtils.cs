// -----------------------------------------------------------------------
// <copyright file="TestUtils.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.IO;

using Microsoft.Build.Definition;
using Microsoft.Build.Evaluation;

namespace Ubiquity.Versioning.Build.Tasks.UT
{
    internal static class TestUtils
    {
        internal static (string CiBuildIndex, string CiBuildName, string BuildTime) GetGeneratedCiBuildInfo( )
        {
            using var dummyCollection = new ProjectCollection();
            var options = new ProjectOptions()
            {
                ProjectCollection = dummyCollection
            };

            var project = Project.FromFile(Path.Combine(TestModuleFixtures.RepoRoot, "GeneratedVersion.props"), options);
            return (project.GetPropertyValue("CiBuildIndex"), project.GetPropertyValue("CiBuildName"), project.GetPropertyValue("BuildTime"));
        }
    }
}
