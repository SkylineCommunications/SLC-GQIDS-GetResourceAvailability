/*
****************************************************************************
*  Copyright (c) 2025,  Skyline Communications NV  All Rights Reserved.    *
****************************************************************************

By using this script, you expressly agree with the usage terms and
conditions set out below.
This script and all related materials are protected by copyrights and
other intellectual property rights that exclusively belong
to Skyline Communications.

A user license granted for this script is strictly for personal use only.
This script may not be used in any way by anyone without the prior
written consent of Skyline Communications. Any sublicensing of this
script is forbidden.

Any modifications to this script by the user are only allowed for
personal use and within the intended purpose of the script,
and will remain the sole responsibility of the user.
Skyline Communications will not be responsible for any damages or
malfunctions whatsoever of the script resulting from a modification
or adaptation by the user.

The content of this script is confidential information.
The user hereby agrees to keep this confidential information strictly
secret and confidential and not to disclose or reveal it, in whole
or in part, directly or indirectly to any person, entity, organization
or administration without the prior written consent of
Skyline Communications.

Any inquiries can be addressed to:

	Skyline Communications NV
	Ambachtenstraat 33
	B-8870 Izegem
	Belgium
	Tel.	: +32 51 31 35 69
	Fax.	: +32 51 31 01 29
	E-mail	: info@skyline.be
	Web		: www.skyline.be
	Contact	: Ben Vandenberghe

****************************************************************************
Revision History:

DATE		VERSION		AUTHOR			COMMENTS

13/01/2025	1.0.0.1		Skyline Communications, Skyline	Initial version
****************************************************************************
*/

using Skyline.DataMiner.Net.ResponseErrorData;

namespace GetResourceAvailability
{
    using System;
    using System.Linq;

    using Skyline.DataMiner.Analytics.GenericInterface;
    using Skyline.DataMiner.Net;
    using Skyline.DataMiner.Net.ManagerStore;
    using Skyline.DataMiner.Net.Messages;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Net.ResourceManager.Objects.ResourceAvailability;
    using Skyline.DataMiner.Net.SubscriptionFilters;

    using SLDataGateway.API.Querying;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    [GQIMetaData(Name = "Get Resource Availability")]
    public sealed class GetResourceAvailabilityDataSource : IGQIDataSource, IGQIOnInit, IGQIInputArguments, IGQIUpdateable, IGQIOnDestroy
    {
        private readonly string _subscriptionSetId = $"GetResourceUsagesDataSource_{Guid.NewGuid()}";

        private readonly GQIStringArgument _resourcePoolArgument = new GQIStringArgument("Resource Pool")
        {
            IsRequired = false
        };

        private readonly AvailabilityContext _availabilityContext;

        private GQIDMS _dms;
        private ResourceManagerHelper _rmHelper;
        private PagingHelper<Resource> _resourcePagingHelper;
        private ResourcePool _poolToFilterOn;
        private RowCollection _rowCollection;
        private IConnection _connection;
        private IGQILogger _logger;
        private SubscriptionFilter _subscriptionFilter;

        public GetResourceAvailabilityDataSource() : this(new AvailabilityContext() { Now = DateTimeOffset.Now })
        {
        }

        public GetResourceAvailabilityDataSource(AvailabilityContext availabilityContext)
        {
            _availabilityContext = availabilityContext;
        }

        public OnInitOutputArgs OnInit(OnInitInputArgs args)
        {
            _logger = args.Logger ?? throw new ArgumentNullException(nameof(args.Logger), $"Failed to initialize ad-hoc data source: logger in '{nameof(args)}' was null");
            _dms = args.DMS ?? throw new ArgumentNullException(nameof(args.DMS), $"Failed to initialize ad-hoc data source: logger in '{nameof(args)}' was null");
            _connection = _dms.GetConnection() ?? throw new ArgumentNullException(nameof(args.DMS), $"Failed to initialize ad-hoc data source: connection in 'DMS' was null"); ;
            _rmHelper = new ResourceManagerHelper(_connection.HandleSingleResponseMessage);
            _rowCollection = new RowCollection(new RowFactory(_availabilityContext));

            return default;
        }

        public GQIArgument[] GetInputArguments()
        {
            return new GQIArgument[] { _resourcePoolArgument };
        }

        public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
        {
            FillPoolToFilterOn(args);

            var resourceFilter = GetResourceFilter();

            _logger.Information($"Preparing paging for resources with filter '{resourceFilter}'");

            _resourcePagingHelper = _rmHelper.PrepareResourcePaging(resourceFilter.OrderBy(ResourceExposers.Name));

            return default;
        }

