using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using visitors_mangement_system.Models;

namespace visitors_mangement_system.Services
{
    public class UserService
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public UserService(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("DefaultConnection not configured");
        }

        public (bool Success, string Token, string UserId) Login(LoginRequest login)
        {
            try
            {

                string query = "SELECT COUNT(*) FROM users WHERE userid=@userid AND password_hash=@password";

                using SqlConnection con = new SqlConnection(_connectionString);
                using SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@userid", login.UserId);
                cmd.Parameters.AddWithValue("@password", login.Password);

                con.Open();
                int count = (int)cmd.ExecuteScalar();
                con.Close();

                if (count <= 0)
                {
                    return (false, "", "");
                }

                var claims = new[]
                {
                new Claim(ClaimTypes.NameIdentifier, login.UserId)
            };

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured")));

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
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                return (false, string.Empty, string.Empty);
            }
        }
    }
}