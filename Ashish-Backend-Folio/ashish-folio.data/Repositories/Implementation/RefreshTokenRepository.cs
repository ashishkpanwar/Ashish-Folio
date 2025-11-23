using Ashish_Backend_Folio.Data.Models;
using Ashish_Backend_Folio.Data.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace Ashish_Backend_Folio.Data.Repositories.Implementation
{
    public class RefreshTokenRepository : BaseRepository<RefreshToken>, IRefreshTokenRepository
    {
        private readonly AppDbContext _db;
        public RefreshTokenRepository(AppDbContext db) :base(db) { }
        
        public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default)
        {
            return await _db.RefreshTokens
                .Include(t => t.User)
                .Where(t => t.Token == token)
                .FirstOrDefaultAsync(ct);
        }
        public async Task<List<RefreshToken>> GetByUserIdAsync(string userId, CancellationToken ct = default)
        => await _db.RefreshTokens.Where(t => t.UserId == userId && !t.IsRevoked).ToListAsync(ct);
        
        public async Task<IEnumerable<RefreshToken>> CleanupExpiredTokensAsync(CancellationToken ct = default) => 
            await _db.RefreshTokens.Where(rt => rt.Expires < DateTime.UtcNow.AddDays(-30)).ToListAsync(ct);

        //public async Task SaveChangesAsync(CancellationToken ct = default) => await _db.SaveChangesAsync(ct);
    }
}
