using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseAPI.Helpers;
using WarehouseAPI.Services.Warehouse;

namespace WarehouseAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WarehouseController : ControllerBase
    {
        private readonly IWarehouseService warehouseService;

        public WarehouseController(IWarehouseService warehouseService)
        {
            this.warehouseService = warehouseService;
        }

        /// <summary>
        /// Lấy dữ liệu đầy đủ của kho để render 3D
        /// Warehouse owner: Nhìn toàn bộ kho
        /// Customer: Chỉ nhìn zones của họ
        /// </summary>
        /// <param name="warehouseId">ID của kho</param>
        /// <returns>Dữ liệu kho bao gồm zones, racks, shelves, pallets, items</returns>
        [HttpGet("{warehouseId}/3d-data")]
        public IActionResult GetWarehouse3DData(int warehouseId)
        {
            var accountId = Utils.GetCurrentAccountId(User);
            var role = Utils.GetCurrentRole(User);

            if (accountId == null)
            {
                return Unauthorized(new { message = "Token không hợp lệ" });
            }

            var result = warehouseService.GetWarehouse3DData(warehouseId, accountId.Value, role);
            if (result.StatusCode == 200)
            {
                return Ok(result);
            }
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Lấy danh sách tất cả kho đang hoạt động
        /// </summary>
        /// <returns>Danh sách kho</returns>
        [HttpGet("all")]
        public IActionResult GetAllWarehouses()
        {
            var result = warehouseService.GetAllWarehouses();
            if (result.StatusCode == 200)
            {
                return Ok(result);
            }
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Lấy danh sách kho theo chủ kho
        /// </summary>
        /// <param name="ownerId">ID của chủ kho</param>
        /// <returns>Danh sách kho của chủ kho</returns>
        [HttpGet("owner/{ownerId}")]
        public IActionResult GetWarehousesByOwner(int ownerId)
        {
            var result = warehouseService.GetWarehousesByOwner(ownerId);
            if (result.StatusCode == 200)
            {
                return Ok(result);
            }
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Lấy danh sách kho mà customer đã thuê (có zone được gán cho customer)
        /// </summary>
        /// <param name="customerId">ID của customer</param>
        /// <returns>Danh sách kho đã thuê</returns>
        [HttpGet("customer/{customerId}")]
        public IActionResult GetWarehousesByCustomer(int customerId)
        {
            var accountId = Utils.GetCurrentAccountId(User);
            var role = Utils.GetCurrentRole(User);

            // Kiểm tra quyền: customer chỉ có thể xem kho của chính mình
            if (role == "customer" && accountId != customerId)
            {
                return Unauthorized(new { message = "Bạn không có quyền xem kho của customer khác" });
            }

            var result = warehouseService.GetWarehousesByCustomer(customerId);
            if (result.StatusCode == 200)
            {
                return Ok(result);
            }
            return StatusCode(result.StatusCode, result);
        }
    }
}
