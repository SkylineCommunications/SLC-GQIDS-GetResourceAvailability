namespace SLC_GQIDS_GetResourceAvailabilityTests
{
    using System.Collections.Generic;
    using Skyline.DataMiner.Analytics.GenericInterface;

    internal class MockOnArgumentsProcessedInputArgs : OnArgumentsProcessedInputArgs
    {
        private readonly Dictionary<string, object> _arguments;

        public MockOnArgumentsProcessedInputArgs(Dictionary<string, object> arguments)
        {
            _arguments = arguments;
        }

        public override bool HasArgumentValue(string name) => _arguments.ContainsKey(name);

        public override bool TryGetArgumentValue<T>(string name, out T value)
        {
            if (!_arguments.TryGetValue(name, out var rawValue))
            {
                value = default;
                return false;
            }

            if (rawValue is T castedValue)
            {
                value = castedValue;
                return true;
            }

            value = default;
            return false;
        }
    }
}