using Microsoft.Data.SqlClient;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using visitors_mangement_system.Models;

namespace visitors_mangement_system.Repositories
{
    public class AuthRepository
    {
        private readonly string _connectionString;

        public AuthRepository()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .Build();

            _connectionString = config.GetConnectionString("DefaultConnection") ?? string.Empty;
        }

        public (bool Success, string Token, string UserId) Login(LoginRequest login)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_connectionString))
                    return (false, string.Empty, string.Empty);

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

                return (true, "", login.UserId);
            }
            catch (Exception ex)
            {
                return (false, string.Empty, string.Empty);
            }
        }
    }
}
