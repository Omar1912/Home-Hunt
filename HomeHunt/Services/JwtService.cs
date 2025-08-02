using HomeHunt.Models.Entities;
using HomeHunt.Services.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace HomeHunt.Services
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _config;
        public JwtService(IConfiguration config)
        {
            _config = config;
        }
        public string GenerateJwtToken(UserEntity user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email,(String) user.Email),
                new Claim(ClaimTypes.Name, user.FirstName + " " + user.LastName)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JwtConfig:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["JwtConfig:Issuer"],
                audience: _config["JwtConfig:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: creds
            );


            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        public string GenerateEmailVerificationToken(string email, string code)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Email, email),
                new Claim("verification_code", code),
                new Claim("purpose", "email_verification")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JwtConfig:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken
                (
                    issuer: _config["JwtConfig:Issuer"],
                    audience: _config["JwtConfig:Audience"],
                    claims: claims,
                    expires: DateTime.Now.AddMinutes(30),
                    signingCredentials: creds

                );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

