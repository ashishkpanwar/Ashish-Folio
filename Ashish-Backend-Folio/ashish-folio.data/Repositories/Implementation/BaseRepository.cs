using Ashish_Backend_Folio.Data.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace Ashish_Backend_Folio.Data.Repositories.Implementation
{
    public class BaseRepository<T> : IBaseRepository<T> where T : class
    {
        protected readonly AppDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public BaseRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            await _dbSet.FindAsync(new object?[] { id, ct }, cancellationToken: ct);

        public async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default) =>
            await _dbSet.ToListAsync(ct);

        public async Task AddAsync(T entity, CancellationToken ct = default) =>
            await _dbSet.AddAsync(entity, ct);

        public void Update(T entity) =>
            _dbSet.Update(entity);

        public void Remove(T entity) =>
            _dbSet.Remove(entity);
        public void RemoveRange(IEnumerable<T> entities) =>
            _dbSet.RemoveRange(entities);

        public IQueryable<T> Query() => _dbSet.AsQueryable();
    }

}
