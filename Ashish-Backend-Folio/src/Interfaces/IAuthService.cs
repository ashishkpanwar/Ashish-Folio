using Ashish_Backend_Folio.Dtos.Request;
using Ashish_Backend_Folio.Dtos.Response;

namespace Ashish_Backend_Folio.Interfaces
{
    // Services/Interfaces/IAuthService.cs
    public interface IAuthService
    {
        Task<AuthResponse> LoginAsync(LoginRequest model, CancellationToken ct = default);
        Task<RefreshResult> RefreshAsync(string refreshToken, CancellationToken ct = default);
    }

}
