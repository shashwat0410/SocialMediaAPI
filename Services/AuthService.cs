using Microsoft.AspNetCore.Identity;
using SocialMediaAPI.Data.Interfaces;
using SocialMediaAPI.DTOs;
using SocialMediaAPI.Models;

namespace SocialMediaAPI.Services
{
    public interface IAuthService
    {
        Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterRequestDto dto);
        Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginRequestDto dto);
        Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto dto);
        Task<ApiResponse<string>> LogoutAsync(string userId);
    }

    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly IUnitOfWork _uow;
        private readonly Configurations.JwtSettings _jwtSettings;

        public AuthService(UserManager<ApplicationUser> userManager,
            ITokenService tokenService, IUnitOfWork uow,
            Configurations.JwtSettings jwtSettings)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _uow = uow;
            _jwtSettings = jwtSettings;
        }

        public async Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterRequestDto dto)
        {
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
                return ApiResponse<AuthResponseDto>.Fail("Email already registered.");

            var user = new ApplicationUser
            {
                FullName  = dto.FullName,
                Email     = dto.Email,
                UserName  = dto.UserName,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                return ApiResponse<AuthResponseDto>.Fail("Registration failed.",
                    result.Errors.Select(e => e.Description).ToList());

            await _userManager.AddToRoleAsync(user, "User");

            return await BuildAuthResponse(user, "Registration successful.");
        }

        public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginRequestDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, dto.Password))
                return ApiResponse<AuthResponseDto>.Fail("Invalid email or password.");

            if (!user.IsActive)
                return ApiResponse<AuthResponseDto>.Fail("Your account has been deactivated.");

            // Revoke old refresh tokens on new login
            await _uow.RefreshTokens.RevokeAllUserTokensAsync(user.Id);

            return await BuildAuthResponse(user, "Login successful.");
        }

        public async Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto dto)
        {
            var principal = _tokenService.GetPrincipalFromExpiredToken(dto.AccessToken);
            if (principal == null)
                return ApiResponse<AuthResponseDto>.Fail("Invalid access token.");

            var userId = principal.FindFirst("userId")?.Value;
            if (userId == null)
                return ApiResponse<AuthResponseDto>.Fail("Invalid token claims.");

            var storedToken = await _uow.RefreshTokens.GetByTokenAsync(dto.RefreshToken);
            if (storedToken == null || !storedToken.IsActive || storedToken.UserId != userId)
                return ApiResponse<AuthResponseDto>.Fail("Invalid or expired refresh token.");

            // Rotate: revoke old, issue new
            var newRefreshToken = _tokenService.GenerateRefreshToken();
            await _uow.RefreshTokens.RevokeAsync(dto.RefreshToken, newRefreshToken);

            var user = storedToken.User;
            var roles = await _userManager.GetRolesAsync(user);
            var newAccessToken = _tokenService.GenerateAccessToken(user, roles);

            await _uow.RefreshTokens.AddAsync(new RefreshToken
            {
                Token     = newRefreshToken,
                UserId    = user.Id,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays)
            });

            return ApiResponse<AuthResponseDto>.Ok(new AuthResponseDto
            {
                AccessToken       = newAccessToken,
                RefreshToken      = newRefreshToken,
                AccessTokenExpiry = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes),
                User = new UserSummaryDto
                {
                    Id                 = user.Id,
                    UserName           = user.UserName!,
                    FullName           = user.FullName,
                    ProfilePictureUrl  = user.ProfilePictureUrl
                }
            }, "Token refreshed.");
        }

        public async Task<ApiResponse<string>> LogoutAsync(string userId)
        {
            await _uow.RefreshTokens.RevokeAllUserTokensAsync(userId);
            return ApiResponse<string>.Ok("Logged out.", "Logout successful.");
        }

        // ── Helper ──────────────────────────────────────────────
        private async Task<ApiResponse<AuthResponseDto>> BuildAuthResponse(ApplicationUser user, string message)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var accessToken = _tokenService.GenerateAccessToken(user, roles);
            var refreshToken = _tokenService.GenerateRefreshToken();

            await _uow.RefreshTokens.AddAsync(new RefreshToken
            {
                Token     = refreshToken,
                UserId    = user.Id,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays)
            });

            return ApiResponse<AuthResponseDto>.Ok(new AuthResponseDto
            {
                AccessToken       = accessToken,
                RefreshToken      = refreshToken,
                AccessTokenExpiry = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes),
                User = new UserSummaryDto
                {
                    Id                = user.Id,
                    UserName          = user.UserName!,
                    FullName          = user.FullName,
                    ProfilePictureUrl = user.ProfilePictureUrl
                }
            }, message);
        }
    }
}
