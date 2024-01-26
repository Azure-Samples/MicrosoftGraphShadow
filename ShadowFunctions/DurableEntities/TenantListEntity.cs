using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client.Entities;
using Microsoft.DurableTask.Client;
using Microsoft.DurableTask.Entities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using EntityTriggerAttribute = Microsoft.Azure.Functions.Worker.EntityTriggerAttribute;
using DurableClientAttribute = Microsoft.Azure.Functions.Worker.DurableClientAttribute;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace ShadowFunctions.DurableEntities
{

    public class Tenant
    {
        public Tenant()
        {

        }
        public Tenant(Guid tid)
        {
            TenantId = tid;
        }   
        public Tenant(string tid)
        {
                TenantId = Guid.Parse(tid);
        }
        public Guid  TenantId{ get; set; }
        public string UserDeltaToken { get; set; } = String.Empty;
        public string GroupDeltaToken { get; set; } = String.Empty;
        public List<SubscriptionEntity> Subscriptions { get; set; } = new List<SubscriptionEntity>();

        public class SubscriptionEntity
        {
            public string Id { get; set; } = String.Empty;
            public DateTime ExpirationDate { get; set; }
            public SubcriptionResource SubscriptionType { get; set; }
        }

        public enum SubcriptionResource {
            Users = 0,
            Groups =1 
        }
    }

    public class TenantListEntity : TaskEntity<Dictionary<Guid,Tenant>>
    {
        readonly ILogger logger;

        public TenantListEntity(ILogger<TenantListEntity> logger)
        {
            this.logger = logger;
        }

        public void Delete()
        {
            logger.LogInformation("Deleting state. " + State.Count);
            State = null!;
        }
            public void Remove(Tenant t)
        {
            State.Remove(t.TenantId);
            logger.LogInformation("Removed tenant from TenantListEntity. New tenant count: " + State.Count);
        }

        public void Add(Tenant t)
        {
            int oldCount = State.Count;
            State.TryAdd(t.TenantId, t);
            logger.LogInformation($"Update: {oldCount} -> {State.Count}");
        }
        public void Replace(Tenant t)
        {
            State[t.TenantId] = t;
            logger.LogInformation("new number: " + State.Count);
        }

        [Function(nameof(TenantListEntity))]
        public Task DispatchAsync([EntityTrigger] TaskEntityDispatcher dispatcher)
        {
            return dispatcher.DispatchAsync(this);
        }

        protected override Dictionary<Guid,Tenant> InitializeState(TaskEntityOperation entityOperation)
        {
            return new Dictionary<Guid, Tenant>();
        }
    }
}
