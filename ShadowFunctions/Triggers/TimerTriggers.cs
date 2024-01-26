using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask.Client;
using Microsoft.DurableTask.Client.Entities;
using Microsoft.DurableTask.Entities;
using Microsoft.Extensions.Logging;
using ShadowFunctions.DurableEntities;
using ShadowFunctions.Orchestrators;

namespace ShadowFunctions.Triggers
{
    public class TimerTriggers
    {
        private readonly ILogger _logger;

        public TimerTriggers(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<TimerTriggers>();
        }
        //https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-timer?tabs=python-v2%2Cisolated-process%2Cnodejs-v4&pivots=programming-language-csharp#attributes
        // RunOnStartup should not be set to true. This is for ensuring that webhook subscriptions are renewed in the "run" function, when passing the Helpers.TenantRepository
        [Function("LongPolling")]
        public void RunLongPolling([TimerTrigger("0 0 3 * * *", RunOnStartup = true)] TimerInfo myTimer,
            [DurableClient] DurableTaskClient client
            )
        {
            Run(client, Helpers.TenantRepository);
        }

        [Function("ShortPolling")]
        public void RunShortPolling([TimerTrigger("0 */3 * * * *")] TimerInfo myTimer,
            [DurableClient] DurableTaskClient client)
        {
            Run(client, Helpers.ProcessingList);
        }

        private void Run(DurableTaskClient client, string scope)
        {
            try
            {
                var entityId = new EntityInstanceId(nameof(TenantListEntity), scope);

                EntityMetadata<Dictionary<Guid, Tenant>>? entity = client.Entities.GetEntityAsync<Dictionary<Guid, Tenant>>(
                    entityId).GetAwaiter().GetResult();

                if (entity != null)
                {
                    _logger.LogInformation($"Tenants to be processed: " + entity!.State.Count());

                    foreach (var item in entity.State)
                    {
                        var r = client.GetInstanceAsync(item.Key.ToString()).GetAwaiter().GetResult();
                        if (r == null
                            || r.RuntimeStatus == OrchestrationRuntimeStatus.Completed
                            || r.RuntimeStatus == OrchestrationRuntimeStatus.Failed
                            || r.RuntimeStatus == OrchestrationRuntimeStatus.Terminated)
                        {
                            if (scope == Helpers.ProcessingList)
                            {
                                var res = client.ScheduleNewOrchestrationInstanceAsync(nameof(FunctionOrchestrator), false, new Microsoft.DurableTask.StartOrchestrationOptions() { InstanceId = item.Key.ToString() }).GetAwaiter().GetResult();
                                var e = new EntityInstanceId(nameof(TenantListEntity), Helpers.ProcessingList);
                                client.Entities.SignalEntityAsync(e, "Remove", new Tenant(item.Key));
                            }
                            if (scope == Helpers.TenantRepository)
                            {
                                var res = client.ScheduleNewOrchestrationInstanceAsync(nameof(FunctionOrchestrator), true, new Microsoft.DurableTask.StartOrchestrationOptions() { InstanceId = item.Key.ToString(), }).GetAwaiter().GetResult();
                            }
                        }
                        else if (r.RuntimeStatus == OrchestrationRuntimeStatus.Running)
                        {
                            _logger.LogWarning("Job already running:" + item.Value.ToString());
                        }
                    }
                    return;
                }
                else
                    _logger.LogInformation($"No tenants to be processed");

            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
