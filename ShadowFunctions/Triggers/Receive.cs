using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.DurableTask.Entities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ShadowFunctions.DurableEntities;
using Shared;

namespace ShadowFunctions.Triggers
{
    public class Receive
    {
        [Function("Receive")]
        public static async Task<HttpResponseData> HttpStart(
           [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
           [DurableClient] DurableTaskClient client,
           FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("Receive");
            
            string token = req.Query["validationToken"]!;
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            dynamic data = JsonConvert.DeserializeObject(requestBody)!;
            token = token ?? data?.token!;

            var response = HttpResponseData.CreateResponse(req);

            //if this is a subscription confirmation request, we need to return confirmation token as part of the response body
            if (!string.IsNullOrEmpty(token))
            {
                logger.LogInformation("Recieved subscription confirmation request");
                response.StatusCode = HttpStatusCode.OK;
                await response.WriteStringAsync(token);
                return response;
            }

            //else we extract payload which has notification about updated resource
            if (data == null || data!.value == null)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync("data or data value is null");
                return response;
            }

            NotificationValue notification= JsonConvert.DeserializeObject<NotificationValue>(requestBody)!;
            var entityId = new EntityInstanceId(nameof(TenantListEntity), Helpers.ProcessingList);
            logger.LogInformation($"Subscription recieved from: {notification.value.First().tenantId}, {notification.value.First().resource}, {notification.value.First().changeType}");
            await client.Entities.SignalEntityAsync(entityId, "Add", new Tenant(notification.value.First().tenantId));
            response.StatusCode = HttpStatusCode.OK;
            return response;
        }
    }
}
