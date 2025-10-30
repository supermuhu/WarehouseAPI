using WarehouseAPI.Helpers;
using WarehouseAPI.ModelView.Auth;
using WarehouseAPI.ModelView.Token;
using WarehouseAPI.ModelView.User;

namespace WarehouseAPI.Services.Auth
{
    public interface IAuthService
    {
        ApiResponse Login(AuthSignInModel user);
        ApiResponse RefreshToken(RefreshTokenModel token);
        ApiResponse Logout(AuthEmailModel model);
        ApiResponse SignUp(AuthSignUpModel model);
        ApiResponse ForgotPassword(AuthForgotPasswordModel model);
        ApiResponse VerifySignUpOTP(AuthVerifyOTPModel model);
        ApiResponse VerifyForgotPasswordOTP(AuthVerifyOTPModel model);
        ApiResponse ResendOTP(AuthEmailModel model);
        ApiResponse Update(int id, UserUpdateModel model);
    }
}
