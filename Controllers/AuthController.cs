using Asp.Versioning;
using FridgeManager.Api.Common;
using FridgeManager.Api.DTOs;
using FridgeManager.Api.Models.Identity;
using FridgeManager.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FridgeManager.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    ITokenService tokenService,
    IEmailService emailService,
    IDateTimeProvider dateTime,
    ILogger<AuthController> logger) : ApiController
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto, CancellationToken ct)
    {
        var existing = await userManager.FindByEmailAsync(dto.Email);
        if (existing is not null)
            return Problem(statusCode: 409, title: "Email is already registered.");

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            CreatedAt = dateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(error.Code, error.Description);
            return ValidationProblem(ModelState);
        }

        // generate email confirmation token and build the verify link
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = Uri.EscapeDataString(token);
        var verifyLink = $"{Request.Scheme}://{Request.Host}/api/v1/auth/verify-email?userId={user.Id}&token={encodedToken}";

        await emailService.SendVerificationEmailAsync(user.Email!, user.FirstName, verifyLink, ct);
        logger.LogInformation("User {Email} registered. Verification email sent.", user.Email);

        return Ok(new { message = "Registration successful. Please check your email to verify your account." });
    }

    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromQuery] string userId, [FromQuery] string token)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound("User not found.");

        var decodedToken = Uri.UnescapeDataString(token);
        var result = await userManager.ConfirmEmailAsync(user, decodedToken);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogWarning("Email verification failed for {UserId}: {Errors}", userId, errors);
            return BadRequest($"Email verification failed: {errors}");
        }

        logger.LogInformation("Email verified for user {Email}", user.Email);
        return Ok(new { message = "Email verified successfully. You can now log in." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await userManager.FindByEmailAsync(dto.Email);
        if (user is null) return Unauthorized("Invalid email or password.");

        if (!await userManager.IsEmailConfirmedAsync(user))
            return Unauthorized("Please verify your email before logging in.");

        var isPasswordValid = await userManager.CheckPasswordAsync(user, dto.Password);
        if (!isPasswordValid) return Unauthorized("Invalid email or password.");

        var (token, expiresAt) = tokenService.GenerateToken(user);
        logger.LogInformation("User {Email} logged in", user.Email);

        return Ok(new AuthResponseDto(token, user.Email!, user.FirstName, user.LastName, expiresAt));
    }

    // Manual test endpoint — resend verification email
    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification([FromBody] string email, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null) return Ok(new { message = "If that email exists, a verification link was sent." });
        if (await userManager.IsEmailConfirmedAsync(user))
            return BadRequest("Email is already verified.");

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = Uri.EscapeDataString(token);
        var verifyLink = $"{Request.Scheme}://{Request.Host}/api/v1/auth/verify-email?userId={user.Id}&token={encodedToken}";

        await emailService.SendVerificationEmailAsync(user.Email!, user.FirstName, verifyLink, ct);
        return Ok(new { message = "If that email exists, a verification link was sent." });
    }
}
