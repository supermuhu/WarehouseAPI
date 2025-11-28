using WarehouseAPI.Helpers;
using WarehouseAPI.ViewModel.Warehouse;

namespace WarehouseAPI.Services.Warehouse
{
    public interface IWarehouseService
    {
        ApiResponse GetWarehouse3DData(int warehouseId, int accountId, string role);
        ApiResponse GetAllWarehouses();
        ApiResponse GetWarehousesByOwner(int ownerId);
        ApiResponse GetWarehousesByCustomer(int customerId);

        // Rack management by zone
        ApiResponse GetZoneRacks(int zoneId, int accountId, string role);
        ApiResponse CreateRack(int zoneId, int accountId, string role, CreateRackRequest request);
        ApiResponse UpdateRack(int zoneId, int rackId, int accountId, string role, UpdateRackRequest request);
        ApiResponse BulkUpdateRackPositions(int zoneId, int accountId, string role, BulkUpdateRackPositionsRequest request);
        ApiResponse DeleteRack(int zoneId, int rackId, int accountId, string role);
    }
}
