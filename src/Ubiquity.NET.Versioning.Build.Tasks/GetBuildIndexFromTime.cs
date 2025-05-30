// -----------------------------------------------------------------------
// <copyright file="GetBuildIndexFromTime.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Globalization;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Ubiquity.NET.Versioning.Build.Tasks
{
    public class GetBuildIndexFromTime
        : Task
    {
        [Required]
        public DateTime TimeStamp { get; set; }

        [Output]
        public string? BuildIndex { get; private set; }

        public override bool Execute( )
        {
            // establish an increasing build index based on the number of seconds from a common UTC date
            var timeStamp = TimeStamp.ToUniversalTime( );

            // Upper 16 bits of the build number is the number of days since the common base value
            uint buildNumber = ((uint)(timeStamp - CommonBaseDate).Days) << 16;

            // Lower 16 bits is the number of seconds (divided by 2) since midnight (on the date of the time stamp)
            var midnightTodayUtc = new DateTime( timeStamp.Year, timeStamp.Month, timeStamp.Day, 0, 0, 0, DateTimeKind.Utc );
            buildNumber += (ushort)((timeStamp - midnightTodayUtc).TotalSeconds / 2);
            BuildIndex = buildNumber.ToString( CultureInfo.InvariantCulture );
            return true;
        }

        // Fixed point in time to use as reference for a build index.
        // Build index value is a string form of the number of days since this point in time + the number of seconds
        // since midnight of that time stamp.
        private static readonly DateTime CommonBaseDate = new( 2000, 1, 1, 0, 0, 0, DateTimeKind.Utc );
    }
}
