// -----------------------------------------------------------------------
// <copyright file="ParseBuildVersionXml.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Ubiquity.NET.Versioning.Build.Tasks
{
    public class ParseBuildVersionXml
        : Task
    {
        [Required]
        public string? BuildVersionXml { get; set; }

        [Output]
        public string? BuildMajor { get; private set; }

        [Output]
        public string? BuildMinor { get; private set; }

        [Output]
        public string? BuildPatch { get; private set; }

        [Output]
        public string? PreReleaseName { get; private set; }

        [Output]
        public string? PreReleaseNumber { get; private set; }

        [Output]
        public string? PreReleaseFix { get; private set; }

        [SuppressMessage( "Design", "CA1031:Do not catch general exception types", Justification = "Caught exceptions are logged as errors" )]
        public override bool Execute( )
        {
            try
            {
                Log.LogMessage(MessageImportance.Low, $"+{nameof(ParseBuildVersionXml)} Task");

                using var stream = File.OpenText( BuildVersionXml );
                var xdoc = System.Xml.Linq.XDocument.Load( stream, System.Xml.Linq.LoadOptions.None );
                var data = xdoc.Element( "BuildVersionData" );

                foreach( var attrib in data.Attributes( ) )
                {
                    switch( attrib.Name.LocalName )
                    {
                    case "BuildMajor":
                        Log.LogMessage(MessageImportance.Low, $"BuildMajor: {attrib.Value}");
                        BuildMajor = attrib.Value;
                        break;

                    case "BuildMinor":
                        Log.LogMessage(MessageImportance.Low, $"BuildMinor: {attrib.Value}");
                        BuildMinor = attrib.Value;
                        break;

                    case "BuildPatch":
                        Log.LogMessage(MessageImportance.Low, $"BuildPatch: {attrib.Value}");
                        BuildPatch = attrib.Value;
                        break;

                    case "PreReleaseName":
                        Log.LogMessage(MessageImportance.Low, $"PreReleaseName: {attrib.Value}");
                        PreReleaseName = attrib.Value;
                        break;

                    case "PreReleaseNumber":
                        Log.LogMessage(MessageImportance.Low, $"PreReleaseNumber: {attrib.Value}");
                        PreReleaseNumber = attrib.Value;
                        break;

                    case "PreReleaseFix":
                        Log.LogMessage(MessageImportance.Low, $"PreReleaseFix: {attrib.Value}");
                        PreReleaseFix = attrib.Value;
                        break;

                    default:
                        Log.LogWarning( "Unexpected attribute {0}", attrib.Name.LocalName );
                        break;
                    }
                }

                // correct malformed values
                if( string.IsNullOrWhiteSpace( PreReleaseName ) )
                {
                    Log.LogMessage(MessageImportance.Low, "PreReleaseName not provided, forcing PreReleaseNumber and PreReleaseFix == 0");
                    PreReleaseNumber = "0";
                    PreReleaseFix = "0";
                }

                if( PreReleaseNumber == "0" && PreReleaseFix != "0")
                {
                    Log.LogMessage(MessageImportance.Low, "PreReleaseNumber is 0; forcing PreReleaseFix == 0");
                    PreReleaseFix = "0";
                }

                Log.LogMessage(MessageImportance.Low, $"-{nameof(ParseBuildVersionXml)} Task");
                return true;
            }
            catch(Exception ex)
            {
                Log.LogErrorFromException(ex, showStackTrace: true);
                return false;
            }
        }
    }
}
