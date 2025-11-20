using Ashish_Backend_Folio.Interfaces;
using Ashish_Backend_Folio.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Ashish_Backend_Folio.Data;
using Microsoft.EntityFrameworkCore;
using Ashish_Backend_Folio.Dtos.Request;
using Ashish_Backend_Folio.Dtos.Response;
using Ashish_Backend_Folio.Dtos.Common;

namespace Ashish_Backend_Folio.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly AppDbContext _db;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IAuthService _authService;


        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ITokenService tokenService,
            AppDbContext dbContext,
            IRefreshTokenService refreshTokenService,
            IAuthService authService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _db = dbContext;
            _refreshTokenService = refreshTokenService;
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest model)
        {
            try
            {
                var regiterResult = await _authService.RegisterUserAsync(model);

                return Ok(regiterResult);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            try
            {
                var res = await _authService.LoginAsync(model);
                return Ok(res);
            }
            catch (UnauthorizedAccessException) { return Unauthorized(); }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(RefreshDto dto)
        {
            // find token entit

            try
            {
                var res = await _authService.RefreshAsync(dto.RefreshToken);
                return Ok(new { token = res.AccessToken, refreshToken = res.RefreshToken });
            }
            catch (UnauthorizedAccessException) { return Unauthorized(); }
        }

        [HttpPost("revoke")]
        [Authorize]
        public async Task<IActionResult> Revoke([FromBody] RefreshDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (dto.RefreshToken != null)
            {
                var r = await _authService.RevokeRefreshToken(dto.RefreshToken); // you may add revoke method on IAuthService that calls RefreshTokenService
                return r ? Ok() : NotFound();
            }
            else
            {
                var r = await _authService.RevokeAllForUser(userId!);
                return r ? Ok() : NotFound();
            }
        }



        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            var roles = await _userManager.GetRolesAsync(user);
            return Ok(new { user.UserName, user.Email, user.DisplayName, roles });
        }
    }
}
