using System;
using System.Linq;
using Skyline.DataMiner.Analytics.GenericInterface;
using Skyline.DataMiner.Net;
using Skyline.DataMiner.Net.Messages;

namespace SLC_GQIDS_GetResourceAvailabilityTests
{
    internal static class Utils
    {
        private static readonly Random Random = new Random(Guid.NewGuid().GetHashCode());

        public static string GqiRowToString(GQIRow row)
        {
            if (row == null)
            {
                return null;
            }

            var cellsJoined = string.Join(", ", row.Cells.Select(c => c.Value));
            return $"[{row.Key} => {cellsJoined}]";
        }
        
        public static GQIRow CreateExpectedRow(Resource resource, DateTime? start, DateTime? end, string type)
        {
            var id = resource.ID;
            var expectedKey = $"{id}_{start?.Ticks ?? 0}_{end?.Ticks ?? 0}";

            var cells = new []
            {
                new GQICell() { Value = id.ToString() },
                new GQICell() { Value = start },
                new GQICell() { Value = end },
                new GQICell() { Value = resource.Name },
                new GQICell() { Value = type }
            };

            var row = new GQIRow(expectedKey, cells)
            {
                Metadata = new GenIfRowMetadata(new RowMetadataBase[]
                {
                    new ObjectRefMetadata()
                    {
                        Object = new ResourceID(resource.ID)
                    }
                })
            };

            return row;
        }

        public static ResourcePool GetRandomResourcePool()
        {
            return new ResourcePool(Guid.NewGuid())
            {
                Name = GetRandomString(10)
            };
        }

        public static Resource GetRandomResource()
        {
            return new Resource(Guid.NewGuid())
            {
                Name = GetRandomString(10)
            };
        }

        public static string GetRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var stringChars = new char[length];

            for (var i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = chars[Random.Next(chars.Length)];
            }

            return new string(stringChars);
        }
    }
}
