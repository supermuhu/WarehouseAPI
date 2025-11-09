using Microsoft.AspNetCore.Mvc;
using WarehouseAPI.ModelView.Auth;
using WarehouseAPI.Services.Auth;

namespace WarehouseAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService authService;

        public AuthController(IAuthService authService)
        {
            this.authService = authService;
        }

        [HttpPost("login")]
        public IActionResult Login(AuthSignInModel model)
        {
            var result = authService.Login(model);
            if (result.StatusCode == 200)
            {
                return Ok(result);
            }
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            var result = authService.Logout();
            if (result.StatusCode == 200)
            {
                return Ok(result);
            }
            return StatusCode(result.StatusCode, result);
        }
    }
}
