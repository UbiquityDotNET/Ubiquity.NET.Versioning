// -----------------------------------------------------------------------
// <copyright file="DateTimeOffsetExtensions.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Globalization;

namespace Ubiquity.NET.Versioning
{
    /// <summary>Utility Class for <see cref="DateTimeOffset"/> extensions</summary>
    public static class DateTimeOffsetExtensions
    {
        /// <summary>Gets a build index based on a time stamp</summary>
        /// <param name="timeStamp">Time stamp to use to create the build index</param>
        /// <returns>
        /// Build index as a string. The time stamp is converted to UTC (if not already in UTC form)
        /// so that the resulting index is consistent across builds on different machines/locales.
        /// </returns>
        /// <remarks>
        /// Since the resulting build index is based on the number of seconds since midnight and needs
        /// to fit in a limited string output. There is a narrow window of 2 seconds where two distinct
        /// builds (Local or PR) might generate the same matching build number. However, since this number
        /// is only used for local builds that's not realistically a problem. (Automated builds use the
        /// commit hash of the repo as the build index)
        /// </remarks>
        public static string ToBuildIndex( this DateTimeOffset timeStamp )
        {
            // establish an increasing build index based on the number of seconds from a common UTC date
            timeStamp = timeStamp.ToUniversalTime( );

            // Upper 16 bits of the build number is the number of days since the common base value
            uint buildNumber = ((uint)(timeStamp - CommonBaseDate).Days) << 16;

            // Lower 16 bits is the number of seconds (divided by 2) since midnight (on the date of the time stamp)
            var midnightTodayUtc = new DateTime( timeStamp.Year, timeStamp.Month, timeStamp.Day, 0, 0, 0, DateTimeKind.Utc );
            buildNumber += (ushort)((timeStamp - midnightTodayUtc).TotalSeconds / 2);
            return buildNumber.ToString( CultureInfo.InvariantCulture );
        }

        // Fixed point in time to use as reference for a build index.
        // Build index value is a string form of the number of days since this point in time + the number of seconds
        // since midnight of that time stamp.
        private static readonly DateTime CommonBaseDate = new( 2000, 1, 1, 0, 0, 0, DateTimeKind.Utc );
    }
}
