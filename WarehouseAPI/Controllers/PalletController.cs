using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseAPI.ModelView.Pallet;
using WarehouseAPI.Services.Pallet;

namespace WarehouseAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PalletController : ControllerBase
    {
        private readonly IPalletService _palletService;

        public PalletController(IPalletService palletService)
        {
            _palletService = palletService;
        }

        /// <summary>
        /// Lấy danh sách các pallet template có sẵn để người dùng chọn
        /// </summary>
        /// <returns>Danh sách pallet templates đang hoạt động</returns>
        [HttpGet("templates")]
        public IActionResult GetPalletTemplates()
        {
            var result = _palletService.GetPalletTemplates();
            if (result.StatusCode == 200)
            {
                return Ok(result);
            }
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Tạo pallet mới với kích thước và loại tùy chỉnh
        /// </summary>
        /// <param name="request">Thông tin pallet cần tạo</param>
        /// <returns>Thông tin pallet vừa tạo</returns>
        [HttpPost("create")]
        public IActionResult CreatePallet([FromBody] CreatePalletRequest request)
        {
            var result = _palletService.CreatePallet(request);
            if (result.StatusCode == 201)
            {
                return StatusCode(201, result);
            }
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Tạo pallet từ template có sẵn
        /// </summary>
        /// <param name="templateId">ID của template</param>
        /// <param name="barcode">Barcode cho pallet mới</param>
        /// <param name="palletType">Loại pallet (tùy chọn, có thể override từ template)</param>
        /// <returns>Thông tin pallet vừa tạo</returns>
        [HttpPost("create-from-template/{templateId}")]
        public IActionResult CreatePalletFromTemplate(
            int templateId,
            [FromBody] CreatePalletFromTemplateRequest request)
        {
            var result = _palletService.CreatePalletFromTemplate(templateId, request);
            if (result.StatusCode == 201)
            {
                return StatusCode(201, result);
            }
            return StatusCode(result.StatusCode, result);
        }
    }
}

