using WarehouseAPI.Helpers;
using WarehouseAPI.ViewModel.Warehouse;

namespace WarehouseAPI.Services.Warehouse
{
    public interface IWarehouseService
    {
        ApiResponse GetWarehouse3DData(int warehouseId, int accountId, string role);
        ApiResponse GetAllWarehouses();
        ApiResponse GetWarehousesByOwner(int ownerId);
    }
}
