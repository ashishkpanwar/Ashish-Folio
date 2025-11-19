using Ashish_Backend_Folio.Dtos;
using Ashish_Backend_Folio.Helper;
using Ashish_Backend_Folio.Interfaces;
using Ashish_Backend_Folio.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Ashish_Backend_Folio.Data;
using Microsoft.EntityFrameworkCore;

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


        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ITokenService tokenService,
            AppDbContext dbContext)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _db = dbContext;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest model)
        {
            var existing = await _userManager.FindByEmailAsync(model.Email);
            if (existing != null)
                return BadRequest(new { message = "Email is already taken" });

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                DisplayName = model.DisplayName
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            // Optionally assign role: default "User"
            await _userManager.AddToRoleAsync(user, "User");

            var roles = await _userManager.GetRolesAsync(user);
            var token = await _tokenService.CreateTokenAsync(user, roles);

            return Ok(new AuthResponse { Token = token, UserName = user.UserName, Roles = roles });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return Unauthorized(new { message = "Invalid login attempt" });

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: false);
            if (!result.Succeeded)
                return Unauthorized(new { message = "Invalid login attempt" });

            var roles = await _userManager.GetRolesAsync(user);
            var token = await _tokenService.CreateTokenAsync(user, roles);

            // create refresh token
            var refreshToken = TokenGenerator.CreateSecureToken();
            var expires = DateTime.UtcNow.AddDays(7);

            var refreshEntity = new RefreshToken
            {
                Token = refreshToken,
                Expires = expires,
                UserId = user.Id
            };

            _db.RefreshTokens.Add(refreshEntity);
            await _db.SaveChangesAsync();

            return Ok(new AuthResponse { Token = token, UserName = user.UserName, Roles = roles, refreshToken = refreshToken });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(RefreshDto dto)
        {
            // find token entity
            var tokenEntity = await _db.RefreshTokens.Include(rt => rt.User)
                .SingleOrDefaultAsync(t => t.Token == dto.RefreshToken);

            if (tokenEntity == null) return Unauthorized("Invalid refresh token");
            if (tokenEntity.IsRevoked) return Unauthorized("Refresh token revoked");
            if (tokenEntity.Expires < DateTime.UtcNow) return Unauthorized("Refresh token expired");

            var user = tokenEntity.User!;
            if (user == null) return Unauthorized();

            // rotate: revoke old token and create a new one
            tokenEntity.IsRevoked = true;
            var newRefreshToken = TokenGenerator.CreateSecureToken();
            var newTokenEntity = new RefreshToken
            {
                Token = newRefreshToken,
                Expires = DateTime.UtcNow.AddDays(7),
                UserId = user.Id,
                Created = DateTime.UtcNow,
                ReplacedByToken = null
            };

            tokenEntity.ReplacedByToken = newRefreshToken;
            _db.RefreshTokens.Add(newTokenEntity);
            await _db.SaveChangesAsync();

            var roles = await _userManager.GetRolesAsync(user);
            var newJwt = _tokenService.CreateTokenAsync(user, roles);

            return Ok(new { token = newJwt, refreshToken = newRefreshToken });
        }

        [HttpPost("revoke")]
        [Authorize]
        public async Task<IActionResult> Revoke()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (userId == null) return Unauthorized();

            // revoke all refresh tokens for the user, or revoke a specific one passed in the body
            var tokens = await _db.RefreshTokens.Where(t => t.UserId == userId && !t.IsRevoked && t.Expires > DateTime.UtcNow).ToListAsync();
            foreach (var t in tokens) t.IsRevoked = true;
            await _db.SaveChangesAsync();

            return Ok();
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
