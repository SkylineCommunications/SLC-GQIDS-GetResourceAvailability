namespace SLC_GQIDS_GetResourceAvailabilityTests
{
    using Skyline.DataMiner.Analytics.GenericInterface;

    internal class AddRowOperation : IGqiUpdaterOperation
    {
        public GQIRow AddedRow { get; set; }

        public override string ToString() => $"Add: {Utils.GqiRowToString(AddedRow)}";

        public AddRowOperation(GQIRow addedRow)
        {
            AddedRow = addedRow;
        }

        protected bool Equals(AddRowOperation other)
        {
            return new RowComparer().Equals(AddedRow, other.AddedRow);
        }

        public bool Equals(IGqiUpdaterOperation other) => Equals(other as object);

        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj.GetType() == GetType() && Equals((AddRowOperation)obj);
        }

        public override int GetHashCode()
        {
            return AddedRow != null ? AddedRow.GetHashCode() : 0;
        }
    }
}