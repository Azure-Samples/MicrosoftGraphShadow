using AutoMapper;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.DurableTask.Client.Entities;
using Microsoft.DurableTask.Entities;
using Microsoft.Extensions.Logging;
using Repositories.Cosmos;
using Services.Graph;
using ShadowFunctions.DurableEntities;
using Shared;
using System.Security.Cryptography;

namespace ShadowFunctions.DeltaFunctions
{
    public class UserDeltaFunction
    {
        private readonly IGraphService _graphService;
        private readonly IGraphShadowWriter _repo;
        private readonly IMapper _mapper;
        private readonly ILogger<UserDeltaFunction> log;

        public UserDeltaFunction(IGraphService graphService, IGraphShadowWriter repo, IMapper mapper, ILogger<UserDeltaFunction> logger)
        {
            _graphService = graphService;
            _repo = repo;
            _mapper = mapper;
            log = logger;
        }
        [Function("UserDelta")]
        public async Task<string> UserDelta([ActivityTrigger] Tenant t,
            [DurableClient] DurableTaskClient client)
        {
            log.LogInformation($"Delta Start: {t.TenantId}");
            var entityId = new EntityInstanceId(nameof(TenantEntity), t.TenantId.ToString());
            EntityMetadata<Tenant>? entity = client.Entities.GetEntityAsync<Tenant>(
                entityId).GetAwaiter().GetResult();

            try
            {
                if (entity == null || string.IsNullOrEmpty(entity!.State.UserDeltaToken))
                {
                    var r = await _graphService.GetUserDelta(t.TenantId, null, CancellationToken.None);
                    if (r.users.Count != 0)
                    {
                        foreach (var item in r.users.Where(w => !w.AdditionalData.ContainsKey("@removed")))
                        {
                            var g = _mapper.Map<UserEntity>(item);
                            await _repo.UpsertUserEntity(t.TenantId.ToString(), g);
                        }
                        log.LogInformation($"Inital sync: {r.users.Count}, {t.TenantId}");

                        await client.Entities.SignalEntityAsync(entityId, "UpdateUserDelta", r.deltaToken);
                    }
                }
                else
                {
                    var r = await _graphService.GetUserDelta(t.TenantId, entity.State.UserDeltaToken, CancellationToken.None);
                    log.LogInformation($"Delta sync: {r.users.Count}, {t.TenantId}");

                    if (r.users.Count != 0)
                    {
                        foreach (var item in r.users)
                        {
                            var g = _mapper.Map<UserEntity>(item);
                            await _repo.UpsertUserEntity(t.TenantId.ToString(), g);
                        }
                    }
                    else
                        log.LogTrace("No deltas");

                    await client.Entities.SignalEntityAsync(entityId, "UpdateUserDelta", r.deltaToken);
                }
            }
            catch (Exception e)
            {
                log.LogError(e, e.Message);
                await client.Entities.SignalEntityAsync(entityId, "Remove", t);
                throw;
            }

            return $"{t.TenantId}!";
        }

    }
}
