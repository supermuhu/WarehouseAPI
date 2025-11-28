using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseAPI.Helpers;
using WarehouseAPI.ModelView.Inbound;
using WarehouseAPI.Services.Inbound;

namespace WarehouseAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class InboundController : ControllerBase
    {
        private readonly IInboundService _inboundService;

        public InboundController(IInboundService inboundService)
        {
            _inboundService = inboundService;
        }

        /// <summary>
        /// Tạo yêu cầu gửi hàng vào kho
        /// Customer chọn pallet, product, nhập thông tin: ngày sản xuất, ngày hết hạn, đơn giá, thành tiền
        /// </summary>
        /// <param name="request">Thông tin yêu cầu gửi hàng</param>
        /// <returns>Thông tin phiếu nhập kho vừa tạo</returns>
        [HttpPost("create-request")]
        public IActionResult CreateInboundRequest([FromBody] CreateInboundRequestRequest request)
            {
                var accountId = Utils.GetCurrentAccountId(User);
                var role = Utils.GetCurrentRole(User);

            var result = _inboundService.CreateInboundRequest(accountId, role, request);
            if (result.StatusCode == 201)
            {
                    return StatusCode(201, result);
                }
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Lấy dữ liệu phục vụ màn hình duyệt yêu cầu (warehouse_owner)
        /// Bao gồm thông tin pallet, product, kích thước khối hàng và kích thước 1 đơn vị sản phẩm
        /// </summary>
        /// <param name="receiptId">ID phiếu inbound</param>
        [HttpGet("{receiptId}/approval-view")]
        public IActionResult GetInboundApprovalView(int receiptId)
            {
                var accountId = Utils.GetCurrentAccountId(User);
                var role = Utils.GetCurrentRole(User);

            var result = _inboundService.GetInboundApprovalView(receiptId, accountId, role);
            if (result.StatusCode == 200)
            {
                return Ok(result);
            }
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Tính toán layout tối ưu cho các pallet của một phiếu inbound (KHÔNG ghi vào DB).
        /// Dùng cho bước xem trước / gợi ý trước khi nhấn duyệt.
        /// Cho phép truyền PreferredLayouts (thứ tự ưu tiên từ viewer 3D).
        /// </summary>
        [HttpPost("{receiptId}/optimize-layout")]
        public IActionResult OptimizeInboundLayout(int receiptId, [FromBody] ApproveInboundLayoutRequest? request)
            {
                var accountId = Utils.GetCurrentAccountId(User);
                var role = Utils.GetCurrentRole(User);

            var result = _inboundService.OptimizeInboundLayout(receiptId, accountId, role, request);
            if (result.StatusCode == 200)
            {
                return Ok(result);
            }
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Duyệt yêu cầu inbound: sắp xếp pallet vào các khu vực của customer và tạo ItemAllocation
        /// Sau khi duyệt, yêu cầu sẽ chuyển sang trạng thái completed và xuất hiện trong kho 3D.
        /// Có thể truyền thêm PreferredLayouts để ưu tiên thứ tự xếp pallet (từ viewer 3D).
        /// </summary>
        /// <param name="receiptId">ID phiếu inbound</param>
        /// <param name="request">Tuỳ chọn: ưu tiên layout cho từng pallet</param>
        [HttpPost("{receiptId}/approve")]
        public IActionResult ApproveInboundRequest(int receiptId, [FromBody] ApproveInboundLayoutRequest? request)
            {
                var accountId = Utils.GetCurrentAccountId(User);
                var role = Utils.GetCurrentRole(User);

            var result = _inboundService.ApproveInboundRequest(receiptId, accountId, role, request);
            if (result.StatusCode == 200)
            {
                return Ok(result);
            }
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Lấy danh sách các yêu cầu gửi hàng vào kho
        /// Customer: chỉ thấy yêu cầu của mình
        /// Warehouse Owner/Admin: thấy tất cả hoặc theo warehouse
        /// </summary>
        /// <param name="warehouseId">Lọc theo warehouse (tùy chọn)</param>
        /// <param name="status">Lọc theo status (tùy chọn: pending, completed, cancelled)</param>
        /// <returns>Danh sách inbound requests</returns>
        [HttpGet("list")]
        public IActionResult GetInboundRequests([FromQuery] int? warehouseId = null, [FromQuery] string? status = null)
            {
                var accountId = Utils.GetCurrentAccountId(User);
                var role = Utils.GetCurrentRole(User);

            var result = _inboundService.GetInboundRequests(accountId, role, warehouseId, status);
            if (result.StatusCode == 200)
            {
                return Ok(result);
            }
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Lấy chi tiết một yêu cầu gửi hàng
        /// </summary>
        /// <param name="receiptId">ID của receipt</param>
        /// <returns>Chi tiết inbound request</returns>
        [HttpGet("{receiptId}")]
        public IActionResult GetInboundRequestDetail(int receiptId)
            {
                var accountId = Utils.GetCurrentAccountId(User);
                var role = Utils.GetCurrentRole(User);

            var result = _inboundService.GetInboundRequestDetail(receiptId, accountId, role);
            if (result.StatusCode == 200)
            {
                return Ok(result);
            }
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Cập nhật trạng thái của yêu cầu gửi hàng
        /// </summary>
        /// <param name="receiptId">ID của receipt</param>
        /// <param name="request">Thông tin cập nhật status</param>
        /// <returns>Kết quả cập nhật</returns>
        [HttpPut("{receiptId}/status")]
        public IActionResult UpdateInboundRequestStatus(int receiptId, [FromBody] UpdateInboundRequestStatusRequest request)
            {
                var accountId = Utils.GetCurrentAccountId(User);
                var role = Utils.GetCurrentRole(User);

            var result = _inboundService.UpdateInboundRequestStatus(receiptId, accountId, role, request);
            if (result.StatusCode == 200)
            {
                return Ok(result);
            }
            return StatusCode(result.StatusCode, result);
        }
    }
}
