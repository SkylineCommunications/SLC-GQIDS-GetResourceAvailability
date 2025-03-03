namespace GetResourceAvailability
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Skyline.DataMiner.Analytics.GenericInterface;
    using Skyline.DataMiner.Net.ManagerStore;
    using Skyline.DataMiner.Net.Messages;

    /// <summary>
    /// Keeps track of the rows that have been returned, and the resources those rows were generated from.
    /// Responsible for deciding which rows need to be added/updated/removed when a resource is updated/removed, or retrieved by paging.
    /// </summary>
    internal class RowCollection
    {
        private readonly Dictionary<Guid, DateTime> _resourceToLastModifiedTimeStamp = new Dictionary<Guid, DateTime>();
        private readonly Dictionary<Guid, GQIRow[]> _resourceToRows = new Dictionary<Guid, GQIRow[]>();
        private readonly RowFactory _rowFactory;
        private readonly object _updateLock = new object();

        public RowCollection(RowFactory rowFactory)
        {
            _rowFactory = rowFactory;
        }

        public IGQIUpdater Updater { get; set; }

        public GQIRow[] RegisterRowsForResource(Resource resource, bool sendUpdate)
        {
            GQIRow[] newRows;
            GQIRow[] oldRows;

            lock (_updateLock)
            {
                var newTimeStamp = (resource as ITrackLastModified).LastModified;
                if (_resourceToLastModifiedTimeStamp.TryGetValue(resource.GUID, out var timestamp) && timestamp >= newTimeStamp)
                {
                    return Array.Empty<GQIRow>();
                }

                _resourceToLastModifiedTimeStamp[resource.GUID] = newTimeStamp;
                if (!_resourceToRows.TryGetValue(resource.GUID, out oldRows))
                {
                    oldRows = Array.Empty<GQIRow>();
                }

                newRows = _resourceToRows[resource.GUID] = _rowFactory.ResourceToRows(resource).ToArray();
            }

            if (!sendUpdate)
            {
                return newRows;
            }

            var oldRowIds = oldRows.Select(r => r.Key)
                                   .ToHashSet();

            foreach (var newRow in newRows)
            {
                if (!oldRowIds.Remove(newRow.Key))
                {
                    Updater?.AddRow(newRow);
                }
                else
                {
                    Updater?.UpdateRow(newRow);
                }
            }

            foreach (var oldRowId in oldRowIds)
            {
                Updater?.RemoveRow(oldRowId);
            }

            return newRows;
        }

        public void RemoveRowsForResource(Guid id)
        {
            lock (_updateLock)
            {
                _resourceToLastModifiedTimeStamp.Remove(id);

                if (!_resourceToRows.TryGetValue(id, out var rows))
                {
                    return;
                }

                _resourceToRows.Remove(id);
                foreach (var row in rows)
                {
                    Updater?.RemoveRow(row.Key);
                }

            }
        }
    }
}
