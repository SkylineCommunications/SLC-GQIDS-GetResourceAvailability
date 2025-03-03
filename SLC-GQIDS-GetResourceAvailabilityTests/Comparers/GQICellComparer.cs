namespace SLC_GQIDS_GetResourceAvailabilityTests
{
    using System.Collections.Generic;
    using Skyline.DataMiner.Analytics.GenericInterface;

    internal class GqiCellComparer : IEqualityComparer<GQICell>
    {
        public bool Equals(GQICell x, GQICell y)
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

            return Equals(x.Value, y.Value) && x.DisplayValue == y.DisplayValue;
        }

        public int GetHashCode(GQICell obj)
        {
            unchecked
            {
                return ((obj.Value != null ? obj.Value.GetHashCode() : 0) * 397) ^ (obj.DisplayValue != null ? obj.DisplayValue.GetHashCode() : 0);
            }
        }
    }
}