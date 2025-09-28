using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;
using Backend.Services;
using System.IdentityModel.Tokens.Jwt;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly AzureAuthService _azureAuth;
    private readonly JwtService _jwtService;
    private readonly ILogger<AuthController> _logger;
    
    public AuthController(
        AppDbContext context, 
        AzureAuthService azureAuth, 
        JwtService jwtService,
        ILogger<AuthController> logger)
    {
        _context = context;
        _azureAuth = azureAuth;
        _jwtService = jwtService;
        _logger = logger;
    }
    
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            _logger.LogInformation("Login attempt with Azure token");
            
            // 1. Azure 토큰 검증 (매번!)
            var azureUser = await _azureAuth.ValidateTokenAndGetUser(request.AzureToken);
            if (azureUser == null)
            {
                _logger.LogWarning("Invalid Azure token");
                return Unauthorized(new { error = "Invalid Azure token" });
            }
            
            // 2. Rate Limiting 체크 (간단한 구현)
            var recentLogins = await _context.RefreshTokens
                .Where(rt => rt.User.AzureId == azureUser.Id)
                .Where(rt => rt.CreatedAt > DateTime.UtcNow.AddMinutes(-1))
                .CountAsync();
            
            if (recentLogins > 5)
            {
                _logger.LogWarning($"Too many login attempts for user {azureUser.Email}");
                return StatusCode(429, new { error = "Too many login attempts. Please wait a moment." });
            }
            
            // 3. 사용자 조회 또는 생성
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.AzureId == azureUser.Id);
            
            if (user == null)
            {
                _logger.LogInformation($"Creating new user for {azureUser.Email}");
                user = new User
                {
                    AzureId = azureUser.Id,
                    Email = azureUser.Email,
                    DisplayName = azureUser.DisplayName,
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }
            else
            {
                user.LastLoginAt = DateTime.UtcNow;
            }
            
            // 4. 기존 Refresh Token 무효화
            var existingTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == user.Id && !rt.IsRevoked)
                .ToListAsync();
            
            foreach (var token in existingTokens)
            {
                token.IsRevoked = true;
            }
            
            // 5. 새 토큰 생성
            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken(user);
            
            // 6. Refresh Token DB 저장
            _context.RefreshTokens.Add(new RefreshToken
            {
                Token = refreshToken,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow,
                DeviceInfo = Request.Headers["User-Agent"].ToString()
            });
            
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"User {user.Email} logged in successfully");
            
            return Ok(new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = 1800, // 30분
                UserEmail = user.Email
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
    
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        try
        {
            _logger.LogInformation("Token refresh attempt");
            
            // 1. Refresh Token 검증
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(request.RefreshToken);
            
            var userId = jsonToken.Claims
                .FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Invalid refresh token - no user ID");
                return Unauthorized(new { error = "Invalid refresh token" });
            }
            
            // 2. DB에서 Refresh Token 확인
            var storedToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => 
                    rt.Token == request.RefreshToken && 
                    !rt.IsRevoked &&
                    rt.ExpiresAt > DateTime.UtcNow);
            
            if (storedToken == null)
            {
                _logger.LogWarning("Refresh token not found or expired");
                return Unauthorized(new { error = "Invalid or expired refresh token" });
            }
            
            // 3. 새 Access Token 발급
            var newAccessToken = _jwtService.GenerateAccessToken(storedToken.User);
            
            // 4. Refresh Token Rotation (선택적)
            storedToken.IsRevoked = true;
            var newRefreshToken = _jwtService.GenerateRefreshToken(storedToken.User);
            
            _context.RefreshTokens.Add(new RefreshToken
            {
                Token = newRefreshToken,
                UserId = storedToken.UserId,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow,
                DeviceInfo = Request.Headers["User-Agent"].ToString()
            });
            
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Token refreshed for user {storedToken.User.Email}");
            
            return Ok(new TokenResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresIn = 1800,
                UserEmail = storedToken.User.Email
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token refresh error");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
    
    [HttpPost("logout")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        if (!string.IsNullOrEmpty(userId))
        {
            // 모든 Refresh Token 무효화
            var tokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == int.Parse(userId) && !rt.IsRevoked)
                .ToListAsync();
            
            foreach (var token in tokens)
            {
                token.IsRevoked = true;
            }
            
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"User {userId} logged out");
        }
        
        return Ok(new { message = "Logged out successfully" });
    }
    
    [HttpGet("validate")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public IActionResult Validate()
    {
        return Ok(new 
        { 
            authenticated = true, 
            email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value,
            name = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
        });
    }
}
