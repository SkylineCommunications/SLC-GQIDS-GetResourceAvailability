namespace SLC_GQIDS_GetResourceAvailabilityTests
{
    using System;
    using System.Collections.Generic;
    using Skyline.DataMiner.Analytics.GenericInterface;

    internal class MockUpdater : IGQIUpdater
    {
        public Queue<IGqiUpdaterOperation> ReceivedOperations { get; } = new Queue<IGqiUpdaterOperation>();

        public void AddRow(GQIRow row) => ReceivedOperations.Enqueue(new AddRowOperation(row));
        
        public void UpdateRow(GQIRow row) => ReceivedOperations.Enqueue(new UpdateRowOperation(row));
        
        public void RemoveRow(string rowKey) => ReceivedOperations.Enqueue(new RemoveRowOperation(rowKey));

        public void UpdateCell(string rowKey, GQIColumn column, GQICell cell)
        {
            // Not implemented because not needed for this ad-hoc data source
            throw new NotImplementedException();
        }

        public void UpdateCell<T>(string rowKey, GQIColumn<T> column, T value)
        {
            // Not implemented because not needed for this ad-hoc data source
            throw new NotImplementedException();
        }
    }
}