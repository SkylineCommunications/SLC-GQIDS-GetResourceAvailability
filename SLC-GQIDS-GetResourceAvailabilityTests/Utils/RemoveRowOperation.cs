namespace SLC_GQIDS_GetResourceAvailabilityTests
{
    internal class RemoveRowOperation : IGqiUpdaterOperation
    {
        public string RemovedKey { get; set; }

        public override string ToString() => $"Remove: {RemovedKey}";

        public RemoveRowOperation(string removedKey)
        {
            RemovedKey = removedKey;
        }

        protected bool Equals(RemoveRowOperation other)
        {
            return RemovedKey == other.RemovedKey;
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

            return obj.GetType() == GetType() && Equals((RemoveRowOperation)obj);
        }

        public override int GetHashCode()
        {
            return RemovedKey != null ? RemovedKey.GetHashCode() : 0;
        }
    }
}