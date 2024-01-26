using Microsoft.Azure.Cosmos;
using Microsoft.Graph.Models;
using Repositories.File;
using Shared;

namespace Repositories.Cosmos
{
    public interface IGraphShadowWriter
    {
        
        Task<string> UpsertGroupEntity(string tenantId, GroupEntity item);
        Task<string> UpsertUserEntity(string tenantId, UserEntity item);
   }
}