using Azure;
using Azure.Core;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.DurableTask.Client.Entities;
using Microsoft.DurableTask.Entities;
using Microsoft.Extensions.Logging;
using ShadowFunctions.DurableEntities;
using System.Security.Cryptography;

namespace ShadowFunctions.Orchestrators
{
    public static class FunctionOrchestrator
    {
        [Function(nameof(FunctionOrchestrator))]
        public static async Task<List<string>> RunOrchestrator(
            [Microsoft.Azure.Functions.Worker.OrchestrationTrigger] TaskOrchestrationContext context, bool renewSubscription = false)
        {
            ILogger logger = context.CreateReplaySafeLogger(nameof(FunctionOrchestrator));
            logger.LogInformation("Running orchestration for tenant: " + context.InstanceId);
            var tenantId = Guid.Parse(context.InstanceId);

            var r = await context.CallActivityAsync<string>("UserDelta", new Tenant(tenantId));
            var r2 = await context.CallActivityAsync<string>("GroupDelta", new Tenant(tenantId));
            string r3 = String.Empty;
            if (renewSubscription)
            {
                logger.LogInformation("Renewing subscription: " + context.InstanceId);
                r3 = await context.CallActivityAsync<string>("ProcessSubscriptions", new Tenant(tenantId));
            }
            return new List<string> { r, r2, r3! };

        }
    }
}

        

