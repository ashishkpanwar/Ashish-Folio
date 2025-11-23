using Ashish_Backend_Folio.Data.Repositories.Interface;

namespace Ashish_Backend_Folio.Data.Repositories.Implementation
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        public UnitOfWork(AppDbContext context, 
            IRefreshTokenRepository refreshTokenRepository)
        {
            _context = context;
            RefreshTokens = refreshTokenRepository;
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
