using Google.Apis.Auth;
using Mesh.Api.Auth;
using Mesh.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Mesh.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly TokenService _tokens;
    private readonly JwtSettings _jwt;
    private readonly IConfiguration _config;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        TokenService tokens,
        IOptions<JwtSettings> jwtOptions,
        IConfiguration config)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokens = tokens;
        _jwt = jwtOptions.Value;
        _config = config;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));

        return await IssueTokensAsync(user);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Unauthorized(new { message = "Invalid email or password." });

        // CheckPasswordSignInAsync verifies the password and respects Identity's
        // lockout policy, without setting any auth cookie - exactly what we want
        // for an API that only issues JWTs.
        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
            return Unauthorized(new { message = "Invalid email or password." });

        return await IssueTokensAsync(user);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshRequest request)
    {
        var existing = await _tokens.FindActiveRefreshTokenAsync(request.RefreshToken);
        if (existing is null)
            return Unauthorized(new { message = "Invalid or expired refresh token." });

        var user = await _userManager.FindByIdAsync(existing.UserId);
        if (user is null)
            return Unauthorized();

        // Rotate on every use: revoke the presented token, issue a fresh pair.
        await _tokens.RevokeRefreshTokenAsync(existing);
        return await IssueTokensAsync(user);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshRequest request)
    {
        var existing = await _tokens.FindActiveRefreshTokenAsync(request.RefreshToken);
        if (existing is not null)
            await _tokens.RevokeRefreshTokenAsync(existing);

        return NoContent();
    }

    [HttpPost("google")]
    public async Task<IActionResult> GoogleAuth(GoogleAuthRequest request)
    {
        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _config["Google:ClientId"] },
            });
        }
        catch (InvalidJwtException)
        {
            return Unauthorized(new { message = "Invalid Google token." });
        }

        var user = await _userManager.FindByEmailAsync(payload.Email);
        if (user is null)
        {
            // First time we've seen this email - create the account.
            // Google has already verified the email, so EmailConfirmed = true.
            user = new ApplicationUser
            {
                UserName = payload.Email,
                Email = payload.Email,
                EmailConfirmed = true,
                DisplayName = payload.Name ?? payload.Email,
            };
            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
                return BadRequest(createResult.Errors.Select(e => e.Description));
        }

        // Auto-link: record this as one of the user's login methods if not already.
        var logins = await _userManager.GetLoginsAsync(user);
        if (!logins.Any(l => l.LoginProvider == "Google" && l.ProviderKey == payload.Subject))
        {
            await _userManager.AddLoginAsync(user, new UserLoginInfo("Google", payload.Subject, "Google"));
        }

        return await IssueTokensAsync(user);
    }

    private async Task<IActionResult> IssueTokensAsync(ApplicationUser user)
    {
        var accessToken = _tokens.CreateAccessToken(user);
        var refreshToken = await _tokens.CreateRefreshTokenAsync(user.Id);
        return Ok(new AuthResponse(accessToken, refreshToken, DateTime.UtcNow.AddMinutes(_jwt.AccessTokenMinutes)));
    }
}
