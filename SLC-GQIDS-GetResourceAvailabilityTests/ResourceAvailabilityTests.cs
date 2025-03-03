namespace SLC_GQIDS_GetResourceAvailabilityTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using GetResourceAvailability;
    using Moq;
    using NUnit.Framework;
    using NUnit.Framework.Legacy;
    using Skyline.DataMiner.Analytics.GenericInterface;
    using Skyline.DataMiner.Net;
    using Skyline.DataMiner.Net.ManagerStore;
    using Skyline.DataMiner.Net.Messages;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Net.ResourceManager.Objects.ResourceAvailability;
    using Skyline.DataMiner.Net.SubscriptionFilters;
    using Resource = Skyline.DataMiner.Net.Messages.Resource;

    [TestFixture]
    public class ResourceAvailabilityTests
    {
        private readonly List<ResourcePool> _knownPools = new List<ResourcePool>();
        private readonly List<Resource> _knownResources = new List<Resource>();

        [SetUp]
        public void Reset()
        {
            TestContext.AddFormatter<GQIRow>(row => Utils.GqiRowToString(row as GQIRow));
            _knownPools.Clear();
            _knownResources.Clear();
        }

        [Test]
        public void Test_InvalidGuidResourcePoolArgument_ThrowsException()
        {
            const string invalidId = "NotAGuid";
            var exception = Assert.Throws<ArgumentException>(() =>
            {
                var adapterWrapper = InitializeNewAdapter(invalidId);
                _ = adapterWrapper.Updater;
            });
            StringAssert.Contains($"Could not parse '{invalidId}' to a valid ID for a resource pool", exception.Message);
        }

        [Test]
        public void Test_UnknownResourcePoolById_ThrowsException()
        {
            var unknownId = Guid.NewGuid();
            var exception = Assert.Throws<ArgumentException>(() => InitializeNewAdapter(unknownId));
            Assert.That(exception.Message.Contains($"with ID '{unknownId}'"));
        }

        [Test]
        public void Test_DataSource_ReturnsInMultiplePages()
        {
            var pool = AddKnownPool();

            const int amountOfResources = 500;

            for (var i = 0; i < amountOfResources; i++)
            {
                AddKnownResource(pool);
            }

            var adapterWrapper = InitializeNewAdapter(pool.GUID);

            var page = adapterWrapper.Adapter.GetNextPage(new GetNextPageInputArgs());
            
            Assert.That(page.Rows.Length, Is.LessThan(amountOfResources));
            Assert.That(page.HasNextPage, Is.True);

            var numberOfPages = 1;
            var returnedRows = page.Rows.ToList();
            while (page.HasNextPage)
            {
                // The adapter should page with a 'pagesize' of 200.
                page = adapterWrapper.Adapter.GetNextPage(new GetNextPageInputArgs());
                returnedRows.AddRange(page?.Rows.ToList());
                numberOfPages++;
            }

            Assert.That(returnedRows.Count, Is.EqualTo(amountOfResources));
            Assert.That(numberOfPages, Is.EqualTo(3));
        }

        [Test]
        public void Test_DataSource_WithAndWithoutFilterOnPool_GeneratedRows()
        {
            var now = DateTimeOffset.Now;
            var pool = AddKnownPool();

            var testResources = new TestResourceConfiguration(now);
            var list = testResources.GetAllResources();
            
            // Inject all resources so they are known by our fake connection.
            // Half the resources will have the pool.
            var resourcesInPool = new List<Resource>();

            for (var index = 0; index < list.Count; index++)
            {
                var oneResource = list[index];
                AddKnownResource(oneResource);
                if (index % 2 != 0)
                {
                    continue;
                }

                oneResource.PoolGUIDs.Add(pool.GUID);
                resourcesInPool.Add(oneResource);
            }

            // Test adapter with a filter on a pool
            var adapterWrapper = InitializeNewAdapter(pool.GUID.ToString(), now);
            var expectedRows = resourcesInPool.SelectMany(oneResource => testResources.GetExpectedRowsForResource(oneResource)).ToArray();
            
            var page = adapterWrapper.Adapter.GetNextPage(new GetNextPageInputArgs());
            AssertRowsAreEquivalent(page, expectedRows);

            // Test adapter without a filter on a pool
            adapterWrapper = InitializeNewAdapter(now);
            expectedRows = testResources.GetAllResources().SelectMany(oneResource => testResources.GetExpectedRowsForResource(oneResource)).ToArray();

            page = adapterWrapper.Adapter.GetNextPage(new GetNextPageInputArgs());
            AssertRowsAreEquivalent(page, expectedRows);

            CollectionAssert.IsEmpty(adapterWrapper.Updater.ReceivedOperations);
        }

        /// <summary>
        /// Tests the data source with a filter on a pool subscribes/unsubscribes correctly when the lifecycle events are invoked.
        /// </summary>
        [Test]
        public void Test_DataSource_WithFilterOnPool_SubscribesUnsubscribes()
        {
            var pool = AddKnownPool();

            var expectedSubscriptionFilter = new SubscriptionFilter<ResourceManagerEventMessage, Resource>(ResourceExposers.PoolGUIDs.Contains(pool.ID));

            var adapterWrapper = InitializeNewAdapter(pool.GUID);

            // The 'Equals' method on 'SubscriptionFilter<E,T>' does not verify the filter element, so we have to do this ourselves.
            adapterWrapper.InitInputArgs.MockedConnection.Verify(m => m.AddSubscription(It.IsAny<string>(), It.Is<SubscriptionFilter<ResourceManagerEventMessage, Resource>>(actualFilter => ValidateFilter(actualFilter, expectedSubscriptionFilter))), Times.Once);

            adapterWrapper.Adapter.OnStopUpdates();
            adapterWrapper.InitInputArgs.MockedConnection.Verify(m => m.RemoveSubscription(It.IsAny<string>(), It.Is<SubscriptionFilter<ResourceManagerEventMessage, Resource>>(actualFilter => ValidateFilter(actualFilter, expectedSubscriptionFilter))), Times.Once);
        }

        /// <summary>
        /// Tests the data source without a filter subscribes/unsubscribes correctly when the lifecycle events are invoked.
        /// </summary>
        [Test]
        public void Test_DataSource_WithoutFilterOnPool_SubscribesUnsubscribes()
        {
            var expectedSubscriptionFilter = new SubscriptionFilter<ResourceManagerEventMessage, Resource>(new TRUEFilterElement<Resource>());

            var adapterWrapper = InitializeNewAdapter();

            // The 'Equals' method on 'SubscriptionFilter<E,T>' does not verify the filter element, so we have to do this ourselves.
            adapterWrapper.InitInputArgs.MockedConnection.Verify(m => m.AddSubscription(It.IsAny<string>(), It.Is<SubscriptionFilter<ResourceManagerEventMessage, Resource>>(actualFilter => ValidateFilter(actualFilter, expectedSubscriptionFilter))), Times.Once);

            adapterWrapper.Adapter.OnStopUpdates();
            adapterWrapper.InitInputArgs.MockedConnection.Verify(m => m.RemoveSubscription(It.IsAny<string>(), It.Is<SubscriptionFilter<ResourceManagerEventMessage, Resource>>(actualFilter => ValidateFilter(actualFilter, expectedSubscriptionFilter))), Times.Once);
        }

        private static bool ValidateFilter(SubscriptionFilter<ResourceManagerEventMessage, Resource> filter, SubscriptionFilter<ResourceManagerEventMessage, Resource> expectedSubscriptionFilter)
        {
            return Equals(filter, expectedSubscriptionFilter) && Equals(filter.Filter, expectedSubscriptionFilter.Filter);
        }

        /// <summary>
        /// Tests the data source without a filter handles updates to resources correctly.
        /// </summary>
        [Test]
        public void Test_DataSource_WithoutFilter_Lifecycle()
        {
            var now = DateTimeOffset.Now;

            var adapterWrapper = InitializeNewAdapter(now);
            var expectedOperations = new Queue<IGqiUpdaterOperation>();

            // Add row
            var newResource = Utils.GetRandomResource();
            var eventMessage = CreateUpdatedResourceEventMessage(newResource);
            adapterWrapper.InitInputArgs.SendEventMessage(eventMessage);

            var expectedRow = Utils.CreateExpectedRow(newResource, null, null, null);
            expectedOperations.Enqueue(new AddRowOperation(expectedRow));

            CollectionAssert.AreEqual(expectedOperations, adapterWrapper.Updater.ReceivedOperations);

            // Update row
            newResource.Name += "_Updated";
            eventMessage = CreateUpdatedResourceEventMessage(newResource);
            adapterWrapper.InitInputArgs.SendEventMessage(eventMessage);

            expectedRow = Utils.CreateExpectedRow(newResource, null, null, null);
            expectedOperations.Enqueue(new UpdateRowOperation(expectedRow));

            CollectionAssert.AreEqual(expectedOperations, adapterWrapper.Updater.ReceivedOperations);

            // Update again by adding an availability window
            newResource.AvailabilityWindow = new BasicAvailabilityWindow()
            {
                AvailableFrom = now.AddHours(10)
            };
            eventMessage = CreateUpdatedResourceEventMessage(newResource);
            adapterWrapper.InitInputArgs.SendEventMessage(eventMessage);
            
            // New availability is a new row
            var newRow = Utils.CreateExpectedRow(newResource, DateTimeOffset.MinValue.UtcDateTime, now.AddHours(10).UtcDateTime, "Fixed");
            expectedOperations.Enqueue(new AddRowOperation(newRow));
            
            // Row for the old 'null' availability should be removed
            expectedOperations.Enqueue(new RemoveRowOperation(expectedRow.Key));

            CollectionAssert.AreEqual(expectedOperations, adapterWrapper.Updater.ReceivedOperations);

            // Update again
            newResource.Name += "_Again";
            eventMessage = CreateUpdatedResourceEventMessage(newResource);
            adapterWrapper.InitInputArgs.SendEventMessage(eventMessage);
            newRow = Utils.CreateExpectedRow(newResource, DateTimeOffset.MinValue.UtcDateTime, now.AddHours(10).UtcDateTime, "Fixed");

            expectedOperations.Enqueue(new UpdateRowOperation(newRow));
            
            CollectionAssert.AreEqual(expectedOperations, adapterWrapper.Updater.ReceivedOperations);

            // Remove row
            eventMessage = CreateRemovedResourceEventMessage(newResource);
            adapterWrapper.InitInputArgs.SendEventMessage(eventMessage);
            expectedOperations.Enqueue(new RemoveRowOperation(newRow.Key));

            CollectionAssert.AreEqual(expectedOperations, adapterWrapper.Updater.ReceivedOperations);
        }

        /// <summary>
        /// Resources that are in 'Unavailable' or 'Maintenance' mode should return a row indicating they are always unavailable.
        /// </summary>
        [Test]
        public void Test_UnavailableResourcesReturnRows()
        {
            // Add resources in the 'unavailable' and 'maintenance' modes
            var unavailableResource = AddKnownResource();
            unavailableResource.Mode = ResourceMode.Unavailable;

            var maintenanceResource = AddKnownResource();
            maintenanceResource.Mode = ResourceMode.Maintenance;

            var adapterWrapper = InitializeNewAdapter();

            var rows = adapterWrapper.Adapter.GetNextPage(new GetNextPageInputArgs());

            var expectedRows = new []
            {
                Utils.CreateExpectedRow(unavailableResource, DateTime.MinValue.ToUniversalTime(), DateTime.MaxValue.ToUniversalTime(), "Fixed"),
                Utils.CreateExpectedRow(maintenanceResource, DateTime.MinValue.ToUniversalTime(), DateTime.MaxValue.ToUniversalTime(), "Fixed"),
            };

            AssertRowsAreEquivalent(rows, expectedRows);
        }

        /// <summary>
        /// Send a 'ResourceManagerEventMessage' before the first page of the data source is retrieved.
        /// The resource received via paging should be ignored.
        /// </summary>
        [Test]
        public void Test_UpdateBeforeFirstPage()
        {
            var now = DateTimeOffset.Now;

            var resourceConfiguration = new TestResourceConfiguration(now);
            var allResources = resourceConfiguration.GetAllResources();

            allResources.ForEach(AddKnownResource);

            var adapterWrapper = InitializeNewAdapter(now);
            var adapter = adapterWrapper.Adapter;

            var resourceUpdate = Tools.Clone(allResources.First());
            resourceUpdate.Name += "_Updated";
            adapterWrapper.InitInputArgs.SendEventMessage(CreateUpdatedResourceEventMessage(resourceUpdate));

            var result = adapter.GetNextPage(new GetNextPageInputArgs());
            var rowsForUpdatedResource = result.Rows.Where(row => row.Key.Contains(resourceUpdate.GUID.ToString())).ToList();

            CollectionAssert.IsEmpty(rowsForUpdatedResource);
        }

        private AdapterWrapper InitializeNewAdapter() => InitializeNewAdapter(null, null);

        private AdapterWrapper InitializeNewAdapter(Guid poolFilter) => InitializeNewAdapter(poolFilter.ToString(), null);

        private AdapterWrapper InitializeNewAdapter(string poolFilter) => InitializeNewAdapter(poolFilter, null);

        private AdapterWrapper InitializeNewAdapter(DateTimeOffset now) => InitializeNewAdapter(null, now);

        private AdapterWrapper InitializeNewAdapter(string poolFilter, DateTimeOffset? now)
        {
            if (now == null)
            {
                now = DateTimeOffset.Now;
            }

            var context = new AvailabilityContext() { Now = now.Value };
            var adapter = new GetResourceAvailabilityDataSource(context);
            var arguments = new Dictionary<string, object>();
            
            if (poolFilter != null)
            {
                arguments["Resource Pool"] = poolFilter;
            }

            var initInputArgs = new MockOnInitInputArgs(_knownPools, _knownResources);
            var updater = new MockUpdater();

            adapter.OnInit(initInputArgs);
            adapter.OnArgumentsProcessed(new MockOnArgumentsProcessedInputArgs(arguments));
            adapter.GetColumns();
            adapter.OnStartUpdates(updater);
            return new AdapterWrapper(updater, initInputArgs, adapter);
        }

        private ResourcePool AddKnownPool()
        {
            var pool = Utils.GetRandomResourcePool();
            _knownPools.Add(pool);
            return pool;
        }

        private Resource AddKnownResource()
        {
            var resource = Utils.GetRandomResource();
            AddKnownResource(resource);
            return resource;
        }

        private void AddKnownResource(Resource resource)
        {
            _knownResources.Add(resource);
        }

        private void AddKnownResource(ResourcePool pool)
        {
            var resource = AddKnownResource();
            resource.PoolGUIDs.Add(pool.GUID);
        }

        private static void AssertRowsAreEquivalent(GQIPage page, GQIRow[] expectedRows)
        {
            Assert.That(page.Rows, Is.EquivalentTo(expectedRows).Using(new RowComparer()));
        }

        private static ResourceManagerEventMessage CreateUpdatedResourceEventMessage(Resource updatedResource)
        {
            var eventMessage = new ResourceManagerEventMessage();
            var timeStamp = (updatedResource as ITrackLastModified).LastModified;
            (updatedResource as ITrackLastModified).LastModified = timeStamp.AddTicks(1);
            eventMessage.UpdatedResources.Add(updatedResource);
            return eventMessage;
        }

        private static ResourceManagerEventMessage CreateRemovedResourceEventMessage(Resource removedResource)
        {
            var eventMessage = new ResourceManagerEventMessage();
            eventMessage.DeletedResourceObjects.Add(removedResource);
            return eventMessage;
        }
    }
}
