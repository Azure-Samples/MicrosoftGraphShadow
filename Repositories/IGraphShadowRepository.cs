using Microsoft.Graph.Models;

namespace Repositories
{
    public interface IGraphShadowRepository<T>  where T : Entity
    {
        void Save(T item);
        void Upsert(T item);
        void Save(List<T> items);
        T Get(Guid id);
        List<T> GetAll();

    }
}

