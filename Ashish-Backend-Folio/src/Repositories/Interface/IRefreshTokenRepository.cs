using Ashish_Backend_Folio.Models;

namespace Ashish_Backend_Folio.Repositories.Interface
{
    public interface IRefreshTokenRepository : IBaseRepository<RefreshToken>
    {
        //Task AddAsync(RefreshToken token, CancellationToken ct = default);
        Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default);
        Task<List<RefreshToken>> GetByUserIdAsync(string userId, CancellationToken ct = default);
        Task<IEnumerable<RefreshToken>> CleanupExpiredTokensAsync(CancellationToken ct = default);
        //void Update(RefreshToken token); // tracked by EF Core
        //Task SaveChangesAsync(CancellationToken ct = default);
    }

}
