using Ashish_Backend_Folio.Dtos.Response;
using Ashish_Backend_Folio.Models;

namespace Ashish_Backend_Folio.Interfaces
{
    // Services/Interfaces/IRefreshTokenService.cs
    public interface IRefreshTokenService
    {
        /// <summary>Creates & persists a new refresh token for the given user. Returns raw token to send to client.</summary>
        Task<RefreshTokenResult> CreateRefreshTokenAsync(ApplicationUser user, CancellationToken ct = default);

        /// <summary>Validates the incoming refresh token, returns the user if valid; does not rotate.</summary>
        Task<(ApplicationUser? user, RefreshToken? tokenEntity)> ValidateRefreshTokenAsync(string refreshToken, CancellationToken ct = default);

        /// <summary>Rotates the provided refresh token: revokes old token, creates new token, returns new token.</summary>
        Task<RefreshResult> RotateRefreshTokenAsync(string refreshToken, CancellationToken ct = default);

        /// <summary>Revoke a refresh token (by token string) or all tokens for a user.</summary>
        Task<RevokeResult> RevokeRefreshTokenAsync(string? refreshToken = null, string? userId = null, CancellationToken ct = default);

        /// <summary>Optional cleanup helper to delete expired tokens.</summary>
        Task<int> CleanupExpiredTokensAsync(CancellationToken ct = default);
    }

}
