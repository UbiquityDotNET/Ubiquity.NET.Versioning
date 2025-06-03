// -----------------------------------------------------------------------
// <copyright file="CreateVersionInfo.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

// NOTE: Due to constraints, limitations and general issues with MSBUILD Tasks and dependency resolution
//       This must NOT have any dependencies and therefore, does it all locally. This is not as complete
//       as what is offered in the Ubiquity.NET.Versioning library but enables the tasks to function with
//       the least of changes to the consumer (Update to package name and version, gets updated/corrected
//       version)
//
// For gory details of the problems of creating a task with dependencies
// See: https://natemcmaster.com/blog/2017/11/11/msbuild-task-with-dependencies/
namespace Ubiquity.NET.Versioning.Build.Tasks
{
    public class CreateVersionInfo
        : Task
    {
        [Required]
        public int BuildMajor { get; private set; }

        [Required]
        public int BuildMinor { get; private set; }

        [Required]
        public int BuildPatch { get; private set; }

        public string? PreReleaseName { get; private set; }

        public int PreReleaseNumber { get; private set; }

        public int PreReleaseFix { get; private set; }

        public string? CiBuildIndex { get; set; }

        public string? CiBuildName { get; set; }

        public string? BuildMeta { get; set; }

        [Output]
        public string? CSemVer { get; set; }

        [Output]
        public string? ShortCSemVer { get; set; }

        [Output]
        public ushort? FileVersionMajor { get; set; }

        [Output]
        public ushort? FileVersionMinor { get; set; }

        [Output]
        public ushort? FileVersionBuild { get; set; }

        [Output]
        public ushort? FileVersionRevision { get; set; }

        [SuppressMessage( "Design", "CA1031:Do not catch general exception types", Justification = "Caught exceptions are logged as errors" )]
        public override bool Execute( )
        {
            try
            {
                Log.LogMessage(MessageImportance.Low, $"+{nameof(CreateVersionInfo)} Task");

                if(!ValidateInput())
                {
                    Log.LogError("Input validation failed");
                    return false;
                }

                Log.LogMessage(MessageImportance.Low, "CiBuildIndex={0}", CiBuildIndex ?? string.Empty);
                Log.LogMessage(MessageImportance.Low, "CiBuildName={0}", CiBuildName ?? string.Empty);
                Log.LogMessage(MessageImportance.Low, "BuildMeta={0}", BuildMeta ?? string.Empty);
                Log.LogMessage(MessageImportance.Low, "PreReleaseName={0}", PreReleaseName ?? string.Empty);
                Log.LogMessage(MessageImportance.Low, "PreReleaseNumber={0}", PreReleaseNumber);
                Log.LogMessage(MessageImportance.Low, "PreReleaseFix={0}", PreReleaseFix);

                int preRelIndex = ComputePreReleaseIndex( PreReleaseName!);
                Log.LogMessage(MessageImportance.Low, "PreRelIndex={0}", preRelIndex);

                CSemVer = CreateSemVerString( preRelIndex );
                Log.LogMessage(MessageImportance.Low, "CSemVer={0}", CSemVer ?? string.Empty);

                ShortCSemVer = CreateSemVerString( preRelIndex, useShortForm: true, includeMetadata: false );
                Log.LogMessage(MessageImportance.Low, "ShortCSemVer={0}", ShortCSemVer ?? string.Empty);

                SetFileVersion( preRelIndex );
                return true;
            }
            catch(Exception ex)
            {
                Log.LogErrorFromException( ex, showStackTrace: true );
                return false;
            }
            finally
            {
                Log.LogMessage(MessageImportance.Low, $"-{nameof(CreateVersionInfo)} Task");
            }
        }

        /// <summary>Creates a formatted CSemver from the properties of this instance</summary>
        /// <param name="preRelIndex">Numeric form of the build index [(-1)-7] where -1 indicates not a pre-release</param>
        /// <param name="useShortForm">Flag to indicate if the short form is desired</param>
        /// <param name="includeMetadata">Flag to indicate if the metadata is included</param>
        /// <param name="alwaysIncludeZero">Flag to indicate if a 0 pre-release is ALWAYs included [see remarks]</param>
        /// <returns>Formatted CSemVer string</returns>
        /// <remarks>
        /// <para>The <paramref name="alwaysIncludeZero"/> is for legacy behavior and should generally be left at the default. In
        /// prior releases the pre-release number and fix were always included for pre-release builds in the short form. In
        /// the current version based on CSemVer v1.0.0-rc.1 the behavior is the same as for a full version. (The Number is omitted
        /// unless it is > 0 OR Fix >0).</para>
        /// <para>Additionally, the short form uses a two digit leading 0 conversion.</para>
        /// </remarks>
        private string CreateSemVerString( int preRelIndex, bool useShortForm = false, bool includeMetadata = true, bool alwaysIncludeZero = false)
        {
            var bldr = new StringBuilder()
                          .AppendFormat(CultureInfo.InvariantCulture, "{0}.{1}.{2}", BuildMajor, BuildMinor, BuildPatch);

            bool isPreRelease = preRelIndex >= 0;
            if(isPreRelease)
            {
                bldr.Append( '-' )
                    .Append( useShortForm ? PreReleaseShortNames[ preRelIndex ] : PreReleaseNames[ preRelIndex ] );

                if(PreReleaseNumber > 0 || PreReleaseFix > 0 || alwaysIncludeZero)
                {
                    bldr.AppendFormat( CultureInfo.InvariantCulture, useShortForm ? "{0:D02}" : ".{0}", PreReleaseNumber );
                    if(PreReleaseFix > 0 || alwaysIncludeZero)
                    {
                        bldr.AppendFormat( CultureInfo.InvariantCulture, useShortForm ? "-{0:D02}" : ".{0}", PreReleaseFix );
                    }
                }
            }

            if(!string.IsNullOrWhiteSpace( CiBuildIndex ) && !string.IsNullOrWhiteSpace( CiBuildName ))
            {
                bldr.AppendFormat( CultureInfo.InvariantCulture, isPreRelease ? ".ci.{0}.{1}" : "--ci.{0}.{1}", CiBuildIndex, CiBuildName );
            }

            if(!string.IsNullOrWhiteSpace( BuildMeta ) && includeMetadata)
            {
                bldr.AppendFormat( CultureInfo.InvariantCulture, $"+{BuildMeta}" );
            }

            return bldr.ToString();
        }

        private void SetFileVersion( int preRelIndex )
        {
            UInt64 orderedVersion = ((ulong)BuildMajor * MulMajor) + ((ulong)BuildMinor * MulMinor) + (((ulong)BuildPatch + 1) * MulPatch);

            if(preRelIndex >= 0)
            {
                orderedVersion -= MulPatch - 1; // Remove the fix+1 multiplier
                orderedVersion += (ulong)preRelIndex * MulName;
                orderedVersion += ((ulong)PreReleaseNumber) * MulNum;
                orderedVersion += (ulong)PreReleaseFix;
            }

            Log.LogMessage(MessageImportance.Low, "orderedVersion={0}", orderedVersion);

            bool isCiBuild = !string.IsNullOrWhiteSpace(CiBuildIndex) && !string.IsNullOrWhiteSpace(CiBuildName);
            UInt64 fileVersion64 = (orderedVersion << 1) + (isCiBuild ? 1ul : 0ul);
            FileVersionRevision = (UInt16)(fileVersion64 % 65536);

            UInt64 rem = (fileVersion64 - FileVersionRevision.Value) / 65536;
            FileVersionBuild = (UInt16)(rem % 65536);

            rem = (rem - FileVersionBuild.Value) / 65536;
            FileVersionMinor = (UInt16)(rem % 65536);

            rem = (rem - FileVersionMinor.Value) / 65536;
            FileVersionMajor = (UInt16)(rem % 65536);

            Log.LogMessage(MessageImportance.Low, "FileVersionMajor={0}", FileVersionMajor);
            Log.LogMessage(MessageImportance.Low, "FileVersionMinor={0}", FileVersionMinor);
            Log.LogMessage(MessageImportance.Low, "FileVersionBuild={0}", FileVersionBuild);
            Log.LogMessage(MessageImportance.Low, "FileVersionRevision={0}", FileVersionRevision);
        }

        private bool ValidateInput( )
        {
            // Try to report as many input errors at once as is possible
            // (That is, don't stop at first one - so all possible errors
            bool hasInputError = false;
            if(BuildMajor < 0 || BuildMajor > 99999)
            {
                Log.LogError( "BuildMajor value must be in range [0-99999]" );
                hasInputError = true;
            }

            if(BuildMinor < 0 || BuildMinor > 49999)
            {
                Log.LogError( "BuildMinor value must be in range [0-49999]" );
                hasInputError = true;
            }

            if(BuildPatch < 0 || BuildPatch > 9999)
            {
                Log.LogError( "BuildPatch value must be in range [0-99999]" );
                hasInputError = true;
            }

            if(!string.IsNullOrWhiteSpace( PreReleaseName ))
            {
                if(!PreReleaseNames.Contains( PreReleaseName, StringComparer.InvariantCultureIgnoreCase ))
                {
                    if(!PreReleaseShortNames.Contains( PreReleaseName, StringComparer.InvariantCultureIgnoreCase ))
                    {
                        Log.LogError( "PreRelease Name is unknown" );
                        hasInputError = true;
                    }
                }
            }

            if(PreReleaseNumber < 0 || PreReleaseNumber > 99)
            {
                Log.LogError( "PreReleaseNumber value must be in range [0-99]" );
                hasInputError = true;
            }

            if(PreReleaseFix < 0 || PreReleaseFix > 99)
            {
                Log.LogError( "PreReleaseFix value must be in range [0-99]" );
                hasInputError = true;
            }

            if(string.IsNullOrWhiteSpace( CiBuildIndex ) != string.IsNullOrWhiteSpace( CiBuildName ))
            {
                Log.LogError( "If CiBuildIndex is set then CiBuildName must also be set; If CiBuildIndex is NOT set then CiBuildName must not be set." );
                hasInputError = true;
            }

            if(CiBuildIndex != null && !CiBuildIdRegEx.IsMatch( CiBuildIndex ))
            {
                Log.LogError( "CiBuildIndex does not match syntax defined by CSemVer" );
                hasInputError = true;
            }

            if(CiBuildName != null && !CiBuildIdRegEx.IsMatch( CiBuildName ))
            {
                Log.LogError( "CiBuildName does not match syntax defined by CSemVer" );
                hasInputError = true;
            }

            if(!string.IsNullOrEmpty( BuildMeta ) && BuildMeta!.Length > 20)
            {
                Log.LogError( "Build metadata, if provided, must not exceed 20 characters" );
                hasInputError = true;
            }

            return !hasInputError;
        }

        //private void LogError(
        //    string code,
        //    /*[StringSyntax(StringSyntaxAttribute.CompositeFormat)]*/ string message,
        //    params object[] messageArgs
        //    )
        //{
        //    Log.LogError("Task", code, null, null, 0, 0, 0, 0, message, messageArgs);
        //}

        private static int ComputePreReleaseIndex( string preRelName )
        {
            int index = Find( PreReleaseNames, preRelName ).Index;
            return index >= 0 ? index : Find( PreReleaseShortNames, preRelName ).Index;
        }

        private static (string Value, int Index) Find( string[] values, string value )
        {
            var q = from element in values.Select( ( v, i ) => (Value: v, Index: i ) )
                    where string.Equals( element.Value, value, StringComparison.OrdinalIgnoreCase )
                    select element;

            var result = q.FirstOrDefault();
            return result == default ? (string.Empty, -1) : result;
        }

        private const ulong MulNum = 100;
        private const ulong MulName = MulNum * 100;
        private const ulong MulPatch = (MulName * 8) + 1;
        private const ulong MulMinor = MulPatch * 10000;
        private const ulong MulMajor = MulMinor * 50000;

        private static readonly string[] PreReleaseNames = ["alpha", "beta", "delta", "epsilon", "gamma", "kappa", "prerelease", "rc"];
        private static readonly string[] PreReleaseShortNames = ["a", "b", "d", "e", "g", "k", "p", "r"];
        private static readonly Regex CiBuildIdRegEx = new(@"\A[0-9a-zA-Z\-]+\Z");
    }
}
