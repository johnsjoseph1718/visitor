using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using visitors_mangement_system.models;
using visitors_mangement_system.Models;
using visitors_mangement_system.BusinessLogic;

namespace visitors_mangement_system.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AuthBusiness _authBusiness;

                public UsersController()
                {
                    _authBusiness = new AuthBusiness();
                }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest login)
        {
            var result = _authBusiness.Login(login);

            if (result.Success)
            {
                return Ok(new CommonResponseModel<object>
                {
                    Success = true,
                    Message = "Login successful",
                    Response = new
                    {
                        UserId = result.UserId,
                        Token = result.Token
                    }
                });
            }

            return BadRequest(new CommonResponseModel<string>
            {
                Success = false,
                Message = "Invalid username or password",
                Response = null
            });
        }
        [Authorize]
        [HttpGet("validate-token")]
        public IActionResult ValidateToken()
        {
            string? userId =
                User.FindFirst(
                    System.Security.Claims.ClaimTypes.NameIdentifier)
                ?.Value;

            return Ok(new
            {
                Success = true,
                UserId = userId,
                Message = "Token is valid"
            });
        }

    }
}

        
