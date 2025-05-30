// -----------------------------------------------------------------------
// <copyright file="GlobalSuppressions.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage( "StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "MSBuild task library; Classes not documented" )]
[assembly: SuppressMessage( "StyleCop.CSharp.DocumentationRules", "SA1636: The file header copyright text should match the copyright text from the settings.", Justification = "analyzer broken: See: https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3271" )]

[assembly: SuppressMessage( "StyleCop.CSharp.DocumentationRules", "SA1201", Justification = "Ordering from analyzer is brain dead stupid and doesn't allow customization" )]
[assembly: SuppressMessage( "StyleCop.CSharp.DocumentationRules", "SA1202", Justification = "Ordering from analyzer is brain dead stupid and doesn't allow customization" )]
[assembly: SuppressMessage( "StyleCop.CSharp.DocumentationRules", "SA1203", Justification = "Ordering from analyzer is brain dead stupid and doesn't allow customization" )]
[assembly: SuppressMessage( "StyleCop.CSharp.DocumentationRules", "SA1204", Justification = "Ordering from analyzer is brain dead stupid and doesn't allow customization" )]
[assembly: SuppressMessage( "StyleCop.CSharp.DocumentationRules", "SA1214", Justification = "Ordering from analyzer is brain dead stupid and doesn't allow customization" )]
[assembly: SuppressMessage( "StyleCop.CSharp.DocumentationRules", "SA1215", Justification = "Ordering from analyzer is brain dead stupid and doesn't allow customization" )]
