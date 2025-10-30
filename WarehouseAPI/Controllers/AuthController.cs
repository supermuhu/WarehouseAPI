using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WarehouseAPI.Helpers;
using WarehouseAPI.ModelView.Auth;
using WarehouseAPI.ModelView.Token;
using WarehouseAPI.ModelView.User;
using WarehouseAPI.Services.Auth;
using WarehouseAPI.Services.User;

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
            if (result != null)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }


        [HttpPost("refreshToken")]
        public IActionResult RefreshToken(RefreshTokenModel model)
        {
            var result = authService.RefreshToken(model);
            if (result != null)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }
        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout(AuthEmailModel model)
        {
            var result = authService.Logout(model);
            if (result != null)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }
        [HttpPost("signup")]
        public IActionResult SignUp(AuthSignUpModel model)
        {
            var result = authService.SignUp(model);
            if (result != null)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }
        [HttpPost("verifySignUpOTP")]
        public IActionResult VerifyOTP(AuthVerifyOTPModel model)
        {
            var result = authService.VerifySignUpOTP(model);
            if (result != null)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }
        [HttpPost("verifyForgotPasswordOTP")]
        public IActionResult VerifyForgotPasswordOTP(AuthVerifyOTPModel model)
        {
            var result = authService.VerifyForgotPasswordOTP(model);
            if (result != null)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }
        [HttpPost("resendOTP")]
        public IActionResult ResendOTP(AuthEmailModel model)
        {
            var result = authService.ResendOTP(model);
            if (result != null)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }
        [HttpPost("forgotPassword")]
        [Authorize]
        public IActionResult ForgotPassword(AuthForgotPasswordModel model)
        {
            var result = authService.ForgotPassword(model);
            if (result != null)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [HttpPut("profile")]
        [Authorize]
        public IActionResult Update([FromBody] UserUpdateModel model)
        {
            var currentUserId = Utils.GetCurrentUserId(User);
            if (currentUserId == null)
            {
                return Unauthorized(new { message = "Unable to identify current user from token" });
            }
            var result = authService.Update(currentUserId.Value, model);
            if (result != null) return Ok(result);
            return BadRequest(result);
        }
    }
}
