namespace SLC_GQIDS_GetResourceAvailabilityTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Moq;
    using Skyline.DataMiner.Analytics.GenericInterface;
    using Skyline.DataMiner.Net;
    using Skyline.DataMiner.Net.Messages;
    using SLDataGateway.API.Types.Paging;

    internal class MockOnInitInputArgs : OnInitInputArgs
    {
        protected override IGQIDMSInterface CreateDMSInterface() => new MockGQIDMSInterface(MockedConnection.Object);

        public override IGQIFactory Factory => Mock.Of<IGQIFactory>();

        public override IGQILogger Logger => Mock.Of<IGQILogger>();

        public Mock<IConnection> MockedConnection { get; } = new Mock<IConnection>();

        private readonly Dictionary<Guid, Stack<Resource>> _resourcePagingInProgress = new Dictionary<Guid, Stack<Resource>>();

        public MockOnInitInputArgs(List<ResourcePool> knownPools, List<Resource> knownResources)
        {
            MockedConnection.Setup(c => c.HandleSingleResponseMessage(It.IsAny<GetResourcePoolMessage>()))
                             .Returns((GetResourcePoolMessage message) =>
                             {
                                 if (message.ResourceManagerObjects.Count != 1)
                                 {
                                     throw new ArgumentException("Got a message for no or multiple pools");
                                 }

                                 var guid = message.ResourceManagerObjects.First().GUID;
                                 return new ResourcePoolResponseMessage(knownPools.Where(p => p.ID == guid).ToArray());
                             });

            MockedConnection.Setup(c => c.HandleSingleResponseMessage(It.IsAny<ManagerStoreStartPagingRequest<Resource>>()))
                             .Returns((ManagerStoreStartPagingRequest<Resource> message) =>
                             {
                                 var pagingCookie = Guid.NewGuid();
                                 var resourcesToReturn = new Stack<Resource>(message.Filter.ExecuteInMemory(knownResources));
                                 _resourcePagingInProgress[pagingCookie] = resourcesToReturn;

                                 return GetPagingResponse(resourcesToReturn, (int) message.PreferredPageSize, pagingCookie);
                             });

            MockedConnection.Setup(c => c.HandleSingleResponseMessage(It.IsAny<ManagerStoreNextPagingRequest<Resource>>()))
                             .Returns((ManagerStoreNextPagingRequest<Resource> message) =>
                             {
                                 if (!_resourcePagingInProgress.TryGetValue(message.PagingCookie.GUID, out var resourcesToReturn))
                                 {
                                     throw new ArgumentException($"Cannot find a paging in progress with id {message.PagingCookie.GUID}");
                                 }

                                 return GetPagingResponse(resourcesToReturn, (int) message.PreferredPageSize, message.PagingCookie.GUID);
                             });
        }

        private static DMSMessage GetPagingResponse(Stack<Resource> resourcesToReturn, int preferredPageSize, Guid pagingCookie)
        {
            var resourcesOnThisPage = PopMultipleFromStack(resourcesToReturn, preferredPageSize);
            var hasNextPage = resourcesToReturn.Count > 0;

            return new ManagerStorePagingResponse<Resource>()
            {
                IsFinalPage = !hasNextPage,
                PagingCookie = new DisposablePagingCookie(pagingCookie),
                Objects = resourcesOnThisPage
            };
        }

        public void SendEventMessage(EventMessage eventMessage)
        {
            MockedConnection.Raise(c => c.OnNewMessage += null, new NewMessageEventArgs(eventMessage));
        }

        private static List<T> PopMultipleFromStack<T>(Stack<T> stack, int amount)
        {
            var result = new List<T>(amount);
            while (amount-- > 0 && stack.Count > 0)
            {
                result.Add(stack.Pop());
            }
            return result;
        }
    }
}