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
                using var stream = File.OpenText( BuildVersionXml );
                var xdoc = System.Xml.Linq.XDocument.Load( stream, System.Xml.Linq.LoadOptions.None );
                var data = xdoc.Element( "BuildVersionData" );

                foreach( var attrib in data.Attributes( ) )
                {
                    switch( attrib.Name.LocalName )
                    {
                    case "BuildMajor":
                        BuildMajor = attrib.Value;
                        break;

                    case "BuildMinor":
                        BuildMinor = attrib.Value;
                        break;

                    case "BuildPatch":
                        BuildPatch = attrib.Value;
                        break;

                    case "PreReleaseName":
                        PreReleaseName = attrib.Value;
                        break;

                    case "PreReleaseNumber":
                        PreReleaseNumber = attrib.Value;
                        break;

                    case "PreReleaseFix":
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
                    PreReleaseNumber = "0";
                    PreReleaseFix = "0";
                }

                if( PreReleaseNumber == "0" )
                {
                    PreReleaseFix = "0";
                }

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
