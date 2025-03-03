namespace SLC_GQIDS_GetResourceAvailabilityTests
{
    using Skyline.DataMiner.Analytics.GenericInterface;

    internal class UpdateRowOperation : IGqiUpdaterOperation
    {
        public GQIRow UpdatedRow { get; set; }

        public UpdateRowOperation(GQIRow addedRow)
        {
            UpdatedRow = addedRow;
        }

        public override string ToString() => $"Update: {Utils.GqiRowToString(UpdatedRow)}";

        protected bool Equals(UpdateRowOperation other)
        {
            return new RowComparer().Equals(UpdatedRow, other.UpdatedRow);
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

            return obj.GetType() == GetType() && Equals((UpdateRowOperation)obj);
        }

        public override int GetHashCode()
        {
            return UpdatedRow != null ? UpdatedRow.GetHashCode() : 0;
        }
    }
}