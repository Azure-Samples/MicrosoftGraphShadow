using System.Net;
using Common.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.DurableTask.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShadowFunctions.DurableEntities;

namespace ShadowFunctions.Triggers
{
    public class ConsentFunction
    {
        private readonly ILogger _logger;
        private readonly IOptions<GraphSettings> _settings;

        public ConsentFunction(ILoggerFactory loggerFactory, IOptions<GraphSettings> graphSettings)
        {
            _logger = loggerFactory.CreateLogger<ConsentFunction>();
            _settings = graphSettings;
        }

        [Function("ConsentFunction")]
        public HttpResponseData Run(
            [DurableClient] DurableTaskClient client,
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "signup/{tenantId}")] HttpRequestData req, Guid tenantId)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            
            string html =
                $@" <!DOCTYPE html><html><body>
                    <p>Consent</p>
                        <a href='https://login.microsoftonline.com/organizations/adminconsent?client_id={_settings.Value.ClientId}'>Link</a>
                    </body></html>
                ";
            response.WriteString(html);
            var entityId = new EntityInstanceId(nameof(TenantListEntity), Helpers.TenantRepository);
             client.Entities.SignalEntityAsync(entityId, "Add", new Tenant(tenantId));
            return response;
        }
    }
}
