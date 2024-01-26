using AutoMapper;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask.Client;
using Microsoft.DurableTask.Client.Entities;
using Microsoft.DurableTask.Entities;
using Microsoft.Extensions.Logging;
using Repositories.Cosmos;
using Services.Graph;
using ShadowFunctions.DurableEntities;
using Shared;

namespace ShadowFunctions.DeltaFunctions
{
    public class GroupDeltaFunction
    {
        private readonly IGraphService _graphService;
        private readonly IGraphShadowWriter _repo;
        private readonly IMapper _mapper;
        private readonly ILogger<UserDeltaFunction> log;

        public GroupDeltaFunction(IGraphService graphService, IGraphShadowWriter repo, IMapper mapper, ILogger<UserDeltaFunction> logger)
        {
            _graphService = graphService;
            _repo = repo;
            _mapper = mapper;
            log = logger;
        }

        [Function("GroupDelta")]
        public async Task<string> UserDelta([ActivityTrigger] Tenant t,
            [DurableClient] DurableTaskClient client
            )
        {
            log.LogInformation($"Delta Start: {t.TenantId}");

            var entityId = new EntityInstanceId(nameof(TenantEntity), t.TenantId.ToString());
            EntityMetadata<Tenant>? entity = client.Entities.GetEntityAsync<Tenant>(
                entityId).GetAwaiter().GetResult();

            try
            {

                if (entity == null || string.IsNullOrEmpty(entity!.State.GroupDeltaToken))
                {
                    log.LogInformation($"GroupDelta. No persisted state. {t.TenantId}");

                    var r = await _graphService.GetGroupDelta(t.TenantId, null, CancellationToken.None);
                    if (r.groups.Count != 0)
                    {
                        foreach (var item in r.groups.Where(w => !w.AdditionalData.ContainsKey("@removed")))
                        {
                            var g = _mapper.Map<GroupEntity>(item);
                            await _repo.UpsertGroupEntity(t.TenantId.ToString(), g);
                        }
                        log.LogInformation($"Inital sync: {r.groups.Count}, {t.TenantId}");
                        await client.Entities.SignalEntityAsync(entityId, "UpdateGroupDelta", r.deltaToken);
                    }
                }
                else
                {
                    var r = await _graphService.GetGroupDelta(t.TenantId, entity.State.GroupDeltaToken, CancellationToken.None);
                    log.LogInformation($"Delta sync: {r.groups.Count}, {t.TenantId}");
                    if (r.groups.Count != 0)
                    {
                        foreach (var item in r.groups)
                        {
                            var g = _mapper.Map<GroupEntity>(item);
                            await _repo.UpsertGroupEntity(t.TenantId.ToString(), g);
                        }
                        log.LogInformation($"Sync: {r.groups.Count}, {t.TenantId}");
                    }
                    else
                        log.LogTrace("No deltas");
                    await client.Entities.SignalEntityAsync(entityId, "UpdateGroupDelta", r.deltaToken);
                }
            }
            catch (Exception e)
            {
                log.LogError(e, e.Message);
                await client.Entities.SignalEntityAsync(entityId, "Remove", t);
                throw;
            }
            return $"Hello {t.TenantId}!";
        }
    }
}
