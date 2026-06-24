using FridgeManager.Api.Models.Identity;

namespace FridgeManager.Api.Services;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) GenerateToken(ApplicationUser user);
}
