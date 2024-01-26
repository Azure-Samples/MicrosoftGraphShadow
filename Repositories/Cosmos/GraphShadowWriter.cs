using Azure;
using Azure.Core;
using Azure.Identity;
using Common.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph.Models;
using Microsoft.IdentityModel.Abstractions;
using Repositories.File;
using Shared;
using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Dynamic;
using System.IO;
using System.Numerics;
using System.Text;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;
using Container = Microsoft.Azure.Cosmos.Container;

namespace Repositories.Cosmos
{
    public class GraphShadowWriter : IGraphShadowWriter
    {
        private readonly AppSettings _appSettings;
        private static  IOptions<CosmosSettings>? _cosmosSettings;
        private readonly ILogger<GraphShadowWriter> _logger;

        private static CosmosClient? cosmosClient;
        private   CosmosClient InitializeCosmosClient(CosmosSettings c, AppSettings a)
        {
            if (GraphShadowWriter.cosmosClient == null)
            {
                
                if (string.IsNullOrEmpty(a.ManagedIdentityClientId))
                {
                    _logger.LogInformation("Using connection string");
                    var uri = c.CosmosEndpoint;
                    GraphShadowWriter.cosmosClient = new CosmosClient(c.ConnectionString);
                }
                else {
                    _logger.LogInformation($"Using Managed Identity {a.ManagedIdentityClientId} on {c.CosmosEndpoint}");
                    var uri = c.CosmosEndpoint;
                    var cred = new ChainedTokenCredential(new ManagedIdentityCredential(a.ManagedIdentityClientId));

                    GraphShadowWriter.cosmosClient = new CosmosClient(uri, cred);
                }
            }

            return GraphShadowWriter.cosmosClient;
        }


        public GraphShadowWriter(ILogger<GraphShadowWriter> logger, IOptions<CosmosSettings> cosmosSettings, IOptions<AppSettings> appSettings)
        {
            _logger = logger;
            _appSettings = appSettings.Value;

            GraphShadowWriter._cosmosSettings = cosmosSettings;
            InitializeCosmosClient(cosmosSettings.Value, appSettings.Value);
            
            
        }

        private static readonly JsonSerializerOptions s_jsonSerializerOptions =
            new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

        private static void SerializeGraph<T>(T t, Stream stream)
        {
            var jsonWriter = new Utf8JsonWriter(stream);
            JsonSerializer.Serialize(jsonWriter, t, s_jsonSerializerOptions);
            jsonWriter.Flush();
            stream.Position = 0;
        }

        private static T DeserializeGraph<T>(Stream stream)
        {
            // As of .NET 5, the Utf8JsonReader does not offer a way to 
            // deserialize from a stream. Read the supplied stream into 
            // a buffer until this is supported.
            byte[] jsonBytes = new byte[stream.Length];
            stream.Read(jsonBytes, 0, jsonBytes.Length);

            Utf8JsonReader reader = new Utf8JsonReader(jsonBytes,
                                                       isFinalBlock: true,
                                                       state: default);

            return JsonSerializer.Deserialize<T>(ref reader, s_jsonSerializerOptions)!;
        }

        public async Task<string> UpsertGroupEntity(string tenantId, GroupEntity item)
        {
            item.TenantId = tenantId;
            PartitionKey partitionKey = new PartitionKeyBuilder()
            .Add(item.TenantId)
            .Add(item.OdataType)            
            .Build();
            var container = cosmosClient!.GetContainer(_cosmosSettings!.Value.DatabaseId, "Entities");
            
            
            using (ResponseMessage responseMessage = await container.ReadItemStreamAsync(item.Id, partitionKey))
            {
                //CREATE NEW
                if (responseMessage.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    foreach (var data in item.AdditionalData)
                    {
                        if (data.Key.Contains("@removed"))
                        {
                            _logger.LogInformation("Entity has been deleted, on first write.");
                            return responseMessage.StatusCode.ToString(); ;
                        }
                        else
                        {
                            foreach (var elem in JsonDocument.Parse(data.Value.ToString()!).RootElement.EnumerateArray())
                            {
                                JsonElement j;
                                if (elem.TryGetProperty("@removed", out j)) { }
                                else
                                {
                                    var g = Guid.Parse(elem.GetProperty("id").ToString());
                                    item.Members.Add(g); ;
                                }
                            }

                        }
                        var d = data;
                    }
                    MemoryStream m = new MemoryStream();
                    SerializeGraph(item, m);
                    using (ResponseMessage r = await container.CreateItemStreamAsync(m, partitionKey))
                    {
                        var rr = r.StatusCode;
                        try
                        {
                            r.EnsureSuccessStatusCode();
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, e.Message);

                        }
                    }
                    m.Dispose();

                }
                //PATCH
                else
                {
                    //delete if removed
                    if (item.AdditionalData.Where(w => w.Key == "@removed").Count() == 1)
                    {
                        var ms = new MemoryStream();
                        SerializeGraph(item, ms);
                        using (ResponseMessage r = await container.DeleteItemStreamAsync(item.Id, partitionKey))
                        {
                            var rr = r.StatusCode;
                            try
                            {
                                r.EnsureSuccessStatusCode();
                            }
                            catch (Exception e)
                            {
                                _logger.LogError(e, e.Message);

                            }
                            ms.Dispose();
                            return r.StatusCode.ToString();
                        }
                    }

                    //add existing members                    
                    foreach (var members in DeserializeGraph<GroupEntity>(responseMessage.Content).Members)
                    {
                        item.Members.Add(members);
                    }

                    foreach (var data in item.AdditionalData)
                    {
                        foreach (var elem in JsonDocument.Parse(data.Value.ToString()!).RootElement.EnumerateArray())
                        {
                            JsonElement j;
                            if (elem.TryGetProperty("@removed", out j))
                            {
                                var gs = Guid.Parse(elem.GetProperty("id").ToString());
                                if (item.Members.Exists(x => x == gs))
                                {
                                    item.Members.Remove(gs);
                                };
                            }
                            else
                            {
                                var gs = Guid.Parse(elem.GetProperty("id").ToString());
                                item.Members.Add(gs); ;
                            }
                        }
                    }

                    MemoryStream m = new MemoryStream();
                    SerializeGraph(item, m);
                    using (ResponseMessage r = await container.ReplaceItemStreamAsync(m, item.Id, partitionKey))
                    {
                        var rr = r.StatusCode;
                        try
                        {
                            r.EnsureSuccessStatusCode();
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, e.Message);

                        }
                    }
                    m.Dispose();
                }
            }

