Pull data, using delta tokens

Below shows the overall delta process. For each tenant known to the SaaS solution, an initial request is made, return all the entities and a delta token. The delta token is used in next request, to get the changes since the delta token was issued. A new delta token is returned when using a delta token.
[Get incremental changes for users - Microsoft Graph | Microsoft Learn](https://learn.microsoft.com/en-us/graph/delta-query-users?tabs=http)

```mermaid
sequenceDiagram
autonumber

Shadow ->> Shadow : Fetch a tenant id to shadow
activate Shadow
Note over Shadow : Run delta for a tenant
Shadow ->> Workflow state : Get deltatoken (exists if process is running)
Workflow state-->> Shadow : State
alt theres no state for the tenant - start the process
Shadow ->> Graph Tenant: Inital Request
Graph Tenant ->> Graph Tenant : Paginate through entities
Graph Tenant -->> Shadow : Entities + Deltatoken
Shadow ->> Entity store : Upset entities
Shadow ->> Workflow state : Save deltatoken for tenant

else state is known - delta is tracked
Graph Tenant ->> Graph Tenant : Paginate through deltas
Graph Tenant -->> Shadow : Entities + Deltatoken
Shadow ->> Entity store : Upset entities
Shadow ->> Workflow state : Save deltatoken for tenant
end
deactivate Shadow

```