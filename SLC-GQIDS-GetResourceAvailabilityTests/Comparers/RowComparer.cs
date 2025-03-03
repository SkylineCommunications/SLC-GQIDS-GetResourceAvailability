using System.Collections.Generic;
using System.Linq;
using Skyline.DataMiner.Analytics.GenericInterface;

namespace SLC_GQIDS_GetResourceAvailabilityTests
{
    internal class RowComparer : IEqualityComparer<GQIRow>
    {
        public bool Equals(GQIRow x, GQIRow y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null)
            {
                return false;
            }

            if (y is null)
            {
                return false;
            }

            if (x.GetType() != y.GetType())
            {
                return false;
            }

            return x.Key == y.Key && new MetaDataComparer().Equals(x.Metadata, y.Metadata) && x.Cells.SequenceEqual(y.Cells, new GqiCellComparer());
        }

        public int GetHashCode(GQIRow obj)
        {
            unchecked
            {
                var hashCode = obj.Key != null ? obj.Key.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ (obj.Cells != null ? obj.Cells.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (obj.Metadata != null ? obj.Metadata.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}