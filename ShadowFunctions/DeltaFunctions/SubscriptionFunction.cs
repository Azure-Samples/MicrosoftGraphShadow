using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask.Client;
using Microsoft.DurableTask.Client.Entities;
using Microsoft.DurableTask.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Services.Graph;
using ShadowFunctions.DurableEntities;
using ActivityTriggerAttribute = Microsoft.Azure.Functions.Worker.ActivityTriggerAttribute;
using DurableClientAttribute = Microsoft.Azure.Functions.Worker.DurableClientAttribute;


namespace ShadowFunctions.DeltaFunctions
{
    internal class SubscriptionFunction
    {
        private readonly IGraphService _graphService;
        private readonly ILogger<SubscriptionFunction> _logger;

        public SubscriptionFunction(IGraphService graphService, ILogger<SubscriptionFunction> logger)
        {
            _graphService = graphService;
            _logger = logger;
        }

        [Function("ProcessSubscriptions")]
        public async Task ProcessSubscriptions([ActivityTrigger] Tenant tenant, [DurableClient] DurableTaskClient client)
        {
            try
            {
                if (tenant==null || tenant.TenantId.ToString().IsNullOrEmpty())
                    throw new Exception("tenantId is empty");
                _logger.LogInformation(string.Format("CreateSubscription function started for tenant {0}", tenant.TenantId.ToString()));

                var tenantId = tenant.TenantId.ToString();

                var entityId = new EntityInstanceId(nameof(TenantEntity), tenant.TenantId.ToString());
                EntityMetadata<Tenant>? entity = client.Entities.GetEntityAsync<Tenant>(
                    entityId).GetAwaiter().GetResult();

                if (entity!.State.Subscriptions.IsNullOrEmpty())
                {
                    var usersResult = await CreateSubscription("users", tenantId);
                    var groupsResult = await CreateSubscription("groups", tenantId);
                    await client.Entities.SignalEntityAsync(entityId, "AddSubscription", usersResult);
                    await client.Entities.SignalEntityAsync(entityId, "AddSubscription", groupsResult);
                }
                else
                {
                    foreach (var subscription in entity!.State.Subscriptions)
                    {
                        await RenewSubscription(subscription.Id, tenantId);
                        await client.Entities.SignalEntityAsync(entityId, "UpdateSubscriptionExpireDate", subscription);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        private async Task<Tenant.SubscriptionEntity> CreateSubscription(string resourceType, string tenantId)
        {
            try
            {
                if (resourceType.IsNullOrEmpty() || tenantId.IsNullOrEmpty())
                    throw new Exception("resourceType or tenantId is empty");

                var result = await _graphService.SubscribeToChanges(new Guid(tenantId), resourceType);
                if (result != null)
                {
                    Tenant.SubcriptionResource subscriptionType;
                    if (resourceType.ToLower().Equals("users"))
                        subscriptionType = Tenant.SubcriptionResource.Users;
                    else if (resourceType.ToLower().Equals("groups"))
                        subscriptionType = Tenant.SubcriptionResource.Groups;
                    else
                        throw new Exception(string.Format("Unsupported subscription resource type: ", resourceType));

                    var subscriptionEntity = new Tenant.SubscriptionEntity() { Id = result.Id!, ExpirationDate = result.ExpirationDateTime!.Value.DateTime, SubscriptionType = subscriptionType };
                    return subscriptionEntity;
                }
                else
                    throw new Exception(string.Format("IGraphService SubscribeToChanges for tenant {0} returned null value - Subscription failed", tenantId));
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        private async Task<string> RenewSubscription(string subscriptionId, string tenantId)
        {
            try
            {
                if (subscriptionId.IsNullOrEmpty() || tenantId.IsNullOrEmpty())
                    throw new Exception("subscriptionId or tenantId is empty");

                var result = await _graphService.RenewSubscription(subscriptionId, new Guid(tenantId));

                if (result != null)
                {
                    return result.Id!;
                }
                else
                    throw new Exception(string.Format("IGraphService SubscribeToChanges for tenant {0} returned null value - Subscription failed", tenantId.ToString()));
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
