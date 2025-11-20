using Ashish_Backend_Folio.Dtos.Request;
using Ashish_Backend_Folio.Dtos.Response;

namespace Ashish_Backend_Folio.Interfaces
{
    // Services/Interfaces/IAuthService.cs
    public interface IAuthService
    {
        Task<AuthResponse> RegisterUserAsync(RegisterRequest model, CancellationToken ct = default);
        Task<AuthResponse> LoginAsync(LoginRequest model, CancellationToken ct = default);
        Task<RefreshResult> RefreshAsync(string refreshToken, CancellationToken ct = default);

        Task<bool> RevokeRefreshToken(string refreshToken, CancellationToken ct = default);
        Task<bool> RevokeAllForUser(string userId, CancellationToken ct = default);

    }

}
