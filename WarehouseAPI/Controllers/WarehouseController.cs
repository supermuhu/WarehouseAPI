using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseAPI.Helpers;
using WarehouseAPI.Services.Warehouse;
using WarehouseAPI.ViewModel.Warehouse;

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
        /// <param name="noCache">Bỏ qua cache để lấy dữ liệu mới nhất</param>
        /// <returns>Dữ liệu kho bao gồm zones, racks, shelves, pallets, items</returns>
        [HttpGet("{warehouseId}/3d-data")]
        public IActionResult GetWarehouse3DData(int warehouseId, [FromQuery] bool noCache = false)
        {
            var accountId = Utils.GetCurrentAccountId(User);
            var role = Utils.GetCurrentRole(User);

            if (accountId == null)
            {
                return Unauthorized(new { message = "Token không hợp lệ" });
            }

            var result = warehouseService.GetWarehouse3DData(warehouseId, accountId.Value, role, noCache);
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
        /// Bật / tắt trạng thái cho thuê của kho
        /// </summary>
        [HttpPut("{warehouseId}/rent-status")]
        public IActionResult SetWarehouseRentStatus(int warehouseId, [FromQuery] bool isRentable)
        {
            var accountId = Utils.GetCurrentAccountId(User);
            var role = Utils.GetCurrentRole(User);

            if (accountId == null)
            {
                return Unauthorized(new { message = "Token không hợp lệ" });
            }

            var result = warehouseService.SetWarehouseRentable(warehouseId, isRentable, accountId.Value, role);
            if (result.StatusCode == 200)
            {
                return Ok(result);
            }
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost]
        public IActionResult CreateWarehouse([FromBody] CreateWarehouseModel model)
        {
            var accountId = Utils.GetCurrentAccountId(User);
            var role = Utils.GetCurrentRole(User);

            if (accountId == null)
            {
                return Unauthorized(new { message = "Token không hợp lệ" });
            }

            var result = warehouseService.CreateWarehouse(accountId.Value, role, model);
            if (result.StatusCode == 201 || result.StatusCode == 200)
            {
                return StatusCode(result.StatusCode, result);
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

        /// <summary>
        /// Lấy danh sách kệ trong một khu vực (zone)
        /// </summary>
        [HttpGet("zones/{zoneId}/racks")]
        public IActionResult GetZoneRacks(int zoneId)
        {
            var accountId = Utils.GetCurrentAccountId(User);
            var role = Utils.GetCurrentRole(User);

            if (accountId == null)
            {
                return Unauthorized(new { message = "Token không hợp lệ" });
            }

            var result = warehouseService.GetZoneRacks(zoneId, accountId.Value, role);
            if (result.StatusCode == 200)
            {
                return Ok(result);
            }
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Tạo mới kệ trong một khu vực
        /// </summary>
        [HttpPost("zones/{zoneId}/racks")]
        public IActionResult CreateRack(int zoneId, [FromBody] CreateRackRequest request)
        {
            var accountId = Utils.GetCurrentAccountId(User);
            var role = Utils.GetCurrentRole(User);

            if (accountId == null)
            {
                return Unauthorized(new { message = "Token không hợp lệ" });
            }

            var result = warehouseService.CreateRack(zoneId, accountId.Value, role, request);
            if (result.StatusCode == 201 || result.StatusCode == 200)
            {
                return StatusCode(result.StatusCode, result);
            }
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Cập nhật thông tin/vị trí một kệ trong khu vực
        /// </summary>
        [HttpPut("zones/{zoneId}/racks/{rackId}")]
        public IActionResult UpdateRack(int zoneId, int rackId, [FromBody] UpdateRackRequest request)
        {
            var accountId = Utils.GetCurrentAccountId(User);
            var role = Utils.GetCurrentRole(User);

            if (accountId == null)
            {
                return Unauthorized(new { message = "Token không hợp lệ" });
            }

            var result = warehouseService.UpdateRack(zoneId, rackId, accountId.Value, role, request);
            if (result.StatusCode == 200)
            {
                return Ok(result);
            }
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Cập nhật hàng loạt vị trí kệ trong khu vực (phù hợp drag & drop)
        /// </summary>
        [HttpPut("zones/{zoneId}/racks/positions")]
        public IActionResult BulkUpdateRackPositions(int zoneId, [FromBody] BulkUpdateRackPositionsRequest request)
        {
            var accountId = Utils.GetCurrentAccountId(User);
            var role = Utils.GetCurrentRole(User);

            if (accountId == null)
            {
                return Unauthorized(new { message = "Token không hợp lệ" });
            }

            var result = warehouseService.BulkUpdateRackPositions(zoneId, accountId.Value, role, request);
            if (result.StatusCode == 200)
            {
                return Ok(result);
            }
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Xóa một kệ trong khu vực
        /// </summary>
        [HttpDelete("zones/{zoneId}/racks/{rackId}")]
        public IActionResult DeleteRack(int zoneId, int rackId)
        {
            var accountId = Utils.GetCurrentAccountId(User);
            var role = Utils.GetCurrentRole(User);

            if (accountId == null)
            {
                return Unauthorized(new { message = "Token không hợp lệ" });
            }

            var result = warehouseService.DeleteRack(zoneId, rackId, accountId.Value, role);
            if (result.StatusCode == 200)
            {
                return Ok(result);
            }
            return StatusCode(result.StatusCode, result);
        }
    }
}
