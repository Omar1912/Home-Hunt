using HomeHunt.Models.Entities;

namespace HomeHunt.Services.Interfaces
{
    public interface IJwtService
    {
        string GenerateJwtToken(UserEntity user);
        string GenerateEmailVerificationToken(string email, string code);
    }
}
