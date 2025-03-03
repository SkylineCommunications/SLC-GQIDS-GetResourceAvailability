namespace SLC_GQIDS_GetResourceAvailabilityTests
{
    internal class AdapterWrapper
    {
        public MockUpdater Updater { get; private set; }
        public MockOnInitInputArgs InitInputArgs { get; private set; }
        public GetResourceAvailability.GetResourceAvailabilityDataSource Adapter { get; private set; }

        public AdapterWrapper(MockUpdater updater, MockOnInitInputArgs returnValue, GetResourceAvailability.GetResourceAvailabilityDataSource adapter)
        {
            Updater = updater;
            InitInputArgs = returnValue;
            Adapter = adapter;
        }
    }
}