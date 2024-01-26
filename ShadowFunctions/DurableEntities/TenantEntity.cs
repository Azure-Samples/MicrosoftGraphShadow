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

namespace ShadowFunctions.DurableEntities
{
    public class TenantEntity : TaskEntity<Tenant>
    {
        readonly ILogger logger;

        public TenantEntity(ILogger<TenantEntity> logger)
        {
            this.logger = logger;
        }

        public void Remove(Tenant t)
        {
            State = null!;
        }

        public void Delete(Tenant t)
        {
            Remove(t);
        }
        public void Add(Tenant t)
        {
            State = t;
        }

        public void UpdateGroupDelta(string delta)
        {
            State.GroupDeltaToken = delta;
        }
        public void UpdateUserDelta( string delta)
        {
            State.UserDeltaToken= delta;
        }

        public void AddSubscription(Tenant.SubscriptionEntity sub)
        {
            if (!State.Subscriptions.Contains(sub))
                State.Subscriptions.Add(sub);
        }
        public void UpdateSubscriptionExpireDate(Tenant.SubscriptionEntity sub)
        {
            var subscription = State.Subscriptions.FirstOrDefault(x => x.Id == sub.Id);
            if (subscription != null)
                subscription.ExpirationDate = sub.ExpirationDate;
        }
        
        public void RemoveSubscription(string subscriptionId)
        {
            var subscription = State.Subscriptions.FirstOrDefault(s => s.Id == subscriptionId);

            if (subscription != null)
                State.Subscriptions.Remove(subscription);
        }

        [Function(nameof(TenantEntity))]
        public Task DispatchAsync([EntityTrigger] TaskEntityDispatcher dispatcher)
        {
            return dispatcher.DispatchAsync(this);
        }
        protected override Tenant InitializeState(TaskEntityOperation entityOperation)
        {
            return new Tenant(Guid.Parse(this.Context.Id.Key)) ;
        }
    }
}