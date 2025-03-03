namespace GetResourceAvailability
{
    using System;
    using System.Collections.Generic;

    using Skyline.DataMiner.Analytics.GenericInterface;
    using Skyline.DataMiner.Net.Messages;
    using Skyline.DataMiner.Net.ResourceManager.Objects.ResourceAvailability;
    using Skyline.DataMiner.Net;

    /// <summary>
    /// Responsible for converting a resource to the correct GQI rows.
    /// </summary>
    public class RowFactory
    {
        private readonly AvailabilityContext _context;

        public RowFactory(AvailabilityContext context)
        {
            _context = context;
        }

        public IEnumerable<GQIRow> ResourceToRows(Resource resource)
        {
            if (resource.Mode != ResourceMode.Available)
            {
                yield return GetRow(resource, DateTime.MinValue.ToUniversalTime(), DateTime.MaxValue.ToUniversalTime(), BoundaryDefinition.Fixed.ToString());
                yield break;
            }

            var timeRanges = GetUnavailableTimeRanges(resource);

            if (timeRanges.Count == 0)
            {
                yield return GetRow(resource, null, null, null);
                yield break;
            }

            foreach (var unavailableTimeRange in timeRanges)
            {
                yield return GetRow(resource, unavailableTimeRange.Start.UtcDateTime, unavailableTimeRange.Stop.UtcDateTime, unavailableTimeRange.StartDetails.BoundaryDefinition.ToString());
            }
        }

        private static GenIfRowMetadata GetMetaData(Resource resource)
        {
            return new GenIfRowMetadata(new RowMetadataBase[]
            {
                new ObjectRefMetadata()
                {
                    Object = new ResourceID(resource.ID)
                }
            });
        }

        private static GQIRow GetRow(Resource resource, DateTime? start, DateTime? stop, string type)
        {
            var rowKey = $"{resource.ID}_{start?.Ticks ?? 0}_{stop?.Ticks ?? 0}";

            return new GQIRow(rowKey, new[]
            {
                new GQICell() { Value = resource.GUID.ToString() },
                new GQICell() { Value = start },
                new GQICell() { Value = stop },
                new GQICell() { Value = resource.Name },
                new GQICell() { Value = type },
            })
            {
                Metadata = GetMetaData(resource)
            };
        }
        private List<ResourceWindowTimeRange> GetUnavailableTimeRanges(Resource resource)
        {
            if (resource.AvailabilityWindow == null)
            {
                return new List<ResourceWindowTimeRange>();
            }

            var availability = resource.AvailabilityWindow.GetAvailability(_context);

            return availability.UnavailableTimeRanges;
        }
    }
}