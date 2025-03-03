namespace SLC_GQIDS_GetResourceAvailabilityTests
{
    using System;
    using System.Collections.Generic;
    using GetResourceAvailability;
    using Skyline.DataMiner.Analytics.GenericInterface;
    using Skyline.DataMiner.Net.Messages;
    using Skyline.DataMiner.Net.ResourceManager.Objects.ResourceAvailability;

    /// <summary>
    /// Contains different combinations of availability windows with their expected rows
    /// </summary>
    internal class TestResourceConfiguration
    {
        private readonly Resource _resourceNoWindow;
        private readonly Resource _resourceOnlyAvailableFrom;
        private readonly Resource _resourceOnlyAvailableUntil;
        private readonly Resource _resourceOnlyRollingWindow;
        private readonly Resource _resourceAvailableFromAndRollingWindow;
        private readonly Resource _resourceAvailableUntilAndRollingWindow;
        private readonly Resource _resourceAvailableFromAndUntil;
        private readonly Resource _resourceAvailableFromUntilAndRollingWindow;
        private readonly Resource _resourceAvailableFromUntilAndRollingWindowBeforeUntil;

        private readonly Dictionary<Guid, List<GQIRow>> _expectedRowsPerResource = new Dictionary<Guid, List<GQIRow>>();

        public TestResourceConfiguration(DateTimeOffset now)
        {
            // 'resourceNoWindow' rows
            _resourceNoWindow = AddResource();
            _resourceNoWindow.AvailabilityWindow = null;
            AddExpectedRows(_resourceNoWindow, Utils.CreateExpectedRow(_resourceNoWindow, null, null, null));

            // 'resourceOnlyAvailableFrom' rows
            _resourceOnlyAvailableFrom = AddResource(from: now.AddHours(1));
            AddExpectedRows(_resourceOnlyAvailableFrom, Utils.CreateExpectedRow(_resourceOnlyAvailableFrom, DateTime.MinValue, now.AddHours(1).UtcDateTime, "Fixed"));

            // 'resourceOnlyAvailableUntil' rows
            _resourceOnlyAvailableUntil = AddResource(until: now.AddHours(10));
            AddExpectedRows(_resourceOnlyAvailableUntil, Utils.CreateExpectedRow(_resourceOnlyAvailableUntil, now.AddHours(10).UtcDateTime, DateTime.MaxValue, "Fixed"));

            // 'resourceOnlyRollingWindow' rows
            _resourceOnlyRollingWindow = AddResource(window: TimeSpan.FromHours(7));
            AddExpectedRows(_resourceOnlyRollingWindow, Utils.CreateExpectedRow(_resourceOnlyRollingWindow, now.AddHours(7).UtcDateTime, DateTime.MaxValue, "RollingWindow"));

            // 'resourceAvailableFromAndRollingWindow' rows
            _resourceAvailableFromAndRollingWindow = AddResource(from: now.AddHours(5), window: TimeSpan.FromDays(30));
            var expectedRows = new[]
            {
                Utils.CreateExpectedRow(_resourceAvailableFromAndRollingWindow, DateTime.MinValue, now.AddHours(5).UtcDateTime,"Fixed"),
                Utils.CreateExpectedRow(_resourceAvailableFromAndRollingWindow, now.AddDays(30).UtcDateTime, DateTime.MaxValue,"RollingWindow")
            };
            AddExpectedRows(_resourceAvailableFromAndRollingWindow, expectedRows);

            // 'resourceAvailableUntilAndRollingWindow' rows
            _resourceAvailableUntilAndRollingWindow = AddResource(until: now.AddDays(100), window: TimeSpan.FromDays(30));
            expectedRows = new[]
            {
                Utils.CreateExpectedRow(_resourceAvailableUntilAndRollingWindow, now.AddDays(30).UtcDateTime, now.AddDays(100).UtcDateTime, "RollingWindow"),
                Utils.CreateExpectedRow(_resourceAvailableUntilAndRollingWindow, now.AddDays(100).UtcDateTime, DateTime.MaxValue, "Fixed")
            };
            AddExpectedRows(_resourceAvailableUntilAndRollingWindow, expectedRows);

            // 'resourceAvailableFromAndUntil' rows
            _resourceAvailableFromAndUntil = AddResource(from: now.AddDays(-1), until: now.AddDays(100));
            expectedRows = new[]
            {
                Utils.CreateExpectedRow(_resourceAvailableFromAndUntil, DateTime.MinValue, now.AddDays(-1).UtcDateTime, "Fixed"),
                Utils.CreateExpectedRow(_resourceAvailableFromAndUntil, now.AddDays(100).UtcDateTime, DateTime.MaxValue, "Fixed")
            };
            AddExpectedRows(_resourceAvailableFromAndUntil, expectedRows);

            // 'resourceAvailableFromUntilAndRollingWindow' rows
            _resourceAvailableFromUntilAndRollingWindow = AddResource(from: now.AddDays(7), until: now.AddDays(16), window: TimeSpan.FromDays(10));
            expectedRows = new[]
            {
                Utils.CreateExpectedRow(_resourceAvailableFromUntilAndRollingWindow, DateTime.MinValue, now.AddDays(7).UtcDateTime, "Fixed"),
                Utils.CreateExpectedRow(_resourceAvailableFromUntilAndRollingWindow, now.AddDays(10).UtcDateTime, now.AddDays(16).UtcDateTime, "RollingWindow"),
                Utils.CreateExpectedRow(_resourceAvailableFromUntilAndRollingWindow, now.AddDays(16).UtcDateTime, DateTime.MaxValue, "Fixed")
            };
            AddExpectedRows(_resourceAvailableFromUntilAndRollingWindow, expectedRows);

            // 'resourceAvailableFromUntilAndRollingWindowBeforeUntil'
            _resourceAvailableFromUntilAndRollingWindowBeforeUntil = AddResource(from: now.AddDays(7), until: now.AddDays(16), window: TimeSpan.FromDays(5));
            expectedRows = new[]
            {
                Utils.CreateExpectedRow(_resourceAvailableFromUntilAndRollingWindowBeforeUntil, DateTime.MinValue, now.AddDays(7).UtcDateTime, "Fixed"),
                Utils.CreateExpectedRow(_resourceAvailableFromUntilAndRollingWindowBeforeUntil, now.AddDays(7).UtcDateTime, now.AddDays(16).UtcDateTime, "RollingWindow"),
                Utils.CreateExpectedRow(_resourceAvailableFromUntilAndRollingWindowBeforeUntil, now.AddDays(16).UtcDateTime, DateTime.MaxValue, "Fixed")
            };
            AddExpectedRows(_resourceAvailableFromUntilAndRollingWindowBeforeUntil, expectedRows);
        }

        public GQIRow[] GetExpectedRowsForResource(Resource resource) => _expectedRowsPerResource[resource.GUID].ToArray();

        public List<Resource> GetAllResources()
        {
            return new List<Resource>()
            {
                _resourceNoWindow, 
                _resourceOnlyAvailableFrom, 
                _resourceOnlyAvailableUntil, 
                _resourceOnlyRollingWindow, 
                _resourceAvailableFromAndRollingWindow, 
                _resourceAvailableUntilAndRollingWindow, 
                _resourceAvailableFromAndUntil, 
                _resourceAvailableFromUntilAndRollingWindow, 
                _resourceAvailableFromUntilAndRollingWindowBeforeUntil
            };
        }

        private static BasicAvailabilityWindow GetAvailabilityWindow(DateTimeOffset? from = null, DateTimeOffset? until = null, TimeSpan? window = null)
        {
            var availability = new BasicAvailabilityWindow
            {
                AvailableFrom = from ?? DateTimeOffset.MinValue,
                AvailableUntil = until ?? DateTimeOffset.MaxValue
            };
            if (window != null)
            {
                availability.RollingWindowConfiguration = new RollingWindowConfiguration(window.Value);
            }

            return availability;
        }

        private Resource AddResource(DateTimeOffset? from = null, DateTimeOffset? until = null, TimeSpan? window = null)
        {
            var resource = Utils.GetRandomResource();
            resource.AvailabilityWindow = GetAvailabilityWindow(from, until, window);
            _expectedRowsPerResource[resource.GUID] = new List<GQIRow>();

            return resource;
        }

        private void AddExpectedRows(Resource resource, params GQIRow[] expectedRows)
        {
            foreach (var oneRow in expectedRows)
            {
                _expectedRowsPerResource[resource.GUID].Add(oneRow);
            }
        }
    }
}
