// -----------------------------------------------------------------------
// <copyright file="FileVersionQuad.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Numerics;

namespace Ubiquity.NET.Versioning
{
    /// <summary>Represents a traditional "File" version QUAD of 16bit values</summary>
    /// <param name="Major">Major version number</param>
    /// <param name="Minor">Minor version number</param>
    /// <param name="Build">Build version number</param>
    /// <param name="Revision">Revision number</param>
    /// <remarks>
    /// <para>The "FILEVERSION" structure was first used in Windows as part of the Resource compiler's
    /// "VERSION" information (Still used to this day). However, it's use in other places exists
    /// and has grown as it is simple, and naturally fits (maps to) an unsigned 64bit value. Thus,
    /// CSemVer defines a specific mapping of values to this common format.</para>
    /// <para>A standard .NET <see cref="Version"/> is very similar except that the bit width of each
    /// field is larger AND they are signed values. That is, every <see cref="FileVersionQuad"/> can
    /// produce a valid .NET <see cref="Version"/>. However, not every <see cref="Version"/> can result
    /// in a valid <see cref="FileVersionQuad"/>.</para>
    /// <para>A file version is a quad of 4 <see cref="UInt16"/> values. This is convertible to a <see cref="UInt64"/> in the
    /// following pattern:
    /// (bits are numbered with MSB as the highest numeric value [Actual byte ordering depends on platform endianess])
    /// <list type="table">
    ///     <listheader><term>Field</term><term>Description</term></listheader>
    ///     <item><term>bits 48-63</term><description> Major part of Build number</description></item>
    ///     <item><term>bits 32-47</term><description> Minor part of Build number</description></item>
    ///     <item><term>bits 16-31</term><description> Build part of Build number</description></item>
    ///     <item><term>bits 0-15</term><description> Revision part of Build number (LSB indicates a release/CI build see remarks section for details)</description></item>
    /// </list>
    /// </para>
    /// <note type="important">
    /// The role of the LSB for the Revision field is confusing as it indicates a CI build or not which itself is confusing. A CI
    /// build occurs AFTER a release! CI builds are ordered AFTER a release (or for a pre-release based on time only [0.0.0]). That is,
    /// a CI build ***always*** has a Patch+1 of a released build or [Major.Minor.Patch] == [0.0.0].
    /// </note>
    /// <para>A file version cast as a <see cref="UInt64"/> is <i><b>NOT</b></i> the same as an Ordered version number.
    /// The file version includes a "bit" for the status as a CI Build. Thus, a "file version" as a <see cref="UInt64"/> is the
    /// ordered version shifted left by one bit and the LSB indicates if it is a Release/CI build</para>
    /// </remarks>
    /// <seealso href="https://csemver.org/"/>
    public readonly record struct FileVersionQuad( UInt16 Major, UInt16 Minor, UInt16 Build, UInt16 Revision )
        : IComparable<FileVersionQuad>
        , IComparisonOperators<FileVersionQuad, FileVersionQuad, bool>
    {
        /// <summary>Gets a value indicating whether this version is a CI build</summary>
        public bool IsCiBuild => (Revision & 1) == 1; // 1 indicates a CI build with a higher sort order.

        /// <inheritdoc/>
        public int CompareTo( FileVersionQuad other )
        {
            return ToUInt64().CompareTo(other.ToUInt64());
        }

        /// <inheritdoc/>
        public static bool operator <( FileVersionQuad left, FileVersionQuad right ) => left.CompareTo( right ) < 0;

        /// <inheritdoc/>
        public static bool operator <=( FileVersionQuad left, FileVersionQuad right ) => left.CompareTo( right ) <= 0;

        /// <inheritdoc/>
        public static bool operator >( FileVersionQuad left, FileVersionQuad right ) => left.CompareTo( right ) > 0;

        /// <inheritdoc/>
        public static bool operator >=( FileVersionQuad left, FileVersionQuad right ) => left.CompareTo( right ) >= 0;

        /// <summary>Gets the UInt64 representation of the version</summary>
        /// <returns>UInt64 version of the version</returns>
        /// <remarks>
        /// The value is independent of system "endianness" and consists of the multiple parts
        /// corresponding to four 16 bit wide fields. In order of significance those values are:
        /// Major, Minor, Build, Revision. Thus, while the actual byte ordering of the data making
        /// up an integral value will depend on the system architecture, it's VALUE does not.
        /// </remarks>
        public UInt64 ToUInt64( )
        {
            return ((UInt64)Major << 48)
                 + ((UInt64)Minor << 32)
                 + ((UInt64)Build << 16)
                 + Revision;
        }

        /// <summary>Gets the CSemVer defined ordered version number for this FileVersion quad</summary>
        /// <param name="isCIBuild">Indicates if the File Version is for a CI build or not</param>
        /// <returns>Ordered version number of this file version</returns>
        /// <remarks>
        /// The File version form of a CSemVer value includes a bit to indicate if the version is a CI build
        /// or not. This condition is important for ordering
        /// </remarks>
        public UInt64 ToOrderedVersion(out bool isCIBuild)
        {
            isCIBuild = IsCiBuild;
            return ToUInt64() >> 1;
        }

        /// <inheritdoc cref="ToOrderedVersion(out bool)"/>
        public UInt64 ToOrderedVersion()
        {
            return ToOrderedVersion(out _);
        }

        /// <summary>Converts this instance to a <see cref="Version"/></summary>
        /// <returns>Values of this instance as a <see cref="Version"/></returns>
        /// <remarks>
        /// <note type="important">
        /// This conversion INCLUDES the CI build information bit and thus ordering
        /// of the result is NOT correct. (In File version Quads an ODD revision
        /// indicates a CI build, but any such build has an ordering that is LESS
        /// then any without it and therefore does NOT match the numeric ordering)
        /// </note>
        /// </remarks>
        public Version ToVersion( )
        {
            return new Version( Major, Minor, Build, Revision );
        }

        /// <summary>Converts this instance to a dotted string form</summary>
        /// <returns>Formatted string</returns>
        public override string ToString( ) => $"{Major}.{Minor}.{Build}.{Revision}";

        /// <summary>Converts a <see cref="Version"/> value to a <see cref="FileVersionQuad"/> if possible</summary>
        /// <param name="v">Version to convert</param>
        /// <returns>Resulting <see cref="FileVersionQuad"/></returns>
        /// <exception cref="ArgumentOutOfRangeException">At least one of the members of <paramref name="v"/> is out of range of a <see cref="UInt16"/></exception>
        public static FileVersionQuad From( Version v)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(v.Major, UInt16.MaxValue);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(v.Minor, UInt16.MaxValue);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(v.Build, UInt16.MaxValue);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(v.Revision, UInt16.MaxValue);

            return new((UInt16)v.Major, (UInt16)v.Minor, (UInt16)v.Build, (UInt16)v.Revision);
        }

        /// <summary>Converts a version integral value into a <see cref="FileVersionQuad"/></summary>
        /// <param name="value">Value to convert</param>
        /// <returns> <see cref="FileVersionQuad"/> variant of <paramref name="value"/></returns>
        public static FileVersionQuad From( UInt64 value )
        {
            UInt16 revision = (UInt16)(value % 65536);
            UInt64 rem = (value - revision) / 65536;

            UInt16 build = (UInt16)(rem % 65536);
            rem = (rem - build) / 65536;

            UInt16 minor = (UInt16)(rem % 65536);
            rem = (rem - minor) / 65536;

            UInt16 major = (UInt16)(rem % 65536);
            return new( major, minor, build, revision );
        }
    }
}
