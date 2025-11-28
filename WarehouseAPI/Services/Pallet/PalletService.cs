using Microsoft.EntityFrameworkCore;
using WarehouseAPI.Data;
using WarehouseAPI.ModelView.Common;
using WarehouseAPI.ModelView.Pallet;

namespace WarehouseAPI.Services.Pallet
{
    public class PalletService : IPalletService
    {
        private readonly WarehouseApiContext _context;

        public PalletService(WarehouseApiContext context)
        {
            _context = context;
        }

        public ApiResponse<List<PalletTemplateViewModel>> GetPalletTemplates()
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

                return ApiResponse<List<PalletTemplateViewModel>>.Ok(
                    templates,
                    "Lấy danh sách pallet templates thành công"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<List<PalletTemplateViewModel>>.Fail(
                    $"Lỗi khi lấy danh sách pallet templates: {ex.Message}",
                    "INTERNAL_ERROR",
                    null,
                    500
                );
            }
        }

        public ApiResponse<PalletViewModel> CreatePallet(CreatePalletRequest request)
        {
            try
            {
                // Kiểm tra barcode đã tồn tại chưa
                var existingPallet = _context.Pallets
                    .FirstOrDefault(p => p.Barcode == request.Barcode);

                if (existingPallet != null)
                {
                    return ApiResponse<PalletViewModel>.Fail(
                        "Barcode đã tồn tại trong hệ thống",
                        "DUPLICATE_BARCODE",
                        null,
                        400
                    );
                }

                // Tạo pallet mới
                var pallet = new WarehouseAPI.Data.Pallet
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

                return ApiResponse<PalletViewModel>.Ok(
                    palletViewModel,
                    "Tạo pallet thành công",
                    201
                );
            }
            catch (DbUpdateException ex)
            {
                return ApiResponse<PalletViewModel>.Fail(
                    $"Lỗi khi tạo pallet: {ex.InnerException?.Message ?? ex.Message}",
                    "DATABASE_ERROR",
                    null,
                    500
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<PalletViewModel>.Fail(
                    $"Lỗi khi tạo pallet: {ex.Message}",
                    "INTERNAL_ERROR",
                    null,
                    500
                );
            }
        }

        public ApiResponse<PalletViewModel> CreatePalletFromTemplate(int templateId, CreatePalletFromTemplateRequest request)
        {
            try
            {
                // Kiểm tra template có tồn tại và đang hoạt động không
                var template = _context.PalletTemplates
                    .FirstOrDefault(t => t.TemplateId == templateId && (t.IsActive == true));

                if (template == null)
                {
                    return ApiResponse<PalletViewModel>.Fail(
                        "Template không tồn tại hoặc không hoạt động",
                        "TEMPLATE_NOT_FOUND",
                        null,
                        404
                    );
                }

                // Kiểm tra barcode đã tồn tại chưa
                var existingPallet = _context.Pallets
                    .FirstOrDefault(p => p.Barcode == request.Barcode);

                if (existingPallet != null)
                {
                    return ApiResponse<PalletViewModel>.Fail(
                        "Barcode đã tồn tại trong hệ thống",
                        "DUPLICATE_BARCODE",
                        null,
                        400
                    );
                }

                // Tạo pallet từ template
                var pallet = new WarehouseAPI.Data.Pallet
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

                return ApiResponse<PalletViewModel>.Ok(
                    palletViewModel,
                    "Tạo pallet từ template thành công",
                    201
                );
            }
            catch (DbUpdateException ex)
            {
                return ApiResponse<PalletViewModel>.Fail(
                    $"Lỗi khi tạo pallet: {ex.InnerException?.Message ?? ex.Message}",
                    "DATABASE_ERROR",
                    null,
                    500
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<PalletViewModel>.Fail(
                    $"Lỗi khi tạo pallet: {ex.Message}",
                    "INTERNAL_ERROR",
                    null,
                    500
                );
            }
        }
    }
}

