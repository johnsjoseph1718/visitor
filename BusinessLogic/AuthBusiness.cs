using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using visitors_mangement_system.Models;
using visitors_mangement_system.Repositories;

namespace visitors_mangement_system.BusinessLogic
{
    public class AuthBusiness
    {
        private readonly AuthRepository _repo;
        private readonly IConfiguration _configuration;

        public AuthBusiness()
        {
            _repo = new AuthRepository();
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .Build();
            _configuration = config;
        }

        public (bool Success, string Token, string UserId) Login(LoginRequest login)
        {
            var result = _repo.Login(login);
            if (!result.Success)
                return (false, string.Empty, string.Empty);

            var claims = new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, login.UserId) };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? string.Empty));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            string jwtToken = new JwtSecurityTokenHandler().WriteToken(token);

            return (true, jwtToken, login.UserId);
        }
    }
}
