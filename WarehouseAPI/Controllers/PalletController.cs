using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseAPI.Data;
using WarehouseAPI.ModelView.Common;
using WarehouseAPI.ModelView.Pallet;

namespace WarehouseAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PalletController : ControllerBase
    {
        private readonly WarehouseApiContext _context;

        public PalletController(WarehouseApiContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy danh sách các pallet template có sẵn để người dùng chọn
        /// </summary>
        /// <returns>Danh sách pallet templates đang hoạt động</returns>
        [HttpGet("templates")]
        public IActionResult GetPalletTemplates()
        {
            try
            {
                var templates = _context.PalletTemplates
                    .Where(t => t.IsActive == true)
                    .OrderBy(t => t.TemplateName)
                    .Select(t => new PalletTemplateViewModel
                    {
                        TemplateId = t.TemplateId,
                        TemplateName = t.TemplateName,
                        PalletType = t.PalletType,
                        Length = t.Length,
                        Width = t.Width,
                        Height = t.Height,
                        MaxWeight = t.MaxWeight,
                        MaxStackHeight = t.MaxStackHeight,
                        Description = t.Description,
                        IsActive = t.IsActive ?? false
                    })
                    .ToList();

                var result = ApiResponse<List<PalletTemplateViewModel>>.Ok(
                    templates,
                    "Lấy danh sách pallet templates thành công"
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                var result = ApiResponse<List<PalletTemplateViewModel>>.Fail(
                    $"Lỗi khi lấy danh sách pallet templates: {ex.Message}",
                    "INTERNAL_ERROR",
                    null,
                    500
                );
                return StatusCode(500, result);
            }
        }

        /// <summary>
        /// Tạo pallet mới với kích thước và loại tùy chỉnh
        /// </summary>
        /// <param name="request">Thông tin pallet cần tạo</param>
        /// <returns>Thông tin pallet vừa tạo</returns>
        [HttpPost("create")]
        public IActionResult CreatePallet([FromBody] CreatePalletRequest request)
        {
            try
            {
                // Kiểm tra barcode đã tồn tại chưa
                var existingPallet = _context.Pallets
                    .FirstOrDefault(p => p.Barcode == request.Barcode);

                if (existingPallet != null)
                {
                    var errorResult = ApiResponse<PalletViewModel>.Fail(
                        "Barcode đã tồn tại trong hệ thống",
                        "DUPLICATE_BARCODE",
                        null,
                        400
                    );
                    return BadRequest(errorResult);
                }

                // Tạo pallet mới
                var pallet = new Pallet
                {
                    Barcode = request.Barcode,
                    Length = request.Length,
                    Width = request.Width,
                    Height = request.Height ?? 0.15m, // Mặc định 0.15m nếu không có
                    MaxWeight = request.MaxWeight ?? 1000m, // Mặc định 1000kg
                    MaxStackHeight = request.MaxStackHeight ?? 1.5m, // Mặc định 1.5m
                    PalletType = request.PalletType,
                    Status = "available",
                    CreatedAt = DateTime.Now
                };

                _context.Pallets.Add(pallet);
                _context.SaveChanges();

                var palletViewModel = new PalletViewModel
                {
                    PalletId = pallet.PalletId,
                    Barcode = pallet.Barcode,
                    Length = pallet.Length,
                    Width = pallet.Width,
                    Height = pallet.Height,
                    MaxWeight = pallet.MaxWeight,
                    MaxStackHeight = pallet.MaxStackHeight,
                    Status = pallet.Status,
                    PalletType = pallet.PalletType,
                    CreatedAt = pallet.CreatedAt
                };

                var result = ApiResponse<PalletViewModel>.Ok(
                    palletViewModel,
                    "Tạo pallet thành công",
                    201
                );

                return StatusCode(201, result);
            }
            catch (DbUpdateException ex)
            {
                var result = ApiResponse<PalletViewModel>.Fail(
                    $"Lỗi khi tạo pallet: {ex.InnerException?.Message ?? ex.Message}",
                    "DATABASE_ERROR",
                    null,
                    500
                );
                return StatusCode(500, result);
            }
            catch (Exception ex)
            {
                var result = ApiResponse<PalletViewModel>.Fail(
                    $"Lỗi khi tạo pallet: {ex.Message}",
                    "INTERNAL_ERROR",
                    null,
                    500
                );
                return StatusCode(500, result);
            }
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
            try
            {
                // Kiểm tra template có tồn tại và đang hoạt động không
                var template = _context.PalletTemplates
                    .FirstOrDefault(t => t.TemplateId == templateId && (t.IsActive == true));

                if (template == null)
                {
                    var errorResult = ApiResponse<PalletViewModel>.Fail(
                        "Template không tồn tại hoặc không hoạt động",
                        "TEMPLATE_NOT_FOUND",
                        null,
                        404
                    );
                    return NotFound(errorResult);
                }

                // Kiểm tra barcode đã tồn tại chưa
                var existingPallet = _context.Pallets
                    .FirstOrDefault(p => p.Barcode == request.Barcode);

                if (existingPallet != null)
                {
                    var errorResult = ApiResponse<PalletViewModel>.Fail(
                        "Barcode đã tồn tại trong hệ thống",
                        "DUPLICATE_BARCODE",
                        null,
                        400
                    );
                    return BadRequest(errorResult);
                }

                // Tạo pallet từ template
                var pallet = new Pallet
                {
                    Barcode = request.Barcode,
                    Length = template.Length,
                    Width = template.Width,
                    Height = template.Height,
                    MaxWeight = template.MaxWeight,
                    MaxStackHeight = template.MaxStackHeight,
                    PalletType = request.PalletType ?? template.PalletType, // Ưu tiên loại từ request
                    Status = "available",
                    CreatedAt = DateTime.Now
                };

                _context.Pallets.Add(pallet);
                _context.SaveChanges();

                var palletViewModel = new PalletViewModel
                {
                    PalletId = pallet.PalletId,
                    Barcode = pallet.Barcode,
                    Length = pallet.Length,
                    Width = pallet.Width,
                    Height = pallet.Height,
                    MaxWeight = pallet.MaxWeight,
                    MaxStackHeight = pallet.MaxStackHeight,
                    Status = pallet.Status,
                    PalletType = pallet.PalletType,
                    CreatedAt = pallet.CreatedAt
                };

                var result = ApiResponse<PalletViewModel>.Ok(
                    palletViewModel,
                    "Tạo pallet từ template thành công",
                    201
                );

                return StatusCode(201, result);
            }
            catch (DbUpdateException ex)
            {
                var result = ApiResponse<PalletViewModel>.Fail(
                    $"Lỗi khi tạo pallet: {ex.InnerException?.Message ?? ex.Message}",
                    "DATABASE_ERROR",
                    null,
                    500
                );
                return StatusCode(500, result);
            }
            catch (Exception ex)
            {
                var result = ApiResponse<PalletViewModel>.Fail(
                    $"Lỗi khi tạo pallet: {ex.Message}",
                    "INTERNAL_ERROR",
                    null,
                    500
                );
                return StatusCode(500, result);
            }
        }
    }
}

