using Ashish_Backend_Folio.Dtos.Request;
using Ashish_Backend_Folio.Dtos.Response;
using Ashish_Backend_Folio.Interfaces;
using Ashish_Backend_Folio.Models;
using Microsoft.AspNetCore.Identity;

namespace Ashish_Backend_Folio.Services
{
    // Services/AuthService.cs
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly ITokenService _tokenService; // creates JWTs

        public AuthService(UserManager<ApplicationUser> userManager,
                           SignInManager<ApplicationUser> signInManager,
                           IRefreshTokenService refreshTokenService,
                           ITokenService tokenService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _refreshTokenService = refreshTokenService;
            _tokenService = tokenService;
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest loginModel, CancellationToken ct = default)
        {
            var user = await _userManager.FindByEmailAsync(loginModel.Email) ?? throw new UnauthorizedAccessException();

            var res = await _signInManager.CheckPasswordSignInAsync(user, loginModel.Password, lockoutOnFailure: false);
            if (!res.Succeeded) throw new UnauthorizedAccessException();

            var roles = await _userManager.GetRolesAsync(user);
            var jwt = await _tokenService.CreateTokenAsync(user, roles);

            var refresh = await _refreshTokenService.CreateRefreshTokenAsync(user, ct);

            return new AuthResponse { token = jwt, userName = user.UserName, roles = roles, refreshToken = refresh.Token };
        }

        public async Task<RefreshResult> RefreshAsync(string refreshToken, CancellationToken ct = default)
        {
            // Validate and rotate
            var (user, tokenEntity) = await _refreshTokenService.ValidateRefreshTokenAsync(refreshToken, ct);
            if (user == null || tokenEntity == null) throw new UnauthorizedAccessException();

            // rotate (revoke old, create new)
            var createRes = await _refreshTokenService.CreateRefreshTokenAsync(user, ct); // we could call RotateRefreshTokenAsync too
                                                                                          // revoke old
            await _refreshTokenService.RevokeRefreshTokenAsync(refreshToken, null, ct);

            // create new jwt
            var roles = await _userManager.GetRolesAsync(user);
            var newJwt = await _tokenService.CreateTokenAsync(user, roles);

            return new RefreshResult(newJwt, createRes.Token);
        }

        public async Task<bool> RevokeRefreshToken(string refreshToken, CancellationToken ct = default)
        {
            var revokeResult = await _refreshTokenService.RevokeRefreshTokenAsync(refreshToken, null, ct);
            return revokeResult.Success;

        }
        public async Task<bool> RevokeAllForUser(string userId, CancellationToken ct = default)
        {
            var revokeResult = await _refreshTokenService.RevokeRefreshTokenAsync(null, userId, ct);
            return revokeResult.Success;
        }
    }

}
