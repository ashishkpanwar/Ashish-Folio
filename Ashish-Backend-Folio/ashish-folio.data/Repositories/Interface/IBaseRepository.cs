namespace Ashish_Backend_Folio.Data.Repositories.Interface
{
    public interface IBaseRepository<T> where T : class   // what is where T : class?
    {
        Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default);
        Task AddAsync(T entity, CancellationToken ct = default);
        void Update(T entity); // why no ct for update and remove and Query
        void Remove(T entity);
        void RemoveRange(IEnumerable<T> entities);
        IQueryable<T> Query(); // For advanced queries with Include, Where, etc.
    }

}
