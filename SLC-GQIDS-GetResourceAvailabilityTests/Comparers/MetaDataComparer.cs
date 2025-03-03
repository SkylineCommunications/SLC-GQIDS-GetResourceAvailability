namespace SLC_GQIDS_GetResourceAvailabilityTests
{
    using System.Collections.Generic;
    using System.Linq;
    using Skyline.DataMiner.Analytics.GenericInterface;

    internal class MetaDataComparer : IEqualityComparer<GenIfRowMetadata>
    {
        public bool Equals(GenIfRowMetadata x, GenIfRowMetadata y)
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

            return Enumerable.SequenceEqual(x.Metadata, y.Metadata, new RowMetadataComparer());
        }

        public int GetHashCode(GenIfRowMetadata obj)
        {
            return obj.Metadata != null ? obj.Metadata.GetHashCode() : 0;
        }
    }
}