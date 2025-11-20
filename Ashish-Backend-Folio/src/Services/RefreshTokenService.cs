namespace Ashish_Backend_Folio.Services
{
    // Services/RefreshTokenService.cs
    using System.Security.Cryptography;
    using System.Text;
    using Ashish_Backend_Folio.Data;
    using Ashish_Backend_Folio.Dtos.Response;
    using Ashish_Backend_Folio.Interfaces;
    using Ashish_Backend_Folio.Models;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;

    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly AppDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _config;
        private readonly TimeSpan _refreshLifetime;
        private readonly bool _useHash;

        public RefreshTokenService(AppDbContext db,
                                   UserManager<ApplicationUser> userManager,
                                   IConfiguration config)
        {
            _db = db;
            _userManager = userManager;
            _config = config;

            // configurable lifetime (days)
            var days = int.Parse(_config["Jwt:RefreshTokenDays"] ?? "7");
            _refreshLifetime = TimeSpan.FromDays(days);

            // enable hashing via config "Jwt:HashRefreshTokens" = "true"
            _useHash = bool.TryParse(_config["Jwt:HashRefreshTokens"], out var h) && h;
        }

        private static string CreateSecureToken(int size = 64)
        {
            var bytes = RandomNumberGenerator.GetBytes(size);
            return Convert.ToBase64String(bytes);
        }

        private static string Hash(string token)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(token);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        private string ToStoredToken(string raw) => _useHash ? Hash(raw) : raw;

        public async Task<RefreshTokenResult> CreateRefreshTokenAsync(ApplicationUser user, CancellationToken ct = default)
        {
            var raw = CreateSecureToken();
            var stored = ToStoredToken(raw);
            var expires = DateTime.UtcNow.Add(_refreshLifetime);

            var entity = new RefreshToken
            {
                Token = stored,
                Expires = expires,
                Created = DateTime.UtcNow,
                IsRevoked = false,
                UserId = user.Id
            };

            _db.RefreshTokens.Add(entity);
            await _db.SaveChangesAsync(ct);

            return new RefreshTokenResult(raw, expires);
        }

        public async Task<(ApplicationUser? user, RefreshToken? tokenEntity)> ValidateRefreshTokenAsync(string refreshToken, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(refreshToken)) return (null, null);
            var lookup = _useHash ? Hash(refreshToken) : refreshToken;

            var tokenEntity = await _db.RefreshTokens
                .Include(rt => rt.User)
                .SingleOrDefaultAsync(rt => rt.Token == lookup, ct);

            if (tokenEntity == null) return (null, null);
            if (tokenEntity.IsRevoked || tokenEntity.Expires < DateTime.UtcNow) return (null, null);

            var user = tokenEntity.User ?? await _userManager.FindByIdAsync(tokenEntity.UserId);
            return (user, tokenEntity);
        }

        public async Task<RefreshResult> RotateRefreshTokenAsync(string refreshToken, CancellationToken ct = default)
        {
            var (user, tokenEntity) = await ValidateRefreshTokenAsync(refreshToken, ct);
            if (user == null || tokenEntity == null)
                throw new InvalidOperationException("Invalid refresh token");

            // Revoke current token and create a new one (rotation)
            tokenEntity.IsRevoked = true;
            tokenEntity.ReplacedByToken = Guid.NewGuid().ToString(); // trace id if needed

            var newRaw = CreateSecureToken();
            var newStored = ToStoredToken(newRaw);
            var newEntity = new RefreshToken
            {
                Token = newStored,
                Expires = DateTime.UtcNow.Add(_refreshLifetime),
                Created = DateTime.UtcNow,
                IsRevoked = false,
                UserId = user.Id
            };

            _db.RefreshTokens.Add(newEntity);
            await _db.SaveChangesAsync(ct);

            // create new access token is responsibility of IAuthService (token generation)
            return new RefreshResult(newRaw, newEntity.Expires.ToString()); // we'll keep signature change below
        }

        // A convenience overload if you want proper return type
        public async Task<RefreshResult> RotateRefreshTokenAsync(string refreshToken)
        {
            var (user, tokenEntity) = await ValidateRefreshTokenAsync(refreshToken);
            if (user == null || tokenEntity == null)
                throw new InvalidOperationException("Invalid refresh token");

            tokenEntity.IsRevoked = true;
            tokenEntity.ReplacedByToken = Guid.NewGuid().ToString();

            var newRaw = CreateSecureToken();
            var newStored = ToStoredToken(newRaw);
            var newEntity = new RefreshToken
            {
                Token = newStored,
                Expires = DateTime.UtcNow.Add(_refreshLifetime),
                Created = DateTime.UtcNow,
                IsRevoked = false,
                UserId = user.Id
            };

            _db.RefreshTokens.Add(newEntity);
            await _db.SaveChangesAsync();

            return new RefreshResult(newRaw, newRaw); // placeholder; actual access token created in IAuthService
        }

        public async Task<RevokeResult> RevokeRefreshTokenAsync(string? refreshToken = null, string? userId = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(refreshToken) && string.IsNullOrWhiteSpace(userId))
                return new RevokeResult(false);

            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                var lookup = _useHash ? Hash(refreshToken) : refreshToken;
                var ent = await _db.RefreshTokens.SingleOrDefaultAsync(rt => rt.Token == lookup, ct);
                if (ent == null) return new RevokeResult(false);
                ent.IsRevoked = true;
                await _db.SaveChangesAsync(ct);
                return new RevokeResult(true);
            }

            // revoke all for user
            var tokens = await _db.RefreshTokens.Where(rt => rt.UserId == userId && !rt.IsRevoked).ToListAsync(ct);
            tokens.ForEach(t => t.IsRevoked = true);
            await _db.SaveChangesAsync(ct);
            return new RevokeResult(true);
        }

        public async Task<int> CleanupExpiredTokensAsync(CancellationToken ct = default)
        {
            var stale = await _db.RefreshTokens.Where(rt => rt.Expires < DateTime.UtcNow.AddDays(-30)).ToListAsync(ct);
            if (!stale.Any()) return 0;
            _db.RefreshTokens.RemoveRange(stale);
            await _db.SaveChangesAsync(ct);
            return stale.Count;
        }
    }

}