        public GQIColumn[] GetColumns()
        {
            return new GQIColumn[]
            {
                new GQIStringColumn("Resource ID"),
                new GQIDateTimeColumn("Start time"),
                new GQIDateTimeColumn("End time"),
                new GQIStringColumn("Resource Name"),
                new GQIStringColumn("Type"), // Rolling window or fixed
            };
        }

        public void OnStartUpdates(IGQIUpdater updater)
        {
            _rowCollection.Updater = updater;

            if (_connection == null)
            {
                return;
            }

            _connection.OnNewMessage += HandleResourceManagerEventMessage;

            var resourceFilter = GetResourceFilter();

            _subscriptionFilter = new SubscriptionFilter<ResourceManagerEventMessage, Resource>(resourceFilter);

            _logger.Information($"Subscribing on resource updates with filter {resourceFilter}");

            _connection.AddSubscription(_subscriptionSetId, _subscriptionFilter);

            _logger.Information($"Done subscribing on resource updates with filter {resourceFilter}");
        }

        private FilterElement<Resource> GetResourceFilter()
        {
            FilterElement<Resource> resourceFilter = new TRUEFilterElement<Resource>();
            if (_poolToFilterOn != null)
            {
                resourceFilter = ResourceExposers.PoolGUIDs.Contains(_poolToFilterOn.GUID);
            }

            return resourceFilter;
        }

        public GQIPage GetNextPage(GetNextPageInputArgs args)
        {
            try
            {
                if (!_resourcePagingHelper.MoveToNextPage())
                {
                    return new GQIPage(Array.Empty<GQIRow>())
                    {
                        HasNextPage = false
                    };
                }
            }
            catch (CrudFailedException ex) when (IsNoPermissionException(ex))
            {
                // Same behavior as built-in data sources: return an empty dataset if the user does not have permissions.
                return new GQIPage(Array.Empty<GQIRow>())
                {
                    HasNextPage = false
                };
            }

            var resources = _resourcePagingHelper.GetCurrentPage();
            _logger.Information($"Getting next page. Got {resources.Count} resources");

            var resourceRows = resources.SelectMany(r => _rowCollection.RegisterRowsForResource(r, sendUpdate: false)).ToArray();

            return new GQIPage(resourceRows)
            {
                HasNextPage = _resourcePagingHelper.HasNextPage()
            };
        }

        private static bool IsNoPermissionException(CrudFailedException ex)
        {
            return ex.TraceData != null && ex.TraceData.GetErrorDataOfType<ResourceManagerErrorData>().Any(e => e.ErrorReason == ResourceManagerErrorData.Reason.NotAllowed);
        }

        public void OnStopUpdates()
        {
            RemoveSubscription();
        }

        public OnDestroyOutputArgs OnDestroy(OnDestroyInputArgs args)
        {
            RemoveSubscription();
            _rowCollection = null;
            _rmHelper = null;
            _connection = null;
            _dms = null;
            _logger = null;

            return default;
        }

        private void FillPoolToFilterOn(OnArgumentsProcessedInputArgs args)
        {
            if (!args.TryGetArgumentValue(_resourcePoolArgument, out var poolId))
            {
                return;
            }

            if (!Guid.TryParse(poolId, out var poolGuid))
            {
                ThrowInvalidPool($"Could not parse '{poolId}' to a valid ID for a resource pool");
            }

            _poolToFilterOn = _rmHelper.GetResourcePool(poolGuid);
            if (_poolToFilterOn == null)
            {
                ThrowInvalidPool($"Could not find resource pool with ID '{poolId}'");
            }
        }

        private void ThrowInvalidPool(string errorMessage)
        {
            _logger.Error(errorMessage);
            throw new ArgumentException(errorMessage);
        }

        private void HandleResourceManagerEventMessage(object sender, NewMessageEventArgs args)
        {
            if (!(args.Message is ResourceManagerEventMessage resourceManagerEvent))
            {
                return;
            }

            foreach (var oneResource in resourceManagerEvent.DeletedResources)
            {
                _rowCollection.RemoveRowsForResource(oneResource);
            }

            foreach (var oneResource in resourceManagerEvent.UpdatedResources)
            {
                _rowCollection.RegisterRowsForResource(oneResource, sendUpdate: true);
            }
        }

        private void RemoveSubscription()
        {
            if (_connection == null)
            {
                return;
            }

            _connection.OnNewMessage -= HandleResourceManagerEventMessage;

            if (_subscriptionFilter != null)
            {
                _connection.RemoveSubscription(_subscriptionSetId, _subscriptionFilter);
            }
        }
    }
}