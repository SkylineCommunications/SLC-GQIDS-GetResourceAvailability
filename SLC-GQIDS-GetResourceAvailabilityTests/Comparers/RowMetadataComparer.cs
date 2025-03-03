namespace SLC_GQIDS_GetResourceAvailabilityTests
{
    using System.Collections.Generic;
    using Skyline.DataMiner.Analytics.GenericInterface;

    internal class RowMetadataComparer : IEqualityComparer<RowMetadataBase>
    {
        public bool Equals(RowMetadataBase x, RowMetadataBase y)
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

            var columnsEqual = x.ColumnGroupIdx == y.ColumnGroupIdx;
            if (!columnsEqual)
            {
                return false;
            }

            if (x is ObjectRefMetadata xMeta && y is ObjectRefMetadata yMeta)
            {
                return Equals(xMeta.Object, yMeta.Object);
            }

            // Don't compare other subclasses here yet as there is no need for now.

            return true;
        }

        public int GetHashCode(RowMetadataBase obj)
        {
            return obj.ColumnGroupIdx;
        }
    }
}