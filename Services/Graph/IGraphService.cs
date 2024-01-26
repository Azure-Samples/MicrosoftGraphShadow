using Microsoft.Graph.Models;
using static Services.Graph.GraphService;

namespace Services.Graph
{
    public interface IGraphService
    {
        Task<List<User>?> GetUsersAsync(Guid tenantId, string filter = "");
        Task<List<Group>?> GetGroupsAsync(Guid tenantId);
        Task<List<User>> GetUserPagesAsync(Guid t, string filter = "");
        Task<GroupResponse> GetGroupDelta(Guid tenantId, string? deltaToken, CancellationToken ct);
        Task<UserResponse> GetUserDelta(Guid tenantId, string? deltaToken, CancellationToken ct);
        Task<Subscription?> SubscribeToChanges(Guid tenantId, string resource, int days = 2);
        Task<Subscription?> RenewSubscription(string subscriptionId, Guid tenantId, int numberOfDays = 28);
    }
}