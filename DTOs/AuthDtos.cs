namespace FridgeManager.Api.DTOs;

public record RegisterDto(
    string FirstName,
    string LastName,
    string Email,
    string Password
);

public record LoginDto(
    string Email,
    string Password
);

public record AuthResponseDto(
    string AccessToken,
    string Email,
    string FirstName,
    string LastName,
    DateTime ExpiresAt
);
