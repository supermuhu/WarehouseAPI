using WarehouseAPI.Helpers;
using WarehouseAPI.ModelView.Auth;

namespace WarehouseAPI.Services.Auth
{
    public interface IAuthService
    {
        ApiResponse Login(AuthSignInModel user);
        ApiResponse Logout();
    }
}
