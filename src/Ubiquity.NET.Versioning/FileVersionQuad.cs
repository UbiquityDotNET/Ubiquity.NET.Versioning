// -----------------------------------------------------------------------
// <copyright file="FileVersionQuad.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Ubiquity.NET.Versioning
{
    /// <summary>Represents a traditional "File" version QUAD of 16bit values</summary>
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
    public readonly record struct FileVersionQuad
        : IComparable<FileVersionQuad>
        , IComparisonOperators<FileVersionQuad, FileVersionQuad, bool>
    {
        /// <summary>Initializes a new instance of the <see cref="FileVersionQuad"/> struct.</summary>
        /// <param name="major">Major component of the version Quad</param>
        /// <param name="minor">Minor component of the version Quad</param>
        /// <param name="build">Build component of the version Quad</param>
        /// <param name="revision">Revision component of the version Quad</param>
        public FileVersionQuad( UInt16 major, UInt16 minor, UInt16 build, UInt16 revision )
        {
            FileVersion64 = ((UInt64)major << 48)
                          + ((UInt64)minor << 32)
                          + ((UInt64)build << 16)
                          + revision;
        }

        /// <summary>Initializes a new instance of the <see cref="FileVersionQuad"/> struct.</summary>
        /// <param name="fileVersion64">Unsigned 64 bit representation of a file version Quad</param>
        public FileVersionQuad( UInt64 fileVersion64 )
        {
            FileVersion64 = fileVersion64;
        }

        /// <summary>Initializes a new instance of the <see cref="FileVersionQuad"/> struct.</summary>
        /// <param name="orderedVersion">Ordered version number to build the Quad from</param>
        /// <param name="isCiBuild">Boolean indicator of whether the version is a CI build or not</param>
        /// <remarks>
        /// CI build information is NOT included in an ordered version thus it is a required parameter
        /// when converting. The <paramref name="isCiBuild"/> parameter also helps differentiate the
        /// constructor overloads with the <see cref="FileVersionQuad.FileVersionQuad(ulong)"/> form as
        /// both use a 64 bit value (signed vs. unsigned) to represent very different things.
        /// </remarks>
        public FileVersionQuad( Int64 orderedVersion, bool isCiBuild )
        {
            // if the upper bit is set, it isn't a valid ordered version...
            if(orderedVersion < 0)
            {
                throw new ArgumentOutOfRangeException( nameof( orderedVersion ), "Invalid ordered version; Upper bit is set!" );
            }

            FileVersion64 = ((UInt64)orderedVersion << 1) + (isCiBuild ? 1ul : 0ul);
        }

        /// <summary>Initializes a new instance of the <see cref="FileVersionQuad"/> struct.</summary>
        /// <param name="v">Version to initialize from</param>
        /// <exception cref="ArgumentOutOfRangeException">One of the components of <paramref name="v"/> is too large to fit in a unsigned 16 bit value</exception>
        public FileVersionQuad( Version v )
            : this( (UInt16)v.Major.ThrowIfGreaterThan( UInt16.MaxValue ),
                    (UInt16)v.Minor.ThrowIfGreaterThan( UInt16.MaxValue ),
                    (UInt16)v.Build.ThrowIfGreaterThan( UInt16.MaxValue ),
                    (UInt16)v.Revision.ThrowIfGreaterThan( UInt16.MaxValue )
                  )
        {
        }

        /// <summary>Gets a value indicating whether this version is a CI build</summary>
        public bool IsCiBuild => (Revision & 1) == 1; // 1 indicates a CI build with a higher sort order.

        /// <summary>Gets the Major component of the version number</summary>
        public UInt16 Major => (UInt16)(FileVersion64 >> 48);

        /// <summary>Gets the Major component of the version number</summary>
        public UInt16 Minor => (UInt16)(FileVersion64 >> 32);

        /// <summary>Gets the Major component of the version number</summary>
        public UInt16 Build => (UInt16)(FileVersion64 >> 16);

        /// <summary>Gets the Major component of the version number</summary>
        public UInt16 Revision => (UInt16)(FileVersion64);

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
            return FileVersion64;
        }

        /// <inheritdoc/>
        public int CompareTo( FileVersionQuad other )
        {
            return FileVersion64.CompareTo( other.FileVersion64 );
        }

        /// <inheritdoc/>
        public static bool operator <( FileVersionQuad left, FileVersionQuad right ) => left.CompareTo( right ) < 0;

        /// <inheritdoc/>
        public static bool operator <=( FileVersionQuad left, FileVersionQuad right ) => left.CompareTo( right ) <= 0;

        /// <inheritdoc/>
        public static bool operator >( FileVersionQuad left, FileVersionQuad right ) => left.CompareTo( right ) > 0;

        /// <inheritdoc/>
        public static bool operator >=( FileVersionQuad left, FileVersionQuad right ) => left.CompareTo( right ) >= 0;

        /// <summary>Gets the CSemVer defined ordered version number for this FileVersion quad</summary>
        /// <param name="isCIBuild">Indicates if the File Version is for a CI build or not</param>
        /// <returns>Ordered version number of this file version</returns>
        /// <remarks>
        /// The File version form of a CSemVer value includes a bit to indicate if the version is a CI build
        /// or not. This condition is important for ordering
        /// </remarks>
        public Int64 ToOrderedVersion( out bool isCIBuild )
        {
            isCIBuild = IsCiBuild;
            return (Int64)(FileVersion64 >> 1);
        }

        /// <inheritdoc cref="ToOrderedVersion(out bool)"/>
        public Int64 ToOrderedVersion( )
        {
            return ToOrderedVersion( out _ );
        }

        /// <summary>Converts this instance to a <see cref="Version"/></summary>
        /// <returns>Values of this instance as a <see cref="Version"/></returns>
        /// <remarks>
        /// <note type="important">
        /// <para>This conversion INCLUDES the CI build information bit and thus ordering
        /// of the result remains correct. (In File version Quads an ODD revision
        /// indicates a CI build which is ordered AFTER the build it depends on!)</para>
        /// <para>Keep in mind that a CI build is a POST release build numbering and does
        /// not reflect what release the build may ever become.</para>
        /// </note>
        /// </remarks>
        public Version ToVersion( )
        {
            return new Version( Major, Minor, Build, Revision );
        }

        /// <summary>Converts this instance to a <see cref="SemVer"/> derived constrained type</summary>
        /// <returns>Constrained version type</returns>
        /// <remarks>The result is either a <see cref="CSemVerCI"/> or <see cref="CSemVer"/> depending
        /// on the state of the <see cref="IsCiBuild"/> property. This is used in consuming code when
        /// it gets only the file version as a 64bit value or a <see cref="FileVersionQuad"/> to produce
        /// a correct version. Usually a consumer won't care and is only concerned with ordering but
        /// if needed simple "is" testing is available.
        /// </remarks>
        /// <example>
        /// <code><![CDATA[
        /// FileVersionQuad quad;
        /// // ...
        /// SemVer ver = quad.ToSemVer();
        /// if(ver > MinimumVer)
        /// {
        ///     // Good to go!
        ///     if( ver is CSmeVerCI)
        ///     {
        ///         // and it's a CI build!
        ///     }
        /// }
        ///
        /// // ...
        /// static readonly CSemVer MinimumVer = new(1,2,3);
        /// ]]></code>
        /// </example>
        public SemVer ToSemVer()
        {
            return IsCiBuild ? CSemVerCI.From(this) : CSemVer.From(this);
        }

        /// <summary>Converts this instance to a dotted string form</summary>
        /// <returns>Formatted string</returns>
        public override string ToString( ) => $"{Major}.{Minor}.{Build}.{Revision}";

        private readonly UInt64 FileVersion64;

        /// <summary>explicit cast of a <see cref="FileVersionQuad"/> as a version</summary>
        /// <param name="v">Value to cast</param>
        public static explicit operator Version( FileVersionQuad v )
        {
            return v.ToVersion();
        }

        /// <summary>Explicit cast operator from a <see cref="Version"/> to a <see cref="FileVersionQuad"/></summary>
        /// <param name="v">Version to convert</param>
        [SuppressMessage( "Usage", "CA2225:Operator overloads have named alternates", Justification = "Constructor exists" )]
        public static explicit operator FileVersionQuad( Version v )
        {
            return new( v );
        }

        /// <summary>Explicit cast operator from a <see cref="UInt64"/> raw value to a <see cref="FileVersionQuad"/></summary>
        /// <param name="v">Version to convert</param>
        [SuppressMessage( "Usage", "CA2225:Operator overloads have named alternates", Justification = "Constructor exists" )]
        public static explicit operator FileVersionQuad( UInt64 v )
        {
            return new( v );
        }
    }
}