            return "Ok";
        }


        public async Task<string> UpsertUserEntity(string tenantId, UserEntity item)
        {
            item.TenantId = tenantId;
            PartitionKey partitionKey = new PartitionKeyBuilder()
            .Add(item.TenantId)
            .Add(item.OdataType)
            .Build();
            var container = cosmosClient!.GetContainer(_cosmosSettings!.Value.DatabaseId, "Entities");

            try
            {

                using (ResponseMessage responseMessage = await container.ReadItemStreamAsync(item.Id, partitionKey))
                {
                    //CREATE NEW
                    if (responseMessage.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        foreach (var data in item.AdditionalData)
                        {
                            if (data.Key.Contains("@removed"))
                            {
                                _logger.LogInformation("Entity has been deleted, on first write.");
                                return responseMessage.StatusCode.ToString(); ;
                            }
                            else
                            {
                                //foreach (var elem in JsonDocument.Parse(data.Value.ToString()).RootElement.EnumerateArray())
                                //{
                                //    JsonElement j;
                                //    if (elem.TryGetProperty("@removed", out j)) { }
                                //    else
                                //    {
                                //        var g = Guid.Parse(elem.GetProperty("id").ToString());
                                //        item.MemberOf.Add(g); ;
                                //    }
                                //}

                            }
                            var d = data;
                        }
                        MemoryStream m = new MemoryStream();
                        SerializeGraph(item, m);
                        using (ResponseMessage r = await container.CreateItemStreamAsync(m, partitionKey))
                        {
                            var rr = r.StatusCode;
                            try
                            {
                                r.EnsureSuccessStatusCode();
                            }
                            catch (Exception e)
                            {
                                _logger.LogError(e, e.Message);

                            }
                        }
                        m.Dispose();

                    }
                    //PATCH
                    else
                    {
                        //delete if removed
                        if (item.AdditionalData.Where(w => w.Key == "@removed").Count() == 1)
                        {


                            var ms = new MemoryStream();
                            SerializeGraph(item, ms);
                            using (ResponseMessage r = await container.DeleteItemStreamAsync(item.Id, partitionKey))
                            {
                                
                                ms.Dispose();
                                return r.StatusCode.ToString();
                            }
                            
                            
                        }

                        //add existing members                    
                        //foreach (var members in DeserializeGraph<UserEntity>(responseMessage.Content).MemberOf)
                        //{
                        //    item.MemberOf.Add(members);
                        //}

                        foreach (var data in item.AdditionalData)
                        {
                            foreach (var elem in JsonDocument.Parse(data.Value.ToString()!).RootElement.EnumerateArray())
                            {
                                //JsonElement j;
                                //if (elem.TryGetProperty("@removed", out j))
                                //{
                                //    var gs = Guid.Parse(elem.GetProperty("id").ToString());
                                //    if (item.MemberOf.Exists(x => x == gs))
                                //    {
                                //        item.MemberOf.Remove(gs);
                                //    };
                                //}
                                //else
                                //{
                                //    var gs = Guid.Parse(elem.GetProperty("id").ToString());
                                //    item.MemberOf.Add(gs); ;
                                //}
                            }
                        }

                        MemoryStream m = new MemoryStream();
                        SerializeGraph(item, m);
                        using (ResponseMessage r = await container.ReplaceItemStreamAsync(m, item.Id, partitionKey))
                        {
                            var rr = r.StatusCode;
                            try
                            {
                                r.EnsureSuccessStatusCode();
                            }
                            catch (Exception e)
                            {
                                _logger.LogError(e, e.Message);

                            }
                        }
                        m.Dispose();


                    }
                }


            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
            return "Final";
        }


    }
}
