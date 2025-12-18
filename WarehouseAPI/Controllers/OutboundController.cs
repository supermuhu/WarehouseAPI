using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseAPI.Helpers;
using WarehouseAPI.ModelView.Outbound;
using WarehouseAPI.Services.Outbound;

namespace WarehouseAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OutboundController : ControllerBase
    {
        private readonly IOutboundService _outboundService;

        public OutboundController(IOutboundService outboundService)
        {
            _outboundService = outboundService;
        }

        /// <summary>
        /// Lấy danh sách pallet / hàng hóa khả dụng để tạo yêu cầu xuất kho.
        /// Lọc theo kho và (tùy chọn) theo customer.
        /// </summary>
        [HttpGet("available-pallets")]
        public IActionResult GetAvailablePallets([FromQuery] int warehouseId, [FromQuery] int? customerId = null)
        {
            var accountId = Utils.GetCurrentAccountId(User);
            var role = Utils.GetCurrentRole(User);

            var result = _outboundService.GetAvailablePallets(accountId, role, warehouseId, customerId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Tạo yêu cầu xuất kho cho khách hàng trong một kho.
        /// </summary>
        [HttpPost("create-request")]
        public IActionResult CreateOutboundRequest([FromBody] CreateOutboundRequestRequest request)
        {
            var accountId = Utils.GetCurrentAccountId(User);
            var role = Utils.GetCurrentRole(User);

            var result = _outboundService.CreateOutboundRequest(accountId, role, request);
            if (result.StatusCode == 201)
            {
                return StatusCode(201, result);
            }

            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Lấy danh sách các yêu cầu xuất kho.
        /// Customer: chỉ thấy yêu cầu của mình.
        /// Warehouse Owner: thấy yêu cầu xuất từ các kho của mình.
        /// Admin: thấy tất cả.
        /// </summary>
        [HttpGet("list")]
        public IActionResult GetOutboundRequests([FromQuery] int? warehouseId = null)
        {
            var accountId = Utils.GetCurrentAccountId(User);
            var role = Utils.GetCurrentRole(User);

            var result = _outboundService.GetOutboundRequests(accountId, role, warehouseId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Danh sách các phiếu xuất đã duyệt (status = completed) nhưng chưa lấy xong hàng
        /// dựa trên bảng outbound_pallet_picks (chưa pick đủ tất cả pallet liên quan).
        /// </summary>
        [HttpGet("picking-requests")]
        public IActionResult GetOutboundPickingRequests([FromQuery] int? warehouseId)
        {
            var accountId = Utils.GetCurrentAccountId(User);
            var role = Utils.GetCurrentRole(User);

            var result = _outboundService.GetOutboundPickingRequests(accountId, role, warehouseId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Lấy chi tiết một yêu cầu xuất kho.
        /// Customer chỉ xem được phiếu của chính mình.
        /// </summary>
        [HttpGet("{receiptId}")]
        public IActionResult GetOutboundRequestDetail(int receiptId)
        {
            var accountId = Utils.GetCurrentAccountId(User);
            var role = Utils.GetCurrentRole(User);

            var result = _outboundService.GetOutboundRequestDetail(receiptId, accountId, role);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Cập nhật trạng thái yêu cầu xuất kho.
        /// Customer chỉ được phép tự hủy (cancelled) phiếu của mình.
        /// </summary>
        [HttpPut("{receiptId}/status")]
        public IActionResult UpdateOutboundRequestStatus(int receiptId, [FromBody] UpdateOutboundRequestStatusRequest request)
        {
            var accountId = Utils.GetCurrentAccountId(User);
            var role = Utils.GetCurrentRole(User);

            var result = _outboundService.UpdateOutboundRequestStatus(receiptId, accountId, role, request);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Lấy danh sách pallet đã được đánh dấu "Đã lấy" cho một phiếu xuất.
        /// </summary>
        [HttpGet("{receiptId}/pallet-picks")]
        public IActionResult GetOutboundPalletPicks(int receiptId)
        {
            var accountId = Utils.GetCurrentAccountId(User);
            var role = Utils.GetCurrentRole(User);

            var result = _outboundService.GetOutboundPalletPicks(receiptId, accountId, role);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Đánh dấu một pallet trong phiếu xuất là đã được lấy.
        /// Chỉ admin và warehouse_owner của kho được phép thực hiện.
        /// </summary>
        [HttpPost("{receiptId}/pallet-picks")]
        public IActionResult MarkPalletPicked(int receiptId, [FromBody] OutboundPalletPickRequest request)
        {
            var accountId = Utils.GetCurrentAccountId(User);
            var role = Utils.GetCurrentRole(User);

            var result = _outboundService.MarkPalletPicked(receiptId, accountId, role, request);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Lấy dữ liệu phục vụ in phiếu xuất kho.
        /// </summary>
        /// <param name="receiptId">ID phiếu outbound</param>
        [HttpGet("{receiptId}/print-data")]
        public IActionResult GetOutboundReceiptPrintData(int receiptId)
        {
            var accountId = Utils.GetCurrentAccountId(User);
            var role = Utils.GetCurrentRole(User);

            var result = _outboundService.GetOutboundReceiptPrintData(receiptId, accountId, role);
            if (result.StatusCode == 200)
            {
                return Ok(result);
            }

            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Xuất phiếu xuất kho ra file PDF.
        /// </summary>
        /// <param name="receiptId">ID phiếu outbound</param>
        [HttpGet("{receiptId}/export-pdf")]
        public IActionResult ExportOutboundReceiptPdf(int receiptId)
        {
            var accountId = Utils.GetCurrentAccountId(User);
            var role = Utils.GetCurrentRole(User);

            var printDataResponse = _outboundService.GetOutboundReceiptPrintData(receiptId, accountId, role);
            if (printDataResponse.StatusCode != 200 || !printDataResponse.Success || printDataResponse.Data == null)
            {
                return StatusCode(printDataResponse.StatusCode, printDataResponse);
            }

            var pdfBytes = OutboundReceiptPdfGenerator.Generate(printDataResponse.Data);
            var fileName = $"{printDataResponse.Data.ReceiptNumber}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }
    }
}
