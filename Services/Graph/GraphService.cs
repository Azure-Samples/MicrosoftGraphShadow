using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Extensions.Options;
using Common.Models;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions;
using System.Collections;
using Microsoft.Kiota.Http.HttpClientLibrary.Middleware.Options;
using Repositories;



using Tavis.UriTemplates;
using Microsoft.Extensions.Logging;

namespace Services.Graph
{
    public partial class GraphService : IGraphService
    {

        private readonly GraphSettings _graphSettings;
        private readonly ILogger<GraphService> _logger;


        public GraphService(IOptions<GraphSettings> graphSettings, ILogger<GraphService> logger)
        {
            _graphSettings = graphSettings.Value;
            _logger = logger;

        }
        private GraphServiceClient GetGraphServiceClient(Guid tenantId)
        {
            var scopes = new[] { "https://graph.microsoft.com/.default" };

            var clientId = _graphSettings.ClientId;
            var clientSecret = _graphSettings.Secret;
            //_logger.LogInformation($"tenantId: {tenantId}, clientId: {clientId}, clientSecret: {clientSecret.Substring(0, 3)}");
            var clientSecretCredential = new ClientSecretCredential(tenantId.ToString(), clientId, clientSecret);
            var graphClient = new GraphServiceClient(clientSecretCredential, scopes);

            return graphClient;
        }
        public async Task<List<User>?> GetUsersAsync(Guid tenantId, string filter)
        {
            var client = GetGraphServiceClient(tenantId);
            var r = await client.Users.GetAsync(opts =>
            {
                opts.QueryParameters.Select = _graphSettings.UserAttributeSelection;
                if (filter != null)
                    opts.QueryParameters.Filter = filter;

            });
            return r!.Value;
        }

        public async Task<List<User>> GetUserPagesAsync(Guid tenantId, string filter = "")
        {
            var graphClient = GetGraphServiceClient(tenantId);

            var usersResponse = await graphClient
                .Users
                .GetAsync(opts =>
                {
                    opts.QueryParameters.Select = _graphSettings.UserAttributeSelection;
                    opts.QueryParameters.Expand = _graphSettings.UserExpands;
                    if (filter != "")
                        opts.QueryParameters.Filter = filter;
                });

            var userList = new List<User>();
            var pageIterator = PageIterator<User, UserCollectionResponse>.CreatePageIterator(graphClient, usersResponse!, (user) => { userList.Add(user); return true; });

            await pageIterator.IterateAsync();
            return userList;
        }

        public async Task<List<Group>?> GetGroupsAsync(Guid t)
        {
            var client = GetGraphServiceClient(t);
            var r = await client.Groups.GetAsync();
            return r!.Value;
        }

        public record GroupResponse(List<Group> groups, string deltaToken);

        public async Task<GroupResponse> GetGroupDelta(Guid tenantId, string? deltaToken, CancellationToken ct)
        {
            try
            {
                var graphClient = GetGraphServiceClient(tenantId);
                if (String.IsNullOrEmpty(deltaToken))
                {
                    var deltaResponse = await graphClient.Groups.Delta.GetAsDeltaGetResponseAsync((requestConfiguration) =>
                    {
                        requestConfiguration.QueryParameters.Select = new[] { "members", "displayName" };
                        requestConfiguration.Headers.Add("Prefer", "return=minimal");
                    });

                    List<Group> groups = new();

                    // create a page iterator to iterate through the pages of the response
                    var pageIterator = PageIterator<Group, Microsoft.Graph.Groups.Delta.DeltaGetResponse>.CreatePageIterator(graphClient, deltaResponse!, group =>
                    {
                        groups.Add(group);
                        return true;
                    });

                    await pageIterator.IterateAsync();
                    return new GroupResponse(groups, pageIterator.Deltalink);
                }
                else
                {
                    Microsoft.Graph.Groups.Delta.DeltaRequestBuilder deltaRequestBuilder = new Microsoft.Graph.Groups.Delta.DeltaRequestBuilder(deltaToken, graphClient.RequestAdapter);
                    var result = await deltaRequestBuilder.GetAsDeltaGetResponseAsync();
                    return new GroupResponse(result!.Value!, result!.OdataDeltaLink!);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message + " for tenant: " + tenantId);
                throw;
            }
        }

        public record UserResponse(List<User> users, string deltaToken);

        public async Task<UserResponse> GetUserDelta(Guid tenantId, string? deltaToken, CancellationToken ct)
        {
            try
            {
                var graphClient = GetGraphServiceClient(tenantId);
                if (String.IsNullOrEmpty(deltaToken))
                {
                    var deltaResponse = await graphClient.Users.Delta.GetAsDeltaGetResponseAsync((requestConfiguration) =>
                    {
                        requestConfiguration.QueryParameters.Select = _graphSettings.UserAttributeSelection;
                        requestConfiguration.Headers.Add("Prefer", "return=minimal");
                    });

                    List<User> users = new();

                    // create a page iterator to iterate through the pages of the response
                    var pageIterator = PageIterator<User, Microsoft.Graph.Users.Delta.DeltaGetResponse>.CreatePageIterator(graphClient, deltaResponse!, user =>
                    {
                        users.Add(user);
                        return true;
                    });

                    await pageIterator.IterateAsync();
                    return new UserResponse(users, pageIterator.Deltalink);
                }
                else
                {
                    Microsoft.Graph.Users.Delta.DeltaRequestBuilder deltaRequestBuilder = new Microsoft.Graph.Users.Delta.DeltaRequestBuilder(deltaToken, graphClient.RequestAdapter);
                    var result = await deltaRequestBuilder.GetAsDeltaGetResponseAsync();
                    return new UserResponse(result!.Value!, result!.OdataDeltaLink!);
                }

            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        /// <summary>
        /// Call Graph API to create subscription for resource. Subscription is for events: updated, deleted, create
        /// </summary>
        /// <param name="tenantId">ID of tenant</param>
        /// <param name="resource">users or groups</param>
        /// <param name="days">How long subscription should be active for. Default and max value is 2 days.</param>
        /// <returns>Subscription object</returns>
        public async Task<Subscription?> SubscribeToChanges(Guid tenantId, string resource, int days=2)
        {
            var graphClient = GetGraphServiceClient(tenantId);

            var subscription = new Subscription();
            subscription.NotificationUrl = _graphSettings.NotificationUrl;
            subscription.ChangeType = "updated, deleted, created";
            subscription.ClientState = "secretClientValue";
            subscription.Resource = resource;
            subscription.ExpirationDateTime = DateTime.Now.AddDays(days);

            var result = await graphClient.Subscriptions.PostAsync(subscription);
            return result;
        }
        public async Task<Subscription?> RenewSubscription(string subscriptionId, Guid tenantId, int numberOfDays = 28)
        {
            var graphClient = GetGraphServiceClient(tenantId);

            var subscription = new Subscription();
            subscription.Id = subscriptionId;
            subscription.ExpirationDateTime = DateTime.Now.AddDays(numberOfDays);

            var result = await graphClient.Subscriptions[subscriptionId].PatchAsync(subscription);
            return result;
        }
    }

}
















