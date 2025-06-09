using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ubiquity.NET.Versioning
{
    internal class CSemVerComparison
    {
    }

    [SuppressMessage( "StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "DUH! It's file scoped" )]
    file class FileVersionQuadComparer
        : IComparer<FileVersionQuad>
    {
        /// <inheritdoc/>
        /// <remarks>
        /// Comparison of File version is a bit odd as it accounts for the CI indication. Which
        /// is NOT the same as just comparing the results of <see cref="FileVersionQuad.ToUInt64()"/>.
        /// It is a comparison of the ordered version AND if those are equal or the CI build status
        /// is not the same, then the CI status comes in to play. In particular a CI build version is
        /// ALWAYs a lower sort order than a non CI build of the same ordered version value.
        /// </remarks>
        public int Compare( FileVersionQuad lhs, FileVersionQuad rhs )
        {
            UInt64 orderedVersion = lhs.ToOrderedVersion(out bool rhsIsCIBuild);
            UInt64 otherOrderedVersion = rhs.ToOrderedVersion(out bool lhsIsCIBuild);
            int compareResult = orderedVersion.CompareTo(otherOrderedVersion);
            if( compareResult != 0 || rhsIsCIBuild == lhsIsCIBuild )
            {
                return compareResult;
            }

            // There are only two possibilities at this point.
            // The ordered version is the same BUT the CI build status
            // of each is NOT. A CI build should have a lower sort ordering
            // than a non CI build of the same version.
            return rhsIsCIBuild && !lhsIsCIBuild ? -1 : 1;
        }
    }
}
