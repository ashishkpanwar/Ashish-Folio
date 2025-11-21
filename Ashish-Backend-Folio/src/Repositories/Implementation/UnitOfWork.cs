using Ashish_Backend_Folio.Data;
using Ashish_Backend_Folio.Models;
using Ashish_Backend_Folio.Repositories.Interface;

namespace Ashish_Backend_Folio.Repositories.Implementation
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
            RefreshTokens = new RefreshTokenRepository(_context);
        }

        public IRefreshTokenRepository RefreshTokens { get; private set; }

        public async Task<int> CommitAsync(CancellationToken ct = default)
        {
            return await _context.SaveChangesAsync(ct);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }

}
