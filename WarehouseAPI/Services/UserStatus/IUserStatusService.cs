using WarehouseAPI.Helpers;
using WarehouseAPI.ModelView.UserStatus;

namespace WarehouseAPI.Services.UserStatus
{
    public interface IUserStatusService
    {
        ApiResponse GetAll();
        ApiResponse GetById(int id);
        ApiResponse Create(UserStatusCreateModel model);
        ApiResponse Update(int id, UserStatusUpdateModel model);
        ApiResponse Delete(int id);
        ApiResponse GetActiveStatuses();
    }
}
