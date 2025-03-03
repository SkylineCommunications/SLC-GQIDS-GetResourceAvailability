namespace SLC_GQIDS_GetResourceAvailabilityTests
{
    using Skyline.DataMiner.Analytics.GenericInterface;
    using Skyline.DataMiner.Net;
    using Skyline.DataMiner.Net.Messages;

    internal class MockGQIDMSInterface : IGQIDMSInterface
    {
        private readonly IConnection _connection;

        public DMSMessage[] SendMessages(params DMSMessage[] messages) => _connection.HandleMessages(messages);

        public IConnection GetConnection() => _connection;

        public MockGQIDMSInterface(IConnection mockConnection)
        {
            _connection = mockConnection;
        }
    }
}