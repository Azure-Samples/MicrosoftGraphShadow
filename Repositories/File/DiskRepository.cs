using Microsoft.Graph.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Common.Models;

namespace Repositories.File
{

    public class DiskRepository<T> : IGraphShadowRepository<T> where T : Entity
    {
        private readonly IOptions<GraphSettings> _graphSettings;
        private readonly Guid _tenantId;
        JsonSerializerOptions options = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true,
        };

        public DiskRepository(Guid tenantId, IOptions<GraphSettings> graphSettings)
        {
            _graphSettings = graphSettings;
            _tenantId = tenantId;
        }
        public DiskRepository(string tenantId, IOptions<GraphSettings> graphSettings)
        {
            _graphSettings = graphSettings;
            _tenantId = Guid.Parse(tenantId);
        }

        public T Get(Guid id)
        {
            throw new NotImplementedException();
        }

        public List<T> GetAll()
        {
            throw new NotImplementedException();
        }

        public void Save(T item)
        {

            string jsonString = JsonSerializer.Serialize(item, options);
            string path = $@"c:/temp/data/{_tenantId.ToString()}";

            switch (item.OdataType)
            {
                case "#microsoft.graph.user":
                    path = $"{path}/users";
                    User? u = item as User;

                    break;
                case "#microsoft.graph.group":
                    path = $"{path}/groups";
                    Group? g = item as Group;
                    foreach (var operation in g!.AdditionalData)
                    {
                        var ops = operation.Key;
                        var val = operation.Value;

                    }
                    break;
                default:
                    throw new NotImplementedException();
            }

            DirectoryInfo di = new DirectoryInfo(path);
            if (!di.Exists) di.Create();

            System.IO.File.WriteAllText($"{path}/{item.Id}.json", jsonString);
        }
        private void Delete(T item)
        {
            string path = $@"c:/temp/data/{_tenantId.ToString()}";

            switch (item.OdataType)
            {
                case "#microsoft.graph.user":
                    path = $"{path}/users";
                    User? u = item as User;

                    break;
                case "#microsoft.graph.group":
                    Group? g = item as Group;
                    
                    path = $"{path}/groups";
                    break;
                default:
                    throw new NotImplementedException();
            }
            var file = $"{path}/{item.Id}.json";
            if(System.IO.File.Exists(file))
                System.IO.File.Delete(file);

        }
        public void Save(List<T> items)
        {
            foreach (var item in items)
            {
                Save(item);
            }
        }

        public void Upsert(T item)
        {
            if (item.AdditionalData.ContainsKey("@removed"))
            {
                Delete(item);
            }
            else
            {
                Save(item);
            }



        }
    }
}

