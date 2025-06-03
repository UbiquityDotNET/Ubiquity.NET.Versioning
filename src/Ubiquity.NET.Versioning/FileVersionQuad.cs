// -----------------------------------------------------------------------
// <copyright file="FileVersionQuad.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

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
    /// in a valid <see cref="FileVersionQuad"/>.
    /// </para>
    /// </remarks>
    public readonly record struct FileVersionQuad( UInt16 Major, UInt16 Minor, UInt16 Build, UInt16 Revision )
        : IComparable<FileVersionQuad>
    {
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

        /// <inheritdoc/>
        public int CompareTo( FileVersionQuad other )
        {
            return ToUInt64().CompareTo(other.ToUInt64());
        }

        /// <summary>Compares versions (Less than)</summary>
        /// <param name="left">Left hand side of the comparison</param>
        /// <param name="right">Right hand side of the comparison</param>
        /// <returns>Result of the comparison</returns>
        public static bool operator <( FileVersionQuad left, FileVersionQuad right ) => left.CompareTo( right ) < 0;

        /// <summary>Compares versions (Less than or Equal)</summary>
        /// <param name="left">Left hand side of the comparison</param>
        /// <param name="right">Right hand side of the comparison</param>
        /// <returns>Result of the comparison</returns>
        public static bool operator <=( FileVersionQuad left, FileVersionQuad right ) => left.CompareTo( right ) <= 0;

        /// <summary>Compares versions (Greater than)</summary>
        /// <param name="left">Left hand side of the comparison</param>
        /// <param name="right">Right hand side of the comparison</param>
        /// <returns>Result of the comparison</returns>
        public static bool operator >( FileVersionQuad left, FileVersionQuad right ) => left.CompareTo( right ) > 0;

        /// <summary>Compares versions (Greater than or equal)</summary>
        /// <param name="left">Left hand side of the comparison</param>
        /// <param name="right">Right hand side of the comparison</param>
        /// <returns>Result of the comparison</returns>
        public static bool operator >=( FileVersionQuad left, FileVersionQuad right ) => left.CompareTo( right ) >= 0;

        /// <summary>Converts this instance to a <see cref="Version"/></summary>
        /// <returns>Values of this instance as a <see cref="Version"/></returns>
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
