using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QRCoder;
using System.Globalization;
using System.Text.Json;
using System.IO;
using WarehouseAPI.Data;
using WarehouseAPI.ModelView.Common;
using WarehouseAPI.ModelView.Inbound;

namespace WarehouseAPI.Services.Inbound
{
    public class InboundService : IInboundService
    {
        private readonly WarehouseApiContext _context;
        private static bool _questPdfConfigured = false;
        private static readonly object _questPdfLock = new();

        public InboundService(WarehouseApiContext context)
        {
            _context = context;
        }

        private static void EnsureQuestPdfConfigured()
        {
            if (_questPdfConfigured) return;
            lock (_questPdfLock)
            {
                if (_questPdfConfigured) return;
                QuestPDF.Settings.License = LicenseType.Community;
                _questPdfConfigured = true;
            }
        }

        private static IContainer PdfHeaderCell(IContainer container)
        {
            return container
                .Padding(2)
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Darken2)
                .DefaultTextStyle(TextStyle.Default.SemiBold().FontSize(9));
        }

        private static IContainer PdfBodyCell(IContainer container)
        {
            return container
                .Padding(2)
                .DefaultTextStyle(TextStyle.Default.FontSize(9));
        }

        private static decimal GetShelfClearHeight(Rack rack, Shelf shelf)
        {
            var orderedShelves = rack.Shelves
                .OrderBy(s => s.ShelfLevel)
                .ToList();

            var index = orderedShelves.FindIndex(s => s.ShelfId == shelf.ShelfId);
            if (index < 0)
            {
                return rack.Height;
            }

            if (index < orderedShelves.Count - 1)
            {
                var nextShelf = orderedShelves[index + 1];
                return nextShelf.PositionY - shelf.PositionY;
            }

            return rack.Height - shelf.PositionY;
        }

        public ApiResponse<object> CreateInboundRequest(int? accountId, string role, CreateInboundRequestRequest request)
        {
            try
            {
                if (accountId == null)
                {
                    return ApiResponse<object>.Fail(
                        "Token không hợp lệ",
                        "UNAUTHORIZED",
                        null,
                        401
                    );
                }

                // Kiểm tra warehouse có tồn tại không
                var warehouse = _context.Warehouses
                    .FirstOrDefault(w => w.WarehouseId == request.WarehouseId && w.Status == "active");

                if (warehouse == null)
                {
                    return ApiResponse<object>.Fail(
                        "Kho không tồn tại hoặc không hoạt động",
                        "WAREHOUSE_NOT_FOUND",
                        null,
                        404
                    );
                }

                // Xác định customer_id
                int customerId;
                if (role == "customer")
                {
                    customerId = accountId.Value;
                }
                else
                {
                    // Nếu không phải customer, cần có customer_id trong request
                    // Hoặc có thể để customer_id = accountId nếu warehouse_owner tự tạo
                    customerId = accountId.Value; // Tạm thởi dùng accountId
                }

                // Xác định khu vực (zone) nếu FE gửi lên
                WarehouseZone? selectedZone = null;

                if (request.ZoneId.HasValue)
                {
                    selectedZone = _context.WarehouseZones
                        .FirstOrDefault(z => z.ZoneId == request.ZoneId.Value
                                          && z.WarehouseId == request.WarehouseId
                                          && z.CustomerId == customerId);

                    if (selectedZone == null)
                    {
                        return ApiResponse<object>.Fail(
                            "Khu vực được chọn không hợp lệ cho khách hàng trong kho này",
                            "INVALID_ZONE_FOR_CUSTOMER",
                            null,
                            400
                        );
                    }
                }
                else if (role == "customer")
                {
                    // Customer bắt buộc phải chọn rõ khu vực để hệ thống áp dụng rule theo từng zone
                    return ApiResponse<object>.Fail(
                        "Vui lòng chọn khu vực trong kho để tạo yêu cầu nhập",
                        "MISSING_ZONE_FOR_CUSTOMER",
                        null,
                        400
                    );
                }

                // Validate các items
                var palletIds = request.Items.Select(i => i.PalletId).Distinct().ToList();
                var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();

                // Mỗi pallet chỉ được phép gắn một hàng hóa trong một yêu cầu
                var duplicatePallets = request.Items
                    .GroupBy(i => i.PalletId)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                if (duplicatePallets.Any())
                {
                    return ApiResponse<object>.Fail(
                        "Mỗi pallet chỉ được phép gắn một hàng hóa trong một yêu cầu",
                        "DUPLICATE_PALLET_ITEMS",
                        null,
                        400
                    );
                }

                // Kiểm tra pallets có tồn tại và available không
                var pallets = _context.Pallets
                    .Where(p => palletIds.Contains(p.PalletId))
                    .ToList();

                if (pallets.Count != palletIds.Count)
                {
                    return ApiResponse<object>.Fail(
                        "Một hoặc nhiều pallet không tồn tại",
                        "PALLET_NOT_FOUND",
                        null,
                        404
                    );
                }

                // Kiểm tra products có tồn tại và active không
                var products = _context.Products
                    .Where(p => productIds.Contains(p.ProductId) && p.Status == "active")
                    .ToList();

                if (products.Count != productIds.Count)
                {
                    return ApiResponse<object>.Fail(
                        "Một hoặc nhiều product không tồn tại hoặc không hoạt động",
                        "PRODUCT_NOT_FOUND",
                        null,
                        404
                    );
                }

                // Kiểm tra products có thuộc về customer hoặc admin không
                var adminIds = _context.Accounts
                    .Where(a => a.Role == "admin")
                    .Select(a => a.AccountId)
                    .ToList();

                var invalidProducts = products
                    .Where(p => p.CreateUser != customerId && 
                               (p.CreateUser == null || !adminIds.Contains(p.CreateUser.Value)))
                    .ToList();

                if (invalidProducts.Any())
                {
                    return ApiResponse<object>.Fail(
                        "Một hoặc nhiều product không thuộc về bạn hoặc admin",
                        "PRODUCT_ACCESS_DENIED",
                        null,
                        403
                    );
                }

                using var transaction = _context.Database.BeginTransaction();
                try
                {
                    // Tạo receipt number
                    var tempId = Guid.NewGuid().ToString().Replace("-", "");
                    var receiptNumber = $"IN-{DateTime.Now:yyyyMMdd}-{tempId.Substring(0, 8)}";

                    // Tính tổng số items và pallets
                    var totalItems = request.Items.Sum(i => i.Quantity);
                    var totalPallets = palletIds.Count;

                    // Xác định mode xếp (auto/manual)
                    var stackMode = (request.StackMode ?? "auto").ToLower();
                    if (stackMode != "auto" && stackMode != "manual")
                    {
                        stackMode = "auto";
                    }

                    // Tạo inbound receipt
                    string? autoStackTemplate = null;
                    if (string.Equals(stackMode, "auto", StringComparison.OrdinalIgnoreCase))
                    {
                        var tpl = request.AutoStackTemplate?.Trim().ToLower();
                        if (tpl == "straight" || tpl == "brick" || tpl == "cross")
                        {
                            autoStackTemplate = tpl;
                        }
                    }

                    var inboundReceipt = new InboundReceipt
                    {
                        WarehouseId = request.WarehouseId,
                        ZoneId = selectedZone?.ZoneId,
                        CustomerId = customerId,
                        ReceiptNumber = receiptNumber,
                        TotalItems = totalItems,
                        TotalPallets = totalPallets,
                        InboundDate = DateTime.Now,
                        Notes = request.Notes,
                        CreatedBy = accountId.Value,
                        Status = "pending",
                        StackMode = stackMode,
                        AutoStackTemplate = autoStackTemplate
                    };

                    _context.InboundReceipts.Add(inboundReceipt);
                    _context.SaveChanges();

                    // Tạo items và inbound_items
                    var itemCounter = 1;
                    foreach (var itemRequest in request.Items)
                    {
                        var product = products.First(p => p.ProductId == itemRequest.ProductId);
                        var pallet = pallets.First(p => p.PalletId == itemRequest.PalletId);

                        // Kích thước khối hàng trên pallet (stack dimensions)
                        var length = itemRequest.Length ?? product.StandardLength ?? 0.5m;
                        var width = itemRequest.Width ?? product.StandardWidth ?? 0.4m;
                        var height = itemRequest.Height ?? product.StandardHeight ?? 0.3m;

                        // Mỗi InboundItemRequest tương ứng 1 Item (1 khối hàng trên 1 pallet)
                        var qrCode = $"QR-{receiptNumber}-{itemCounter:D4}";

                        // Đảm bảo QR code là duy nhất
                        while (_context.Items.Any(it => it.QrCode == qrCode))
                        {
                            itemCounter++;
                            qrCode = $"QR-{receiptNumber}-{itemCounter:D4}";
                        }

                        // Xác định loại hàng: bao / thùng dựa trên đơn vị
                        var unitLower = product.Unit.ToLower();
                        var categoryLower = product.Category?.ToLower();
                        var itemType = unitLower.Contains("bao") || (categoryLower != null && categoryLower.Contains("bao"))
                            ? "bag"
                            : "box";

                        var item = new Item
                        {
                            QrCode = qrCode,
                            ProductId = itemRequest.ProductId,
                            CustomerId = customerId,
                            ItemName = product.ProductName,
                            ItemType = itemType,
                            Length = length,
                            Width = width,
                            Height = height,
                            Weight = product.StandardWeight,
                            Shape = "rectangle",
                            PriorityLevel = 5,
                            IsHeavy = product.StandardWeight > 20,
                            IsFragile = product.IsFragile,
                            IsNonStackable = product.IsNonStackable,
                            BatchNumber = itemRequest.BatchNumber,
                            ManufacturingDate = DateOnly.FromDateTime(itemRequest.ManufacturingDate),
                            ExpiryDate = itemRequest.ExpiryDate.HasValue
                                ? DateOnly.FromDateTime(itemRequest.ExpiryDate.Value)
                                : null,
                            UnitPrice = itemRequest.UnitPrice,          // Đơn giá 1 đơn vị hàng
                            TotalAmount = itemRequest.TotalAmount,      // Thành tiền cả pallet hàng
                            CreatedAt = DateTime.Now
                        };

                        _context.Items.Add(item);
                        _context.SaveChanges();

                        // Mỗi pallet có 1 inbound_item, Quantity = số đơn vị hàng trên pallet
                        var inboundItem = new InboundItem
                        {
                            ReceiptId = inboundReceipt.ReceiptId,
                            ItemId = item.ItemId,
                            PalletId = itemRequest.PalletId,
                            Quantity = itemRequest.Quantity
                        };

                        _context.InboundItems.Add(inboundItem);
                        itemCounter++;
                    }

                    _context.SaveChanges();
                    transaction.Commit();

                    return ApiResponse<object>.Ok(
                        new
                        {
                            ReceiptId = inboundReceipt.ReceiptId,
                            ReceiptNumber = inboundReceipt.ReceiptNumber,
                            WarehouseId = inboundReceipt.WarehouseId,
                            CustomerId = inboundReceipt.CustomerId,
                            TotalItems = inboundReceipt.TotalItems,
                            TotalPallets = inboundReceipt.TotalPallets,
                            Status = inboundReceipt.Status,
                            InboundDate = inboundReceipt.InboundDate
                        },
                        "Tạo yêu cầu gửi hàng thành công",
                        201
                    );
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (DbUpdateException ex)
            {
                return ApiResponse<object>.Fail(
                    $"Lỗi khi tạo yêu cầu gửi hàng: {ex.InnerException?.Message ?? ex.Message}",
                    "DATABASE_ERROR",
                    null,
                    500
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail(
                    $"Lỗi khi tạo yêu cầu gửi hàng: {ex.Message}",
                    "INTERNAL_ERROR",
                    null,
                    500
                );
            }
        }

        private void TryGenerateInboundReceiptPdf(InboundReceiptPrintViewModel model)
        {
            try
            {
                EnsureQuestPdfConfigured();

                var rootDir = Directory.GetCurrentDirectory();
                var customerDir = Path.Combine(rootDir, "wwwroot", "inbound", model.CustomerId.ToString());
                Directory.CreateDirectory(customerDir);

                var safeReceiptNumber = string.Concat((model.ReceiptNumber ?? string.Empty)
                    .Where(ch => !Path.GetInvalidFileNameChars().Contains(ch)));
                if (string.IsNullOrWhiteSpace(safeReceiptNumber))
                {
                    safeReceiptNumber = $"IN-{model.ReceiptId}";
                }

                var fileName = $"{safeReceiptNumber}.pdf";
                var filePath = Path.Combine(customerDir, fileName);

                var viCulture = new CultureInfo("vi-VN");

                var allItems = model.Pallets.SelectMany(p => p.Items).ToList();
                var totalQuantity = allItems.Sum(i => (decimal)i.Quantity);
                var totalAmount = allItems.Sum(i => i.TotalAmount ?? 0m);

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(30);
                        page.PageColor(Colors.White);

                        page.Content().Column(col =>
                        {
                            col.Spacing(10);

                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Column(left =>
                                {
                                    left.Item().Text(text =>
                                    {
                                        text.Span("Đơn vị: ").Bold().FontSize(10);
                                        text.Span(model.WarehouseName ?? string.Empty).FontSize(10);
                                    });

                                    left.Item().Text(text =>
                                    {
                                        text.Span("Khách hàng: ").Bold().FontSize(10);
                                        text.Span(model.CustomerName ?? string.Empty).FontSize(10);
                                    });
                                });
                            });

                            col.Item().AlignCenter().Text(text =>
                            {
                                text.Span("PHIẾU NHẬP KHO").FontSize(16).Bold();
                            });

                            var date = model.InboundDate ?? DateTime.Now;
                            col.Item().AlignCenter().Text(text =>
                            {
                                text.Span($"Ngày {date:dd} tháng {date:MM} năm {date:yyyy}").FontSize(10);
                            });

                            col.Item().Text(text =>
                            {
                                text.Span("Số phiếu: ").Bold().FontSize(10);
                                text.Span(model.ReceiptNumber ?? string.Empty).FontSize(10);
                            });

                            col.Item().Text(text =>
                            {
                                text.Span("Kho: ").Bold().FontSize(10);
                                var whName = model.WarehouseName ?? string.Empty;
                                text.Span(string.IsNullOrEmpty(whName)
                                    ? $"#{model.WarehouseId}"
                                    : $"{whName} (#{model.WarehouseId})").FontSize(10);
                                if (!string.IsNullOrWhiteSpace(model.ZoneName))
                                {
                                    text.Span($", Khu vực: {model.ZoneName}").FontSize(10);
                                }
                            });

                            if (!string.IsNullOrWhiteSpace(model.Notes))
                            {
                                col.Item().Text(text =>
                                {
                                    text.Span("Ghi chú: ").Bold().FontSize(10);
                                    text.Span(model.Notes ?? string.Empty).FontSize(10);
                                });
                            }

                            col.Item().LineHorizontal(1).LineColor(Colors.Grey.Darken2);

                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(25);   // STT
                                    columns.RelativeColumn(4);    // Tên hàng hóa
                                    columns.RelativeColumn(2);    // Mã số
                                    columns.RelativeColumn(1.5f); // ĐVT
                                    columns.RelativeColumn(2);    // Theo chứng từ
                                    columns.RelativeColumn(2);    // Thực nhập
                                    columns.RelativeColumn(2);    // Đơn giá
                                    columns.RelativeColumn(2);    // Thành tiền
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(PdfHeaderCell).Text("STT");
                                    header.Cell().Element(PdfHeaderCell).Text("Tên hàng hóa");
                                    header.Cell().Element(PdfHeaderCell).Text("Mã số");
                                    header.Cell().Element(PdfHeaderCell).Text("Đơn vị tính");
                                    header.Cell().Element(PdfHeaderCell).Text("Theo chứng từ");
                                    header.Cell().Element(PdfHeaderCell).Text("Thực nhập");
                                    header.Cell().Element(PdfHeaderCell).Text("Đơn giá");
                                    header.Cell().Element(PdfHeaderCell).Text("Thành tiền");
                                });

                                var index = 1;
                                foreach (var pallet in model.Pallets)
                                {
                                    foreach (var item in pallet.Items)
                                    {
                                        var qtyText = item.Quantity.ToString("N0", viCulture);
                                        var unitPriceText = item.UnitPrice.HasValue
                                            ? item.UnitPrice.Value.ToString("#,0.##", viCulture)
                                            : string.Empty;
                                        var amountText = item.TotalAmount.HasValue
                                            ? item.TotalAmount.Value.ToString("#,0.##", viCulture)
                                            : string.Empty;

                                        table.Cell().Element(PdfBodyCell).Text(index.ToString());

                                        table.Cell().Element(PdfBodyCell).Text(text =>
                                        {
                                            var name = item.ProductName ?? item.ItemName ?? string.Empty;
                                            text.Line(name);
                                            if (!string.IsNullOrWhiteSpace(item.BatchNumber))
                                            {
                                                text.Line($"Lô: {item.BatchNumber}");
                                            }
                                            if (!string.IsNullOrWhiteSpace(pallet.PalletBarcode))
                                            {
                                                text.Line($"Pallet: {pallet.PalletBarcode}");
                                            }
                                            if (!string.IsNullOrWhiteSpace(pallet.LocationCode))
                                            {
                                                text.Line($"Vị trí: {pallet.LocationCode}");
                                            }
                                        });

                                        table.Cell().Element(PdfBodyCell).Text(item.ProductCode ?? string.Empty);
                                        table.Cell().Element(PdfBodyCell).Text(item.Unit ?? string.Empty);

                                        // Hiện tại chưa tách riêng số lượng theo chứng từ và thực nhập,
                                        // sử dụng cùng một Quantity cho cả hai cột.
                                        table.Cell().Element(PdfBodyCell).AlignRight().Text(qtyText);
                                        table.Cell().Element(PdfBodyCell).AlignRight().Text(qtyText);

                                        table.Cell().Element(PdfBodyCell).AlignRight().Text(unitPriceText);
                                        table.Cell().Element(PdfBodyCell).AlignRight().Text(amountText);

                                        index++;
                                    }
                                }

                                if (allItems.Any())
                                {
                                    table.Cell().Element(PdfBodyCell).Text(string.Empty);
                                    table.Cell().Element(PdfBodyCell).Text("Cộng");
                                    table.Cell().Element(PdfBodyCell).Text(string.Empty);
                                    table.Cell().Element(PdfBodyCell).Text(string.Empty);
                                    table.Cell().Element(PdfBodyCell).AlignRight()
                                        .Text(totalQuantity.ToString("N0", viCulture));
                                    table.Cell().Element(PdfBodyCell).AlignRight()
                                        .Text(totalQuantity.ToString("N0", viCulture));
                                    table.Cell().Element(PdfBodyCell).Text(string.Empty);
                                    table.Cell().Element(PdfBodyCell).AlignRight()
                                        .Text(totalAmount.ToString("#,0.##", viCulture));
                                }
                            });

                            col.Item().Height(10);

                            col.Item().Row(row =>
                            {
                                row.RelativeItem().AlignCenter().Text(text =>
                                {
                                    text.Span("Người lập phiếu").FontSize(10).SemiBold();
                                });
                                row.RelativeItem().AlignCenter().Text(text =>
                                {
                                    text.Span("Người giao hàng").FontSize(10).SemiBold();
                                });
                                row.RelativeItem().AlignCenter().Text(text =>
                                {
                                    text.Span("Thủ kho").FontSize(10).SemiBold();
                                });
                            });

                            col.Item().Row(row =>
                            {
                                row.RelativeItem().AlignCenter().Text(text =>
                                {
                                    text.Span("(Ký, họ tên)").FontSize(9).Italic();
                                });
                                row.RelativeItem().AlignCenter().Text(text =>
                                {
                                    text.Span("(Ký, họ tên)").FontSize(9).Italic();
                                });
                                row.RelativeItem().AlignCenter().Text(text =>
                                {
                                    text.Span("(Ký, họ tên)").FontSize(9).Italic();
                                });
                            });
                        });
                    });
                });

                document.GeneratePdf(filePath);
            }
            catch
            {
                // Ignore PDF generation errors to avoid breaking approval flow
            }
        }

        public ApiResponse<object> SaveManualStackLayout(int receiptId, int? accountId, string role, ManualStackLayoutRequest request)
        {
            try
            {
                if (accountId == null)
                {
                    return ApiResponse<object>.Fail(
                        "Token không hợp lệ",
                        "UNAUTHORIZED",
                        null,
                        401
                    );
                }

                if (role != "warehouse_owner" && role != "admin")
                {
                    return ApiResponse<object>.Fail(
                        "Chỉ chủ kho hoặc admin mới được lưu layout xếp hàng thủ công",
                        "FORBIDDEN",
                        null,
                        403
                    );
                }

                var receipt = _context.InboundReceipts
                    .Include(r => r.InboundItems)
                        .ThenInclude(ii => ii.Item)
                    .FirstOrDefault(r => r.ReceiptId == receiptId);

                if (receipt == null)
                {
                    return ApiResponse<object>.Fail(
                        "Không tìm thấy yêu cầu gửi hàng",
                        "NOT_FOUND",
                        null,
                        404
                    );
                }

                if (!string.Equals(receipt.StackMode, "manual", StringComparison.OrdinalIgnoreCase))
                {
                    return ApiResponse<object>.Fail(
                        "Chỉ được lưu layout thủ công cho các yêu cầu ở chế độ bạn tự xếp",
                        "INVALID_STACK_MODE",
                        null,
                        400
                    );
                }

                if (receipt.Status != "pending")
                {
                    return ApiResponse<object>.Fail(
                        "Chỉ có thể lưu layout khi phiếu đang ở trạng thái pending",
                        "INVALID_STATUS",
                        null,
                        400
                    );
                }

                if (request.Items == null || !request.Items.Any())
                {
                    return ApiResponse<object>.Fail(
                        "Danh sách layout trống",
                        "EMPTY_LAYOUT",
                        null,
                        400
                    );
                }

                var inboundItemsById = receipt.InboundItems.ToDictionary(ii => ii.InboundItemId);

                // Validate inbound_item_id thuộc về phiếu
                foreach (var layoutItem in request.Items)
                {
                    if (!inboundItemsById.TryGetValue(layoutItem.InboundItemId, out var inboundItem))
                    {
                        return ApiResponse<object>.Fail(
                            $"InboundItemId {layoutItem.InboundItemId} không thuộc về phiếu này",
                            "INVALID_INBOUND_ITEM",
                            null,
                            400
                        );
                    }

                    var expectedQty = inboundItem.Quantity;
                    if (layoutItem.Units == null || !layoutItem.Units.Any())
                    {
                        return ApiResponse<object>.Fail(
                            $"InboundItemId {layoutItem.InboundItemId} không có đơn vị nào trong layout",
                            "EMPTY_UNITS",
                            null,
                            400
                        );
                    }

                    if (layoutItem.Units.Count != expectedQty)
                    {
                        return ApiResponse<object>.Fail(
                            $"Số đơn vị trong layout ({layoutItem.Units.Count}) không khớp với Quantity ({expectedQty}) của InboundItemId {layoutItem.InboundItemId}",
                            "INVALID_UNIT_COUNT",
                            null,
                            400
                        );
                    }
                }

                using var transaction = _context.Database.BeginTransaction();
                try
                {
                    var inboundItemIds = request.Items
                        .Select(i => i.InboundItemId)
                        .Distinct()
                        .ToList();

                    var existingUnits = _context.InboundItemStackUnits
                        .Where(u => inboundItemIds.Contains(u.InboundItemId))
                        .ToList();

                    if (existingUnits.Any())
                    {
                        _context.InboundItemStackUnits.RemoveRange(existingUnits);
                    }

                    foreach (var layoutItem in request.Items)
                    {
                        foreach (var unit in layoutItem.Units)
                        {
                            var entity = new InboundItemStackUnit
                            {
                                InboundItemId = layoutItem.InboundItemId,
                                UnitIndex = unit.UnitIndex,
                                LocalX = unit.LocalX,
                                LocalY = unit.LocalY,
                                LocalZ = unit.LocalZ,
                                Length = unit.Length,
                                Width = unit.Width,
                                Height = unit.Height,
                                RotationY = unit.RotationY
                            };

                            _context.InboundItemStackUnits.Add(entity);
                        }
                    }

                    _context.SaveChanges();
                    transaction.Commit();

                    return ApiResponse<object>.Ok(
                        new { ReceiptId = receipt.ReceiptId },
                        "Lưu layout xếp hàng thủ công thành công"
                    );
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (DbUpdateException ex)
            {
                return ApiResponse<object>.Fail(
                    $"Lỗi khi lưu layout xếp hàng: {ex.InnerException?.Message ?? ex.Message}",
                    "DATABASE_ERROR",
                    null,
                    500
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail(
                    $"Lỗi khi lưu layout xếp hàng: {ex.Message}",
                    "INTERNAL_ERROR",
                    null,
                    500
                );
            }
        }

        public ApiResponse<InboundApprovalViewModel> GetInboundApprovalView(int receiptId, int? accountId, string role)
        {
            try
            {
                if (accountId == null)
                {
                    return ApiResponse<InboundApprovalViewModel>.Fail(
                        "Token không hợp lệ",
                        "UNAUTHORIZED",
                        null,
                        401
                    );
                }

                var receipt = _context.InboundReceipts
                    .Include(r => r.Warehouse)
                    .Include(r => r.Customer)
                    .Include(r => r.InboundItems)
                        .ThenInclude(ii => ii.Item)
                            .ThenInclude(i => i.Product)
                    .Include(r => r.InboundItems)
                        .ThenInclude(ii => ii.Pallet)
                    .FirstOrDefault(r => r.ReceiptId == receiptId);

                if (receipt == null)
                {
                    return ApiResponse<InboundApprovalViewModel>.Fail(
                        "Không tìm thấy yêu cầu gửi hàng",
                        "NOT_FOUND",
                        null,
                        404
                    );
                }

                // Quyền: customer chỉ xem được yêu cầu của mình, owner/admin xem được kho của họ
                if (role == "customer" && receipt.CustomerId != accountId.Value)
                {
                    return ApiResponse<InboundApprovalViewModel>.Fail(
                        "Bạn không có quyền xem yêu cầu này",
                        "FORBIDDEN",
                        null,
                        403
                    );
                }

                string? zoneName = null;
                if (receipt.ZoneId.HasValue)
                {
                    zoneName = _context.WarehouseZones
                        .Where(z => z.ZoneId == receipt.ZoneId.Value)
                        .Select(z => z.ZoneName)
                        .FirstOrDefault();
                }

                // Nếu đã có layout chi tiết (InboundItemStackUnits) cho các inbound item, tính lại
                // kích thước khối hàng trên pallet theo bounding box của layout để hiển thị chính xác.
                var inboundItemIdsForBounds = receipt.InboundItems
                    .Select(ii => ii.InboundItemId)
                    .ToList();

                var blockBoundsByInboundItem = new Dictionary<int, (decimal Length, decimal Width, decimal Height)>();

                if (inboundItemIdsForBounds.Any())
                {
                    var allUnits = _context.InboundItemStackUnits
                        .Where(u => inboundItemIdsForBounds.Contains(u.InboundItemId))
                        .ToList();

                    if (allUnits.Any())
                    {
                        var unitsByItem = allUnits
                            .GroupBy(u => u.InboundItemId)
                            .ToDictionary(g => g.Key, g => g.ToList());

                        foreach (var ii in receipt.InboundItems)
                        {
                            if (!unitsByItem.TryGetValue(ii.InboundItemId, out var units) || !units.Any())
                            {
                                continue;
                            }

                            // Chiều dài & rộng: bounding box theo trục X/Z quanh tâm pallet
                            var minX = units.Min(u => u.LocalX - u.Length / 2m);
                            var maxX = units.Max(u => u.LocalX + u.Length / 2m);
                            var minZ = units.Min(u => u.LocalZ - u.Width / 2m);
                            var maxZ = units.Max(u => u.LocalZ + u.Width / 2m);

                            var blockLength = maxX - minX;
                            var blockWidth = maxZ - minZ;

                            if (blockLength <= 0m && ii.Item.Length > 0m)
                            {
                                blockLength = ii.Item.Length;
                            }

                            if (blockWidth <= 0m && ii.Item.Width > 0m)
                            {
                                blockWidth = ii.Item.Width;
                            }

                            // Chiều cao: đỉnh cao nhất trừ đi chiều cao pallet
                            var palletHeight = ii.Pallet.Height;
                            var maxTop = units.Max(u => u.LocalY + u.Height / 2m);
                            var goodsHeight = maxTop - palletHeight;

                            if (goodsHeight <= 0m && ii.Item.Height > 0m)
                            {
                                goodsHeight = ii.Item.Height;
                            }

                            if (blockLength > 0m || blockWidth > 0m || goodsHeight > 0m)
                            {
                                blockBoundsByInboundItem[ii.InboundItemId] = (blockLength, blockWidth, goodsHeight);
                            }
                        }
                    }
                }

                var approval = new InboundApprovalViewModel
                {
                    ReceiptId = receipt.ReceiptId,
                    ReceiptNumber = receipt.ReceiptNumber,
                    WarehouseId = receipt.WarehouseId,
                    WarehouseName = receipt.Warehouse.WarehouseName,
                    ZoneId = receipt.ZoneId,
                    ZoneName = zoneName,
                    CustomerId = receipt.CustomerId,
                    CustomerName = receipt.Customer.FullName,
                    Status = receipt.Status,
                    Notes = receipt.Notes,
                    StackMode = receipt.StackMode,
                    Items = receipt.InboundItems.Select(ii =>
                    {
                        var product = ii.Item.Product;
                        var pallet = ii.Pallet;

                        // Heuristic xác định hàng bao
                        var unitLower = product.Unit.ToLower();
                        var categoryLower = product.Category?.ToLower();
                        bool isBag = unitLower.Contains("bao") || (categoryLower != null && categoryLower.Contains("bao"));

                        // Kích thước khối ban đầu từ Item
                        var itemLength = ii.Item.Length;
                        var itemWidth = ii.Item.Width;
                        var itemHeight = ii.Item.Height;

                        // Nếu đã có bounding box từ layout, ưu tiên dùng để hiển thị
                        if (blockBoundsByInboundItem.TryGetValue(ii.InboundItemId, out var bounds))
                        {
                            if (bounds.Length > 0m) itemLength = bounds.Length;
                            if (bounds.Width > 0m) itemWidth = bounds.Width;
                            if (bounds.Height > 0m) itemHeight = bounds.Height;
                        }

                        return new InboundApprovalItemViewModel
                        {
                            InboundItemId = ii.InboundItemId,
                            ItemId = ii.ItemId,
                            PalletId = ii.PalletId,
                            PalletBarcode = pallet.Barcode,
                            ProductId = product.ProductId,
                            ProductCode = product.ProductCode,
                            ProductName = product.ProductName,
                            Unit = product.Unit,
                            Category = product.Category,
                            Quantity = ii.Quantity,
                            UnitLength = product.StandardLength,
                            UnitWidth = product.StandardWidth,
                            UnitHeight = product.StandardHeight,
                            ItemLength = itemLength,
                            ItemWidth = itemWidth,
                            ItemHeight = itemHeight,
                            PalletLength = pallet.Length,
                            PalletWidth = pallet.Width,
                            PalletHeight = pallet.Height,
                            IsBag = isBag
                        };
                    }).ToList()
                };

                return ApiResponse<InboundApprovalViewModel>.Ok(
                    approval,
                    "Lấy dữ liệu duyệt yêu cầu thành công"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<InboundApprovalViewModel>.Fail(
                    $"Lỗi khi lấy dữ liệu duyệt yêu cầu: {ex.Message}",
                    "INTERNAL_ERROR",
                    null,
                    500
                );
            }
        }

        public ApiResponse<InboundOptimizeLayoutViewModel> OptimizeInboundLayout(int receiptId, int? accountId, string role, ApproveInboundLayoutRequest? request)
        {
            try
            {
                if (accountId == null)
                {
                    return ApiResponse<InboundOptimizeLayoutViewModel>.Fail(
                        "Token không hợp lệ",
                        "UNAUTHORIZED",
                        null,
                        401
                    );
                }

                if (role != "warehouse_owner" && role != "admin")
                {
                    return ApiResponse<InboundOptimizeLayoutViewModel>.Fail(
                        "Chỉ chủ kho hoặc admin mới được tối ưu layout inbound",
                        "FORBIDDEN",
                        null,
                        403
                    );
                }

                var receipt = _context.InboundReceipts
                    .Include(r => r.Warehouse)
                    .Include(r => r.InboundItems)
                        .ThenInclude(ii => ii.Item)
                            .ThenInclude(i => i.Product)
                    .Include(r => r.InboundItems)
                        .ThenInclude(ii => ii.Pallet)
                    .FirstOrDefault(r => r.ReceiptId == receiptId);

                if (receipt == null)
                {
                    return ApiResponse<InboundOptimizeLayoutViewModel>.Fail(
                        "Không tìm thấy yêu cầu gửi hàng",
                        "NOT_FOUND",
                        null,
                        404
                    );
                }

                if (receipt.Status != "pending")
                {
                    return ApiResponse<InboundOptimizeLayoutViewModel>.Fail(
                        "Chỉ có thể tối ưu layout cho các yêu cầu ở trạng thái pending",
                        "INVALID_STATUS",
                        null,
                        400
                    );
                }

                // Lấy các khu vực thuộc về customer trong kho này
                // Nếu phiếu đã gắn với một Zone cụ thể, chỉ lấy đúng zone đó
                var zonesQuery = _context.WarehouseZones
                    .Where(z => z.WarehouseId == receipt.WarehouseId && z.CustomerId == receipt.CustomerId);

                if (receipt.ZoneId.HasValue)
                {
                    zonesQuery = zonesQuery.Where(z => z.ZoneId == receipt.ZoneId.Value);
                }

                var zones = zonesQuery.ToList();

                if (!zones.Any())
                {
                    return ApiResponse<InboundOptimizeLayoutViewModel>.Fail(
                        "Khách hàng chưa có khu vực nào trong kho để xếp pallet",
                        "NO_ZONES_FOR_CUSTOMER",
                        null,
                        400
                    );
                }

                var zoneIds = zones.Select(z => z.ZoneId).ToList();

                var racks = _context.Racks
                    .Include(r => r.Shelves)
                    .Where(r => zoneIds.Contains(r.ZoneId))
                    .ToList();

                var racksByZone = racks
                    .GroupBy(r => r.ZoneId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // Nếu có hàng thùng (box) nhưng không có rack nào trong các khu vực của customer thì không cho duyệt
                bool hasBoxItems = receipt.InboundItems.Any(ii => ii.Item.ItemType.ToLower() == "box");

                bool hasAnyRack = racks.Any();

                if (hasBoxItems && !hasAnyRack)
                {
                    return ApiResponse<InboundOptimizeLayoutViewModel>.Fail(
                        "Khách hàng chưa có rack trong các khu vực kho để xếp hàng thùng (box)",
                        "NO_RACK_FOR_BOX_ITEMS",
                        null,
                        400
                    );
                }

                // Các pallet location hiện có trong các zone này
                var existingLocations = _context.PalletLocations
                    .Include(pl => pl.Pallet)
                    .Where(pl => zoneIds.Contains(pl.ZoneId))
                    .ToList();

                // Tính điểm ưu tiên zone dựa trên khoảng cách đến bàn checkin và cổng ra
                var warehouseEntity = receipt.Warehouse;
                var gates = _context.WarehouseGates
                    .Where(g => g.WarehouseId == receipt.WarehouseId)
                    .ToList();

                var exitGates = gates
                    .Where(g => string.Equals(g.GateType, "exit", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (!exitGates.Any())
                {
                    exitGates = gates;
                }

                decimal? checkinX = warehouseEntity.CheckinPositionX;
                decimal? checkinZ = warehouseEntity.CheckinPositionZ;

                decimal Distance2D(decimal x1, decimal z1, decimal x2, decimal z2)
                {
                    var dx = x1 - x2;
                    var dz = z1 - z2;
                    return (decimal)Math.Sqrt((double)(dx * dx + dz * dz));
                }

                var zoneScores = new Dictionary<int, decimal>();

                foreach (var z in zones)
                {
                    var centerX = z.PositionX + z.Length / 2m;
                    var centerZ = z.PositionZ + z.Width / 2m;

                    decimal distToExit = 0m;
                    if (exitGates.Any())
                    {
                        distToExit = exitGates
                            .Select(g => Distance2D(centerX, centerZ, g.PositionX, g.PositionZ))
                            .DefaultIfEmpty(0m)
                            .Min();
                    }

                    decimal distToCheckin = 0m;
                    if (checkinX.HasValue && checkinZ.HasValue)
                    {
                        distToCheckin = Distance2D(centerX, centerZ, checkinX.Value, checkinZ.Value);
                    }

                    decimal score;

                    if (checkinX.HasValue && checkinZ.HasValue)
                    {
                        // Ưu tiên gần bàn checkin hơn, sau đó đến cổng ra
                        score = distToCheckin * 0.7m + distToExit * 0.3m;
                    }
                    else
                    {
                        // Nếu chưa cấu hình vị trí checkin thì chỉ dùng khoảng cách tới cổng ra
                        score = distToExit;
                    }

                    zoneScores[z.ZoneId] = score;
                }

                IEnumerable<WarehouseZone> OrderZonesByProximity(IEnumerable<WarehouseZone> source)
                {
                    if (!zoneScores.Any())
                    {
                        return source.OrderBy(z => z.PositionZ).ThenBy(z => z.PositionX);
                    }

                    return source
                        .OrderBy(z => zoneScores.TryGetValue(z.ZoneId, out var s) ? s : 0m)
                        .ThenBy(z => z.PositionZ)
                        .ThenBy(z => z.PositionX);
                }

                // Parse allowed_item_types: nếu null thì set default theo warehouse_type
                var warehouseType = receipt.Warehouse.WarehouseType.ToLower();
                List<string> allowedItemTypes;

                if (!string.IsNullOrWhiteSpace(receipt.Warehouse.AllowedItemTypes))
                {
                    try
                    {
                        allowedItemTypes = JsonSerializer
                            .Deserialize<List<string>>(receipt.Warehouse.AllowedItemTypes)!
                            .Select(t => t.ToLower())
                            .Distinct()
                            .ToList();
                    }
                    catch
                    {
                        // Nếu parse lỗi thì fallback theo loại kho
                        allowedItemTypes = warehouseType switch
                        {
                            "small" => new List<string> { "bag" },
                            "medium" => new List<string> { "bag", "box" },
                            "large" => new List<string> { "bag", "box", "custom" },
                            _ => new List<string> { "bag", "box", "custom" }
                        };
                    }
                }
                else
                {
                    allowedItemTypes = warehouseType switch
                    {
                        "small" => new List<string> { "bag" },
                        "medium" => new List<string> { "bag", "box" },
                        "large" => new List<string> { "bag", "box", "custom" },
                        _ => new List<string> { "bag", "box", "custom" }
                    };
                }

                // Preferred layout từ FE (kéo thả): PalletId -> Priority (số nhỏ = ưu tiên cao)
                var preferredLayouts = request?.PreferredLayouts ?? new List<PreferredPalletLayoutDto>();

                var preferredDict = preferredLayouts
                    .GroupBy(p => p.PalletId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Min(x => x.Priority ?? 0m)
                    );

                // Map chi tiết layout FE gửi cho từng pallet (zone/shelf/toạ độ nếu có)
                var preferredLayoutMap = preferredLayouts
                    .GroupBy(p => p.PalletId)
                    .ToDictionary(g => g.Key, g => g.First());

                var forceUsePreferred = request?.ForceUsePreferredLayout ?? false;

                IEnumerable<InboundItem> orderedItems;

                if (forceUsePreferred && preferredDict.Any())
                {
                    var preferredItems = receipt.InboundItems
                        .Where(ii => preferredDict.ContainsKey(ii.PalletId))
                        .OrderBy(ii => preferredDict[ii.PalletId])
                        .ToList();

                    var otherItems = receipt.InboundItems
                        .Where(ii => !preferredDict.ContainsKey(ii.PalletId))
                        .OrderByDescending(ii => ii.Item.IsHeavy)
                        .ThenBy(ii => ii.Item.PriorityLevel)
                        .ThenByDescending(ii => ii.Item.Weight ?? 0);

                    orderedItems = preferredItems.Concat(otherItems);
                }
                else
                {
                    // Hành vi cũ: dùng priority nếu có, sau đó đến trọng lượng/priority level
                    orderedItems = receipt.InboundItems
                        .OrderBy(ii => preferredDict.ContainsKey(ii.PalletId) ? preferredDict[ii.PalletId] : 0m)
                        .ThenByDescending(ii => ii.Item.IsHeavy)
                        .ThenBy(ii => ii.Item.PriorityLevel)
                        .ThenByDescending(ii => ii.Item.Weight ?? 0);
                }

                // Các inbound items cần xếp - sau khi áp dụng thứ tự ưu tiên
                var inboundItems = orderedItems.ToList();

                // Tập pallet chứa hàng không được xếp chồng (ví dụ kính, thủy tinh)
                var nonStackablePalletIds = inboundItems
                    .Where(ii => ii.Item.IsNonStackable == true)
                    .Select(ii => ii.PalletId)
                    .Distinct()
                    .ToHashSet();

                // Kiểm tra pallet đã được gán location chưa
                var inboundPalletIds = inboundItems.Select(ii => ii.PalletId).Distinct().ToList();
                var alreadyLocated = _context.PalletLocations
                    .Where(pl => inboundPalletIds.Contains(pl.PalletId))
                    .Select(pl => pl.PalletId)
                    .Distinct()
                    .ToList();

                if (alreadyLocated.Any())
                {
                    return ApiResponse<InboundOptimizeLayoutViewModel>.Fail(
                        "Một hoặc nhiều pallet trong yêu cầu đã được xếp trong kho",
                        "PALLET_ALREADY_ALLOCATED",
                        null,
                        400
                    );
                }

                var newLocations = new List<PalletLocation>();

                // Hàm kiểm tra overlap 2 hình chữ nhật trên mặt phẳng XZ
                bool IsOverlap(decimal x1, decimal z1, decimal l1, decimal w1, decimal x2, decimal z2, decimal l2, decimal w2)
                {
                    return x1 < x2 + l2 && x1 + l1 > x2 && z1 < z2 + w2 && z1 + w1 > z2;
                }

                foreach (var ii in inboundItems)
                {
                    var pallet = ii.Pallet;

                    // Rule theo loại hàng & loại kho
                    var itemType = ii.Item.ItemType?.ToLower() ?? "box";
                    var itemIsBag = itemType == "bag";
                    var itemIsBox = itemType == "box";

                    if (!allowedItemTypes.Contains(itemType))
                    {
                        return ApiResponse<InboundOptimizeLayoutViewModel>.Fail(
                            $"Loại hàng '{itemType}' không được phép trong kho này",
                            "ITEM_TYPE_NOT_ALLOWED_IN_WAREHOUSE",
                            null,
                            400
                        );
                    }

                    // Kho small: chỉ chấp nhận hàng bao (rule business rõ ràng)
                    if (warehouseType == "small" && !itemIsBag)
                    {
                        return ApiResponse<InboundOptimizeLayoutViewModel>.Fail(
                            "Kho loại small chỉ chứa hàng bao",
                            "WAREHOUSE_SMALL_BAG_ONLY",
                            null,
                            400
                        );
                    }

                    // Candidate zones theo loại hàng
                    // - Hàng bao (bag): có thể ở ground hoặc rack (tất cả zone của customer)
                    // - Hàng thùng (box): chỉ ở zone rack
                    // - Loại khác: cho phép tất cả zone của customer
                    var candidateZones = itemIsBox
                        ? zones.Where(z => z.ZoneType == "rack").ToList()
                        : zones;

                    if (!candidateZones.Any())
                    {
                        return ApiResponse<InboundOptimizeLayoutViewModel>.Fail(
                            "Không có khu vực phù hợp trong kho cho loại hàng này",
                            "NO_COMPATIBLE_ZONES",
                            null,
                            400
                        );
                    }

                    bool placed = false;

                    // Nếu FE gửi layout chi tiết (zone/shelf/XZ) và bật forceUsePreferred,
                    // thử xếp đúng toạ độ này trước khi chạy thuật toán quét chuẩn.
                    if (forceUsePreferred && preferredLayoutMap.TryGetValue(pallet.PalletId, out var prefLayout))
                    {
                        // Hàng box: ưu tiên layout có ZoneId + ShelfId + PositionX/Z trên kệ
                        if (itemIsBox
                            && prefLayout.ZoneId.HasValue
                            && prefLayout.ShelfId.HasValue
                            && prefLayout.PositionX.HasValue
                            && prefLayout.PositionZ.HasValue)
                        {
                            var zone = candidateZones.FirstOrDefault(z => z.ZoneId == prefLayout.ZoneId.Value);
                            if (zone != null && racksByZone.TryGetValue(zone.ZoneId, out var zoneRacks) && zoneRacks.Any())
                            {
                                var rack = zoneRacks.FirstOrDefault(r => r.Shelves.Any(s => s.ShelfId == prefLayout.ShelfId.Value));
                                var shelf = rack?.Shelves.FirstOrDefault(s => s.ShelfId == prefLayout.ShelfId.Value);

                                if (rack != null && shelf != null)
                                {
                                    var palletHeight = pallet.Height;
                                    var itemStackHeight = ii.Item.Height;
                                    var clearHeight = GetShelfClearHeight(rack, shelf);

                                    if (palletHeight + itemStackHeight > clearHeight)
                                    {
                                        continue;
                                    }

                                    var shelfX = rack.PositionX;
                                    var shelfZ = rack.PositionZ;
                                    var shelfLength = Math.Min(rack.Length, shelf.Length);
                                    var shelfWidth = Math.Min(rack.Width, shelf.Width);

                                    var x = prefLayout.PositionX.Value;
                                    var z = prefLayout.PositionZ.Value;

                                    // Kiểm tra pallet nằm trọn trong mặt kệ
                                    if (x >= shelfX && x + pallet.Length <= shelfX + shelfLength &&
                                        z >= shelfZ && z + pallet.Width <= shelfZ + shelfWidth)
                                    {
                                        var shelfExisting = existingLocations.Where(l => l.ShelfId == shelf.ShelfId).ToList();
                                        var shelfNew = newLocations.Where(l => l.ShelfId == shelf.ShelfId).ToList();

                                        bool collision = false;

                                        foreach (var loc in shelfExisting)
                                        {
                                            var otherPallet = loc.Pallet;
                                            if (IsOverlap(x, z, pallet.Length, pallet.Width,
                                                    loc.PositionX, loc.PositionZ, otherPallet.Length, otherPallet.Width))
                                            {
                                                collision = true;
                                                break;
                                            }
                                        }

                                        if (!collision)
                                        {
                                            foreach (var loc in shelfNew)
                                            {
                                                var otherPallet = loc.Pallet;
                                                if (IsOverlap(x, z, pallet.Length, pallet.Width,
                                                        loc.PositionX, loc.PositionZ, otherPallet.Length, otherPallet.Width))
                                                {
                                                    collision = true;
                                                    break;
                                                }
                                            }
                                        }

                                        if (!collision)
                                        {
                                            var location = new PalletLocation
                                            {
                                                PalletId = pallet.PalletId,
                                                ZoneId = zone.ZoneId,
                                                ShelfId = shelf.ShelfId,
                                                PositionX = x,
                                                PositionY = shelf.PositionY,
                                                PositionZ = z,
                                                StackLevel = 1,
                                                StackedOnPallet = null,
                                                IsGround = false,
                                                AssignedAt = DateTime.Now
                                            };

                                            newLocations.Add(location);
                                            placed = true;
                                        }
                                    }
                                }
                            }
                        }
                        // Hàng không phải box: cho phép FE chỉ zone + PositionX/Z trên mặt ground
                        else if (!itemIsBox
                            && prefLayout.ZoneId.HasValue
                            && prefLayout.PositionX.HasValue
                            && prefLayout.PositionZ.HasValue)
                        {
                            var zone = candidateZones.FirstOrDefault(z => z.ZoneId == prefLayout.ZoneId.Value);
                            if (zone != null)
                            {
                                var maxX = zone.PositionX + zone.Length;
                                var maxZ = zone.PositionZ + zone.Width;

                                var x = prefLayout.PositionX.Value;
                                var z = prefLayout.PositionZ.Value;

                                if (x >= zone.PositionX && x + pallet.Length <= maxX &&
                                    z >= zone.PositionZ && z + pallet.Width <= maxZ)
                                {
                                    var zoneExisting = existingLocations.Where(l => l.ZoneId == zone.ZoneId).ToList();
                                    var zoneNew = newLocations.Where(l => l.ZoneId == zone.ZoneId).ToList();

                                    bool collision = false;

                                    foreach (var loc in zoneExisting)
                                    {
                                        var otherPallet = loc.Pallet;
                                        if (IsOverlap(x, z, pallet.Length, pallet.Width,
                                                loc.PositionX, loc.PositionZ, otherPallet.Length, otherPallet.Width))
                                        {
                                            collision = true;
                                            break;
                                        }
                                    }

                                    if (!collision)
                                    {
                                        foreach (var loc in zoneNew)
                                        {
                                            var otherPallet = loc.Pallet;
                                            if (IsOverlap(x, z, pallet.Length, pallet.Width,
                                                    loc.PositionX, loc.PositionZ, otherPallet.Length, otherPallet.Width))
                                            {
                                                collision = true;
                                                break;
                                            }
                                        }
                                    }

                                    if (!collision)
                                    {
                                        var location = new PalletLocation
                                        {
                                            PalletId = pallet.PalletId,
                                            ZoneId = zone.ZoneId,
                                            ShelfId = null,
                                            PositionX = x,
                                            PositionY = zone.PositionY,
                                            PositionZ = z,
                                            StackLevel = 1,
                                            StackedOnPallet = null,
                                            IsGround = true,
                                            AssignedAt = DateTime.Now
                                        };

                                        newLocations.Add(location);
                                        placed = true;
                                    }
                                }
                            }
                        }
                    }

                    if (placed)
                    {
                        continue;
                    }

                    if (itemIsBox)
                    {
                        foreach (var zone in OrderZonesByProximity(candidateZones))
                        {
                            if (!racksByZone.TryGetValue(zone.ZoneId, out var zoneRacks) || !zoneRacks.Any())
                            {
                                continue;
                            }

                            foreach (var rack in zoneRacks.OrderBy(r => r.PositionZ).ThenBy(r => r.PositionX))
                            {
                                foreach (var shelf in rack.Shelves.OrderBy(s => s.ShelfLevel))
                                {
                                    var shelfExisting = existingLocations.Where(l => l.ShelfId == shelf.ShelfId).ToList();
                                    var shelfNew = newLocations.Where(l => l.ShelfId == shelf.ShelfId).ToList();

                                    var palletHeight = pallet.Height;
                                    var itemStackHeight = ii.Item.Height;
                                    var clearHeight = GetShelfClearHeight(rack, shelf);

                                    if (palletHeight + itemStackHeight > clearHeight)
                                    {
                                        continue;
                                    }

                                    var shelfX = rack.PositionX;
                                    var shelfZ = rack.PositionZ;
                                    var shelfLength = Math.Min(rack.Length, shelf.Length);
                                    var shelfWidth = Math.Min(rack.Width, shelf.Width);

                                    if (pallet.Length > shelfLength || pallet.Width > shelfWidth)
                                    {
                                        continue;
                                    }

                                    var stepX = pallet.Length;
                                    var stepZ = pallet.Width;

                                    var maxX = shelfX + shelfLength;
                                    var maxZ = shelfZ + shelfWidth;

                                    for (decimal x = shelfX; x + pallet.Length <= maxX && !placed; x += stepX)
                                    {
                                        for (decimal z = shelfZ; z + pallet.Width <= maxZ && !placed; z += stepZ)
                                        {
                                            bool collision = false;

                                            foreach (var loc in shelfExisting)
                                            {
                                                var otherPallet = loc.Pallet;
                                                if (IsOverlap(x, z, pallet.Length, pallet.Width,
                                                        loc.PositionX, loc.PositionZ, otherPallet.Length, otherPallet.Width))
                                                {
                                                    collision = true;
                                                    break;
                                                }
                                            }

                                            if (collision) continue;

                                            foreach (var loc in shelfNew)
                                            {
                                                var otherPallet = loc.Pallet;
                                                if (IsOverlap(x, z, pallet.Length, pallet.Width,
                                                        loc.PositionX, loc.PositionZ, otherPallet.Length, otherPallet.Width))
                                                {
                                                    collision = true;
                                                    break;
                                                }
                                            }

                                            if (collision) continue;

                                            var location = new PalletLocation
                                            {
                                                PalletId = pallet.PalletId,
                                                ZoneId = zone.ZoneId,
                                                ShelfId = shelf.ShelfId,
                                                PositionX = x,
                                                PositionY = shelf.PositionY,
                                                PositionZ = z,
                                                StackLevel = 1,
                                                StackedOnPallet = null,
                                                IsGround = false,
                                                AssignedAt = DateTime.Now
                                            };

                                            newLocations.Add(location);
                                            placed = true;
                                        }
                                    }

                                    if (placed) break;
                                }

                                if (placed) break;
                            }

                            if (placed) break;
                        }

                        if (!placed)
                        {
                            return ApiResponse<InboundOptimizeLayoutViewModel>.Fail(
                                "Không đủ không gian trống trong các khu vực của khách hàng để xếp tất cả pallet (kể cả xếp chồng)",
                                "NO_SPACE_FOR_PALLET",
                                null,
                                400
                            );
                        }
                    }
                    else
                    {
                        foreach (var zone in OrderZonesByProximity(candidateZones))
                        {
                            var zoneExisting = existingLocations.Where(l => l.ZoneId == zone.ZoneId).ToList();
                            var zoneNew = newLocations.Where(l => l.ZoneId == zone.ZoneId).ToList();

                            var maxX = zone.PositionX + zone.Length;
                            var maxZ = zone.PositionZ + zone.Width;

                            if (pallet.Length > zone.Length || pallet.Width > zone.Width)
                            {
                                continue;
                            }

                            var stepX = pallet.Length;
                            var stepZ = pallet.Width;

                            for (decimal x = zone.PositionX; x + pallet.Length <= maxX && !placed; x += stepX)
                            {
                                for (decimal z = zone.PositionZ; z + pallet.Width <= maxZ && !placed; z += stepZ)
                                {
                                    bool collision = false;

                                    foreach (var loc in zoneExisting)
                                    {
                                        var otherPallet = loc.Pallet;
                                        if (IsOverlap(x, z, pallet.Length, pallet.Width,
                                                loc.PositionX, loc.PositionZ, otherPallet.Length, otherPallet.Width))
                                        {
                                            collision = true;
                                            break;
                                        }
                                    }

                                    if (collision) continue;

                                    foreach (var loc in zoneNew)
                                    {
                                        var otherPallet = loc.Pallet;
                                        if (IsOverlap(x, z, pallet.Length, pallet.Width,
                                                loc.PositionX, loc.PositionZ, otherPallet.Length, otherPallet.Width))
                                        {
                                            collision = true;
                                            break;
                                        }
                                    }

                                    if (collision) continue;

                                    var location = new PalletLocation
                                    {
                                        PalletId = pallet.PalletId,
                                        ZoneId = zone.ZoneId,
                                        ShelfId = null,
                                        PositionX = x,
                                        PositionY = zone.PositionY,
                                        PositionZ = z,
                                        StackLevel = 1,
                                        StackedOnPallet = null,
                                        IsGround = true,
                                        AssignedAt = DateTime.Now
                                    };

                                    newLocations.Add(location);
                                    placed = true;
                                }
                            }

                            if (placed) break;
                        }

                        if (!placed)
                        {
                            bool stacked = false;

                            foreach (var zone in OrderZonesByProximity(candidateZones))
                            {
                                var zoneExisting = existingLocations.Where(l => l.ZoneId == zone.ZoneId && l.IsGround == true && l.StackLevel == 1).ToList();
                                var zoneNew = newLocations.Where(l => l.ZoneId == zone.ZoneId && l.IsGround == true && l.StackLevel == 1).ToList();

                                foreach (var baseLoc in zoneExisting.Concat(zoneNew))
                                {
                                    var basePallet = baseLoc.Pallet;

                                    var baseInboundItem = inboundItems.FirstOrDefault(x => x.PalletId == basePallet.PalletId);
                                    if (baseInboundItem == null)
                                    {
                                        continue;
                                    }

                                    // Chỉ xếp chồng khi pallet dưới và pallet trên chứa cùng loại hàng (cùng ProductId)
                                    if (baseInboundItem.Item.ProductId != ii.Item.ProductId)
                                    {
                                        continue;
                                    }

                                    // Không xếp chồng nếu pallet dưới hoặc pallet trên thuộc loại không được xếp chồng
                                    if (nonStackablePalletIds.Contains(basePallet.PalletId) || nonStackablePalletIds.Contains(pallet.PalletId))
                                    {
                                        continue;
                                    }

                                    if (pallet.Length > basePallet.Length || pallet.Width > basePallet.Width)
                                    {
                                        continue;
                                    }

                                    bool hasTop = existingLocations.Any(l => l.StackedOnPallet == basePallet.PalletId)
                                        || newLocations.Any(l => l.StackedOnPallet == basePallet.PalletId);

                                    if (hasTop)
                                    {
                                        continue;
                                    }

                                    var baseHeight = basePallet.Height;
                                    var newHeight = pallet.Height;
                                    var totalPalletHeight = baseHeight + newHeight;

                                    var baseMaxStack = basePallet.MaxStackHeight;
                                    var newMaxStack = pallet.MaxStackHeight;
                                    var allowedStackHeight = Math.Min(baseMaxStack, newMaxStack);

                                    if (totalPalletHeight > 1.5m || totalPalletHeight > allowedStackHeight)
                                    {
                                        continue;
                                    }

                                    var stackedLocation = new PalletLocation
                                    {
                                        PalletId = pallet.PalletId,
                                        ZoneId = baseLoc.ZoneId,
                                        ShelfId = baseLoc.ShelfId,
                                        PositionX = baseLoc.PositionX,
                                        PositionY = baseLoc.PositionY + baseHeight,
                                        PositionZ = baseLoc.PositionZ,
                                        StackLevel = 2,
                                        StackedOnPallet = basePallet.PalletId,
                                        IsGround = false,
                                        AssignedAt = DateTime.Now
                                    };

                                    newLocations.Add(stackedLocation);
                                    stacked = true;
                                    break;
                                }

                                if (stacked) break;
                            }

                            if (!stacked)
                            {
                                return ApiResponse<InboundOptimizeLayoutViewModel>.Fail(
                                    "Không đủ không gian trống trong các khu vực của khách hàng để xếp tất cả pallet (kể cả xếp chồng)",
                                    "NO_SPACE_FOR_PALLET",
                                    null,
                                    400
                                );
                            }
                        }
                    }
                }

                var response = new InboundOptimizeLayoutViewModel
                {
                    ReceiptId = receipt.ReceiptId,
                    WarehouseId = receipt.WarehouseId,
                    CustomerId = receipt.CustomerId,
                    Layouts = newLocations.Select(l => new InboundOptimizeLayoutItemViewModel
                    {
                        PalletId = l.PalletId,
                        ZoneId = l.ZoneId,
                        ShelfId = l.ShelfId,
                        PositionX = l.PositionX,
                        PositionY = l.PositionY,
                        PositionZ = l.PositionZ,
                        StackLevel = l.StackLevel,
                        StackedOnPalletId = l.StackedOnPallet,
                        IsGround = l.IsGround
                    }).ToList()
                };

                return ApiResponse<InboundOptimizeLayoutViewModel>.Ok(
                    response,
                    "Tính toán layout tối ưu thành công"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<InboundOptimizeLayoutViewModel>.Fail(
                    $"Lỗi khi tối ưu layout inbound: {ex.Message}",
                    "INTERNAL_ERROR",
                    null,
                    500
                );
            }
        }

        public ApiResponse<object> ApproveInboundRequest(int receiptId, int? accountId, string role, ApproveInboundLayoutRequest? request)
        {
            try
            {
                if (accountId == null)
                {
                    return ApiResponse<object>.Fail(
                        "Token không hợp lệ",
                        "UNAUTHORIZED",
                        null,
                        401
                    );
                }

                if (role != "warehouse_owner" && role != "admin")
                {
                    return ApiResponse<object>.Fail(
                        "Chỉ chủ kho hoặc admin mới được duyệt yêu cầu",
                        "FORBIDDEN",
                        null,
                        403
                    );
                }

                // Chiều cao thực tế của khối hàng trên pallet (không chỉ chiều cao 1 đơn vị),
                // dùng cho kiểm tra khoảng cách giữa các tầng kệ.
                var stackGoodsHeights = new Dictionary<int, decimal>();

                using var transaction = _context.Database.BeginTransaction();

                var receipt = _context.InboundReceipts
                    .Include(r => r.Warehouse)
                    .Include(r => r.InboundItems)
                        .ThenInclude(ii => ii.Item)
                            .ThenInclude(i => i.Product)
                    .Include(r => r.InboundItems)
                        .ThenInclude(ii => ii.Pallet)
                    .FirstOrDefault(r => r.ReceiptId == receiptId);

                if (receipt == null)
                {
                    transaction.Rollback();
                    return ApiResponse<object>.Fail(
                        "Không tìm thấy yêu cầu gửi hàng",
                        "NOT_FOUND",
                        null,
                        404
                    );
                }

                if (receipt.Status != "pending")
                {
                    transaction.Rollback();
                    return ApiResponse<object>.Fail(
                        "Chỉ có thể duyệt các yêu cầu ở trạng thái pending",
                        "INVALID_STATUS",
                        null,
                        400
                    );
                }

                // Nếu ở chế độ bạn tự xếp, yêu cầu phải có layout thủ công đầy đủ trước khi duyệt
                if (string.Equals(receipt.StackMode, "manual", StringComparison.OrdinalIgnoreCase))
                {
                    var inboundItemIds = receipt.InboundItems
                        .Select(ii => ii.InboundItemId)
                        .ToList();

                    var unitsByItem = _context.InboundItemStackUnits
                        .Where(u => inboundItemIds.Contains(u.InboundItemId))
                        .GroupBy(u => u.InboundItemId)
                        .ToDictionary(g => g.Key, g => g.ToList());

                    foreach (var ii in receipt.InboundItems)
                    {
                        var expectedQty = ii.Quantity;

                        if (!unitsByItem.TryGetValue(ii.InboundItemId, out var units) || units.Count != expectedQty)
                        {
                            transaction.Rollback();
                            return ApiResponse<object>.Fail(
                                "Layout xếp hàng thủ công chưa đầy đủ cho tất cả pallet. Vui lòng lưu layout trước khi duyệt.",
                                "MANUAL_LAYOUT_NOT_READY",
                                null,
                                400
                            );
                        }
                    }

                    // Tính chiều cao thực tế của khối hàng trên pallet từ layout thủ công
                    foreach (var ii in receipt.InboundItems)
                    {
                        if (unitsByItem.TryGetValue(ii.InboundItemId, out var units) && units.Any())
                        {
                            var palletHeight = ii.Pallet.Height;
                            var maxTop = units.Max(u => u.LocalY + u.Height / 2m);
                            var goodsHeight = maxTop - palletHeight;

                            if (goodsHeight <= 0m)
                            {
                                goodsHeight = ii.Item.Height;
                            }

                            stackGoodsHeights[ii.InboundItemId] = goodsHeight;
                        }
                        else
                        {
                            stackGoodsHeights[ii.InboundItemId] = ii.Item.Height;
                        }
                    }
                }

                // Nếu ở chế độ auto, sinh layout mặc định cho từng inbound item và lưu vào InboundItemStackUnits
                if (string.Equals(receipt.StackMode, "auto", StringComparison.OrdinalIgnoreCase))
                {
                    var inboundItemIdsForAuto = receipt.InboundItems
                        .Select(ii => ii.InboundItemId)
                        .ToList();

                    if (inboundItemIdsForAuto.Any())
                    {
                        var existingAutoUnits = _context.InboundItemStackUnits
                            .Where(u => inboundItemIdsForAuto.Contains(u.InboundItemId))
                            .ToList();

                        if (existingAutoUnits.Any())
                        {
                            _context.InboundItemStackUnits.RemoveRange(existingAutoUnits);
                        }

                        // Xác định pattern auto: straight / brick / cross (mặc định straight nếu null/invalid)
                        var pattern = (receipt.AutoStackTemplate ?? "straight").Trim().ToLowerInvariant();
                        if (pattern != "brick" && pattern != "cross" && pattern != "straight")
                        {
                            pattern = "straight";
                        }

                        foreach (var ii in receipt.InboundItems)
                        {
                            var product = ii.Item.Product;
                            var pallet = ii.Pallet;

                            var quantity = ii.Quantity;
                            if (quantity <= 0)
                            {
                                quantity = 1;
                            }

                            var unitLower = product.Unit.ToLower();
                            var categoryLower = product.Category?.ToLower();
                            var isBag = unitLower.Contains("bao") || (categoryLower != null && categoryLower.Contains("bao"));

                            var palletLength = pallet.Length;
                            var palletWidth = pallet.Width;
                            var palletHeight = pallet.Height;

                            var unitLength = product.StandardLength.HasValue && product.StandardLength.Value > 0m
                                ? product.StandardLength.Value
                                : ii.Item.Length;
                            var unitWidth = product.StandardWidth.HasValue && product.StandardWidth.Value > 0m
                                ? product.StandardWidth.Value
                                : ii.Item.Width;

                            decimal unitHeight;
                            if (isBag)
                            {
                                unitHeight = product.StandardHeight.HasValue && product.StandardHeight.Value > 0m
                                    ? product.StandardHeight.Value
                                    : ii.Item.Height / 2m;
                            }
                            else
                            {
                                unitHeight = product.StandardHeight.HasValue && product.StandardHeight.Value > 0m
                                    ? product.StandardHeight.Value
                                    : ii.Item.Height;
                            }

                            if (unitLength <= 0m)
                            {
                                unitLength = palletLength;
                            }
                            if (unitWidth <= 0m)
                            {
                                unitWidth = palletWidth;
                            }
                            if (unitHeight <= 0m)
                            {
                                unitHeight = ii.Item.Height > 0m ? ii.Item.Height : 0.1m;
                            }

                            var maxPerRow = Math.Max(1, (int)Math.Floor((double)(palletLength / unitLength)));
                            var maxPerCol = Math.Max(1, (int)Math.Floor((double)(palletWidth / unitWidth)));
                            var perLayer = Math.Max(1, maxPerRow * maxPerCol);

                            var halfPalL = palletLength / 2m;
                            var halfPalW = palletWidth / 2m;
                            var halfL = unitLength / 2m;
                            var halfW = unitWidth / 2m;

                            decimal maxTopRelToPallet = 0m;

                            for (var idx = 0; idx < quantity; idx++)
                            {
                                var layer = idx / perLayer;
                                var posInLayer = idx % perLayer;
                                var row = posInLayer / maxPerRow;
                                var col = posInLayer % maxPerRow;

                                var xStart = -palletLength / 2m + unitLength / 2m;
                                var zStart = -palletWidth / 2m + unitWidth / 2m;

                                decimal dx = 0m;
                                decimal rotY = 0m;

                                if (pattern == "brick")
                                {
                                    dx = (layer % 2 == 1) ? unitLength / 2m : 0m;
                                }
                                else if (pattern == "cross")
                                {
                                    if (((row + col) % 2) == 1)
                                    {
                                        rotY = (decimal)(Math.PI / 2.0);
                                    }
                                }

                                var localX = xStart + col * unitLength + dx;
                                var localZ = zStart + row * unitWidth;

                                // Clamp vị trí trong phạm vi pallet (đặc biệt khi có dx cho pattern brick)
                                if (localX > halfPalL - halfL) localX = halfPalL - halfL;
                                if (localX < -halfPalL + halfL) localX = -halfPalL + halfL;
                                if (localZ > halfPalW - halfW) localZ = halfPalW - halfW;
                                if (localZ < -halfPalW + halfW) localZ = -halfPalW + halfW;

                                var localY = palletHeight + unitHeight / 2m + layer * unitHeight;

                                // Đỉnh khối hàng so với mặt pallet
                                var topRelToPallet = localY + unitHeight / 2m - palletHeight;
                                if (topRelToPallet > maxTopRelToPallet)
                                {
                                    maxTopRelToPallet = topRelToPallet;
                                }

                                var entity = new InboundItemStackUnit
                                {
                                    InboundItemId = ii.InboundItemId,
                                    UnitIndex = idx,
                                    LocalX = localX,
                                    LocalY = localY,
                                    LocalZ = localZ,
                                    Length = unitLength,
                                    Width = unitWidth,
                                    Height = unitHeight,
                                    RotationY = rotY
                                };

                                _context.InboundItemStackUnits.Add(entity);
                            }

                            if (maxTopRelToPallet <= 0m)
                            {
                                maxTopRelToPallet = ii.Item.Height;
                            }

                            stackGoodsHeights[ii.InboundItemId] = maxTopRelToPallet;
                        }
                    }
                }

                // Logic tương tự OptimizeInboundLayout nhưng lưu vào database
                // Lấy các khu vực thuộc về customer trong kho này
                // Nếu phiếu đã gắn với một Zone cụ thể, chỉ lấy đúng zone đó
                var zonesQuery = _context.WarehouseZones
                    .Where(z => z.WarehouseId == receipt.WarehouseId && z.CustomerId == receipt.CustomerId);

                if (receipt.ZoneId.HasValue)
                {
                    zonesQuery = zonesQuery.Where(z => z.ZoneId == receipt.ZoneId.Value);
                }

                var zones = zonesQuery.ToList();

                if (!zones.Any())
                {
                    transaction.Rollback();
                    return ApiResponse<object>.Fail(
                        "Khách hàng chưa có khu vực nào trong kho để xếp pallet",
                        "NO_ZONES_FOR_CUSTOMER",
                        null,
                        400
                    );
                }

                var zoneIds = zones.Select(z => z.ZoneId).ToList();

                var racks = _context.Racks
                    .Include(r => r.Shelves)
                    .Where(r => zoneIds.Contains(r.ZoneId))
                    .ToList();

                var racksByZone = racks
                    .GroupBy(r => r.ZoneId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // Các pallet location hiện có trong các zone này
                var existingLocations = _context.PalletLocations
                    .Include(pl => pl.Pallet)
                    .Where(pl => zoneIds.Contains(pl.ZoneId))
                    .ToList();

                // Tính điểm ưu tiên zone dựa trên khoảng cách đến cổng ra và vị trí checkin của kho
                var warehouseEntity = receipt.Warehouse;
                var gates = _context.WarehouseGates
                    .Where(g => g.WarehouseId == receipt.WarehouseId)
                    .ToList();

                var exitGates = gates
                    .Where(g => string.Equals(g.GateType, "exit", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (!exitGates.Any())
                {
                    exitGates = gates;
                }

                decimal? checkinX = warehouseEntity.CheckinPositionX;
                decimal? checkinZ = warehouseEntity.CheckinPositionZ;

                decimal Distance2D(decimal x1, decimal z1, decimal x2, decimal z2)
                {
                    var dx = x1 - x2;
                    var dz = z1 - z2;
                    return (decimal)Math.Sqrt((double)(dx * dx + dz * dz));
                }

                var zoneScores = new Dictionary<int, decimal>();

                foreach (var z in zones)
                {
                    var centerX = z.PositionX + z.Length / 2m;
                    var centerZ = z.PositionZ + z.Width / 2m;

                    decimal distToExit = 0m;
                    if (exitGates.Any())
                    {
                        distToExit = exitGates
                            .Select(g => Distance2D(centerX, centerZ, g.PositionX, g.PositionZ))
                            .DefaultIfEmpty(0m)
                            .Min();
                    }

                    decimal distToCheckin = 0m;
                    if (checkinX.HasValue && checkinZ.HasValue)
                    {
                        distToCheckin = Distance2D(centerX, centerZ, checkinX.Value, checkinZ.Value);
                    }

                    decimal score;

                    if (checkinX.HasValue && checkinZ.HasValue)
                    {
                        // Ưu tiên gần bàn checkin hơn, sau đó đến cổng ra
                        score = distToCheckin * 0.7m + distToExit * 0.3m;
                    }
                    else
                    {
                        // Nếu chưa cấu hình vị trí checkin thì chỉ dùng khoảng cách tới cổng ra
                        score = distToExit;
                    }

                    zoneScores[z.ZoneId] = score;
                }

                IEnumerable<WarehouseZone> OrderZonesByProximity(IEnumerable<WarehouseZone> source)
                {
                    if (!zoneScores.Any())
                    {
                        return source.OrderBy(z => z.PositionZ).ThenBy(z => z.PositionX);
                    }

                    return source
                        .OrderBy(z => zoneScores.TryGetValue(z.ZoneId, out var s) ? s : 0m)
                        .ThenBy(z => z.PositionZ)
                        .ThenBy(z => z.PositionX);
                }

                // Parse allowed_item_types
                var warehouseType = receipt.Warehouse.WarehouseType.ToLower();
                List<string> allowedItemTypes;

                if (!string.IsNullOrWhiteSpace(receipt.Warehouse.AllowedItemTypes))
                {
                    try
                    {
                        allowedItemTypes = JsonSerializer
                            .Deserialize<List<string>>(receipt.Warehouse.AllowedItemTypes)!
                            .Select(t => t.ToLower())
                            .Distinct()
                            .ToList();
                    }
                    catch
                    {
                        allowedItemTypes = warehouseType switch
                        {
                            "small" => new List<string> { "bag" },
                            "medium" => new List<string> { "bag", "box" },
                            "large" => new List<string> { "bag", "box", "custom" },
                            _ => new List<string> { "bag", "box", "custom" }
                        };
                    }
                }
                else
                {
                    allowedItemTypes = warehouseType switch
                    {
                        "small" => new List<string> { "bag" },
                        "medium" => new List<string> { "bag", "box" },
                        "large" => new List<string> { "bag", "box", "custom" },
                        _ => new List<string> { "bag", "box", "custom" }
                    };
                }

                // Preferred layout từ FE
                var preferredLayouts = request?.PreferredLayouts ?? new List<PreferredPalletLayoutDto>();
                var preferredDict = preferredLayouts
                    .GroupBy(p => p.PalletId)
                    .ToDictionary(g => g.Key, g => g.Min(x => x.Priority ?? 0m));
                var preferredLayoutMap = preferredLayouts
                    .GroupBy(p => p.PalletId)
                    .ToDictionary(g => g.Key, g => g.First());
                var forceUsePreferred = request?.ForceUsePreferredLayout ?? false;

                IEnumerable<InboundItem> orderedItems;
                if (forceUsePreferred && preferredDict.Any())
                {
                    var preferredItems = receipt.InboundItems
                        .Where(ii => preferredDict.ContainsKey(ii.PalletId))
                        .OrderBy(ii => preferredDict[ii.PalletId])
                        .ToList();
                    var otherItems = receipt.InboundItems
                        .Where(ii => !preferredDict.ContainsKey(ii.PalletId))
                        .OrderByDescending(ii => ii.Item.IsHeavy)
                        .ThenBy(ii => ii.Item.PriorityLevel)
                        .ThenByDescending(ii => ii.Item.Weight ?? 0);
                    orderedItems = preferredItems.Concat(otherItems);
                }
                else
                {
                    orderedItems = receipt.InboundItems
                        .OrderBy(ii => preferredDict.ContainsKey(ii.PalletId) ? preferredDict[ii.PalletId] : 0m)
                        .ThenByDescending(ii => ii.Item.IsHeavy)
                        .ThenBy(ii => ii.Item.PriorityLevel)
                        .ThenByDescending(ii => ii.Item.Weight ?? 0);
                }

                var inboundItems = orderedItems.ToList();

                // Tập pallet chứa hàng không được xếp chồng (ví dụ kính, thủy tinh)
                var nonStackablePalletIds = inboundItems
                    .Where(ii => ii.Item.IsNonStackable == true)
                    .Select(ii => ii.PalletId)
                    .Distinct()
                    .ToHashSet();

                // Kiểm tra pallet đã được gán location chưa
                var inboundPalletIds = inboundItems.Select(ii => ii.PalletId).Distinct().ToList();
                var alreadyLocated = _context.PalletLocations
                    .Where(pl => inboundPalletIds.Contains(pl.PalletId))
                    .Select(pl => pl.PalletId)
                    .Distinct()
                    .ToList();

                if (alreadyLocated.Any())
                {
                    transaction.Rollback();
                    return ApiResponse<object>.Fail(
                        "Một hoặc nhiều pallet trong yêu cầu đã được xếp trong kho",
                        "PALLET_ALREADY_ALLOCATED",
                        null,
                        400
                    );
                }

                var newLocations = new List<PalletLocation>();

                // Hàm kiểm tra overlap
                bool IsOverlap(decimal x1, decimal z1, decimal l1, decimal w1, decimal x2, decimal z2, decimal l2, decimal w2)
                {
                    return x1 < x2 + l2 && x1 + l1 > x2 && z1 < z2 + w2 && z1 + w1 > z2;
                }

                // Logic xếp pallet (giống OptimizeInboundLayout nhưng lưu vào DB ngay)
                foreach (var ii in inboundItems)
                {
                    var pallet = ii.Pallet;
                    var itemType = ii.Item.ItemType?.ToLower() ?? "box";
                    var itemIsBag = itemType == "bag";
                    var itemIsBox = itemType == "box";

                    if (!allowedItemTypes.Contains(itemType))
                    {
                        transaction.Rollback();
                        return ApiResponse<object>.Fail(
                            $"Loại hàng '{itemType}' không được phép trong kho này",
                            "ITEM_TYPE_NOT_ALLOWED_IN_WAREHOUSE",
                            null,
                            400
                        );
                    }

                    if (warehouseType == "small" && !itemIsBag)
                    {
                        transaction.Rollback();
                        return ApiResponse<object>.Fail(
                            "Kho loại small chỉ chứa hàng bao",
                            "WAREHOUSE_SMALL_BAG_ONLY",
                            null,
                            400
                        );
                    }

                    var candidateZones = itemIsBox
                        ? zones.Where(z => z.ZoneType == "rack").ToList()
                        : zones;

                    if (!candidateZones.Any())
                    {
                        transaction.Rollback();
                        return ApiResponse<object>.Fail(
                            "Không có khu vực phù hợp trong kho cho loại hàng này",
                            "NO_COMPATIBLE_ZONES",
                            null,
                            400
                        );
                    }

                    bool placed = false;

                    // Thử xếp theo preferred layout nếu có
                    if (forceUsePreferred && preferredLayoutMap.TryGetValue(pallet.PalletId, out var prefLayout))
                    {
                        if (itemIsBox && prefLayout.ZoneId.HasValue && prefLayout.ShelfId.HasValue 
                            && prefLayout.PositionX.HasValue && prefLayout.PositionZ.HasValue)
                        {
                            var zone = candidateZones.FirstOrDefault(z => z.ZoneId == prefLayout.ZoneId.Value);
                            if (zone != null && racksByZone.TryGetValue(zone.ZoneId, out var zoneRacks) && zoneRacks.Any())
                            {
                                var rack = zoneRacks.FirstOrDefault(r => r.Shelves.Any(s => s.ShelfId == prefLayout.ShelfId.Value));
                                var shelf = rack?.Shelves.FirstOrDefault(s => s.ShelfId == prefLayout.ShelfId.Value);

                                if (rack != null && shelf != null)
                                {
                                    var palletHeight = pallet.Height;

                                    if (!stackGoodsHeights.TryGetValue(ii.InboundItemId, out var itemStackHeight))
                                    {
                                        itemStackHeight = ii.Item.Height;
                                    }

                                    var clearHeight = GetShelfClearHeight(rack, shelf);

                                    if (palletHeight + itemStackHeight > clearHeight)
                                    {
                                        continue;
                                    }

                                    var shelfX = rack.PositionX;
                                    var shelfZ = rack.PositionZ;
                                    var shelfLength = Math.Min(rack.Length, shelf.Length);
                                    var shelfWidth = Math.Min(rack.Width, shelf.Width);
                                    var x = prefLayout.PositionX.Value;
                                    var z = prefLayout.PositionZ.Value;

                                    if (x >= shelfX && x + pallet.Length <= shelfX + shelfLength &&
                                        z >= shelfZ && z + pallet.Width <= shelfZ + shelfWidth)
                                    {
                                        var shelfExisting = existingLocations.Where(l => l.ShelfId == shelf.ShelfId).ToList();
                                        var shelfNew = newLocations.Where(l => l.ShelfId == shelf.ShelfId).ToList();
                                        bool collision = false;

                                        foreach (var loc in shelfExisting.Concat(shelfNew))
                                        {
                                            var otherPallet = loc.Pallet;
                                            if (IsOverlap(x, z, pallet.Length, pallet.Width,
                                                    loc.PositionX, loc.PositionZ, otherPallet.Length, otherPallet.Width))
                                            {
                                                collision = true;
                                                break;
                                            }
                                        }

                                        if (!collision)
                                        {
                                            var location = new PalletLocation
                                            {
                                                PalletId = pallet.PalletId,
                                                ZoneId = zone.ZoneId,
                                                ShelfId = shelf.ShelfId,
                                                PositionX = x,
                                                PositionY = shelf.PositionY,
                                                PositionZ = z,
                                                StackLevel = 1,
                                                StackedOnPallet = null,
                                                IsGround = false,
                                                AssignedAt = DateTime.Now
                                            };

                                            newLocations.Add(location);
                                            _context.PalletLocations.Add(location);
                                            placed = true;
                                        }
                                    }
                                }
                            }
                        }
                        else if (!itemIsBox && prefLayout.ZoneId.HasValue 
                            && prefLayout.PositionX.HasValue && prefLayout.PositionZ.HasValue)
                        {
                            var zone = candidateZones.FirstOrDefault(z => z.ZoneId == prefLayout.ZoneId.Value);
                            if (zone != null)
                            {
                                var maxX = zone.PositionX + zone.Length;
                                var maxZ = zone.PositionZ + zone.Width;
                                var x = prefLayout.PositionX.Value;
                                var z = prefLayout.PositionZ.Value;

                                if (x >= zone.PositionX && x + pallet.Length <= maxX &&
                                    z >= zone.PositionZ && z + pallet.Width <= maxZ)
                                {
                                    var zoneExisting = existingLocations.Where(l => l.ZoneId == zone.ZoneId).ToList();
                                    var zoneNew = newLocations.Where(l => l.ZoneId == zone.ZoneId).ToList();
                                    bool collision = false;

                                    foreach (var loc in zoneExisting.Concat(zoneNew))
                                    {
                                        var otherPallet = loc.Pallet;
                                        if (IsOverlap(x, z, pallet.Length, pallet.Width,
                                                loc.PositionX, loc.PositionZ, otherPallet.Length, otherPallet.Width))
                                        {
                                            collision = true;
                                            break;
                                        }
                                    }

                                    if (!collision)
                                    {
                                        var location = new PalletLocation
                                        {
                                            PalletId = pallet.PalletId,
                                            ZoneId = zone.ZoneId,
                                            ShelfId = null,
                                            PositionX = x,
                                            PositionY = zone.PositionY,
                                            PositionZ = z,
                                            StackLevel = 1,
                                            StackedOnPallet = null,
                                            IsGround = true,
                                            AssignedAt = DateTime.Now
                                        };

                                        newLocations.Add(location);
                                        _context.PalletLocations.Add(location);
                                        placed = true;
                                    }
                                }
                            }
                        }
                    }

                    if (placed) continue;

                    // Thuật toán quét tự động (giống OptimizeInboundLayout)
                    if (itemIsBox)
                    {
                        foreach (var zone in OrderZonesByProximity(candidateZones))
                        {
                            if (!racksByZone.TryGetValue(zone.ZoneId, out var zoneRacks) || !zoneRacks.Any())
                                continue;

                            foreach (var rack in zoneRacks.OrderBy(r => r.PositionZ).ThenBy(r => r.PositionX))
                            {
                                foreach (var shelf in rack.Shelves.OrderBy(s => s.ShelfLevel))
                                {
                                    var shelfExisting = existingLocations.Where(l => l.ShelfId == shelf.ShelfId).ToList();
                                    var shelfNew = newLocations.Where(l => l.ShelfId == shelf.ShelfId).ToList();

                                    var palletHeight = pallet.Height;

                                    if (!stackGoodsHeights.TryGetValue(ii.InboundItemId, out var itemStackHeight))
                                    {
                                        itemStackHeight = ii.Item.Height;
                                    }

                                    var clearHeight = GetShelfClearHeight(rack, shelf);

                                    if (palletHeight + itemStackHeight > clearHeight)
                                    {
                                        continue;
                                    }

                                    var shelfX = rack.PositionX;
                                    var shelfZ = rack.PositionZ;
                                    var shelfLength = Math.Min(rack.Length, shelf.Length);
                                    var shelfWidth = Math.Min(rack.Width, shelf.Width);

                                    if (pallet.Length > shelfLength || pallet.Width > shelfWidth)
                                        continue;

                                    var stepX = pallet.Length;
                                    var stepZ = pallet.Width;
                                    var maxX = shelfX + shelfLength;
                                    var maxZ = shelfZ + shelfWidth;

                                    for (decimal x = shelfX; x + pallet.Length <= maxX && !placed; x += stepX)
                                    {
                                        for (decimal z = shelfZ; z + pallet.Width <= maxZ && !placed; z += stepZ)
                                        {
                                            bool collision = false;
                                            foreach (var loc in shelfExisting.Concat(shelfNew))
                                            {
                                                var otherPallet = loc.Pallet;
                                                if (IsOverlap(x, z, pallet.Length, pallet.Width,
                                                        loc.PositionX, loc.PositionZ, otherPallet.Length, otherPallet.Width))
                                                {
                                                    collision = true;
                                                    break;
                                                }
                                            }

                                            if (!collision)
                                            {
                                                var location = new PalletLocation
                                                {
                                                    PalletId = pallet.PalletId,
                                                    ZoneId = zone.ZoneId,
                                                    ShelfId = shelf.ShelfId,
                                                    PositionX = x,
                                                    PositionY = shelf.PositionY,
                                                    PositionZ = z,
                                                    StackLevel = 1,
                                                    StackedOnPallet = null,
                                                    IsGround = false,
                                                    AssignedAt = DateTime.Now
                                                };

                                                newLocations.Add(location);
                                                _context.PalletLocations.Add(location);
                                                placed = true;
                                            }
                                        }
                                    }
                                    if (placed) break;
                                }
                                if (placed) break;
                            }
                            if (placed) break;
                        }

                        if (!placed)
                        {
                            transaction.Rollback();
                            return ApiResponse<object>.Fail(
                                "Không đủ không gian trống trong các khu vực của khách hàng để xếp tất cả pallet",
                                "NO_SPACE_FOR_PALLET",
                                null,
                                400
                            );
                        }
                    }
                    else
                    {
                        // Ground placement
                        foreach (var zone in OrderZonesByProximity(candidateZones))
                        {
                            var zoneExisting = existingLocations.Where(l => l.ZoneId == zone.ZoneId).ToList();
                            var zoneNew = newLocations.Where(l => l.ZoneId == zone.ZoneId).ToList();
                            var maxX = zone.PositionX + zone.Length;
                            var maxZ = zone.PositionZ + zone.Width;

                            if (pallet.Length > zone.Length || pallet.Width > zone.Width)
                                continue;

                            var stepX = pallet.Length;
                            var stepZ = pallet.Width;

                            for (decimal x = zone.PositionX; x + pallet.Length <= maxX && !placed; x += stepX)
                            {
                                for (decimal z = zone.PositionZ; z + pallet.Width <= maxZ && !placed; z += stepZ)
                                {
                                    bool collision = false;
                                    foreach (var loc in zoneExisting.Concat(zoneNew))
                                    {
                                        var otherPallet = loc.Pallet;
                                        if (IsOverlap(x, z, pallet.Length, pallet.Width,
                                                loc.PositionX, loc.PositionZ, otherPallet.Length, otherPallet.Width))
                                        {
                                            collision = true;
                                            break;
                                        }
                                    }

                                    if (!collision)
                                    {
                                        var location = new PalletLocation
                                        {
                                            PalletId = pallet.PalletId,
                                            ZoneId = zone.ZoneId,
                                            ShelfId = null,
                                            PositionX = x,
                                            PositionY = zone.PositionY,
                                            PositionZ = z,
                                            StackLevel = 1,
                                            StackedOnPallet = null,
                                            IsGround = true,
                                            AssignedAt = DateTime.Now
                                        };

                                        newLocations.Add(location);
                                        _context.PalletLocations.Add(location);
                                        placed = true;
                                    }
                                }
                            }
                            if (placed) break;
                        }

                        // Nếu không xếp được, thử xếp chồng
                        if (!placed)
                        {
                            bool stacked = false;
                            foreach (var zone in OrderZonesByProximity(candidateZones))
                            {
                                var zoneExisting = existingLocations.Where(l => l.ZoneId == zone.ZoneId && l.IsGround == true && l.StackLevel == 1).ToList();
                                var zoneNew = newLocations.Where(l => l.ZoneId == zone.ZoneId && l.IsGround == true && l.StackLevel == 1).ToList();

                                foreach (var baseLoc in zoneExisting.Concat(zoneNew))
                                {
                                    var basePallet = baseLoc.Pallet;

                                    // Không xếp chồng nếu pallet dưới hoặc pallet trên thuộc loại không được xếp chồng
                                    if (nonStackablePalletIds.Contains(basePallet.PalletId) || nonStackablePalletIds.Contains(pallet.PalletId))
                                        continue;

                                    if (pallet.Length > basePallet.Length || pallet.Width > basePallet.Width)
                                        continue;

                                    bool hasTop = existingLocations.Any(l => l.StackedOnPallet == basePallet.PalletId)
                                        || newLocations.Any(l => l.StackedOnPallet == basePallet.PalletId);
                                    if (hasTop) continue;

                                    var baseHeight = basePallet.Height;
                                    var newHeight = pallet.Height;
                                    var totalPalletHeight = baseHeight + newHeight;
                                    var baseMaxStack = basePallet.MaxStackHeight;
                                    var newMaxStack = pallet.MaxStackHeight;
                                    var allowedStackHeight = Math.Min(baseMaxStack, newMaxStack);

                                    if (totalPalletHeight > 1.5m || totalPalletHeight > allowedStackHeight)
                                        continue;

                                    var stackedLocation = new PalletLocation
                                    {
                                        PalletId = pallet.PalletId,
                                        ZoneId = baseLoc.ZoneId,
                                        ShelfId = baseLoc.ShelfId,
                                        PositionX = baseLoc.PositionX,
                                        PositionY = baseLoc.PositionY + baseHeight,
                                        PositionZ = baseLoc.PositionZ,
                                        StackLevel = 2,
                                        StackedOnPallet = basePallet.PalletId,
                                        IsGround = false,
                                        AssignedAt = DateTime.Now
                                    };

                                    newLocations.Add(stackedLocation);
                                    _context.PalletLocations.Add(stackedLocation);
                                    stacked = true;
                                    break;
                                }
                                if (stacked) break;
                            }

                            if (!stacked)
                            {
                                transaction.Rollback();
                                return ApiResponse<object>.Fail(
                                    "Không đủ không gian trống trong các khu vực của khách hàng để xếp tất cả pallet (kể cả xếp chồng)",
                                    "NO_SPACE_FOR_PALLET",
                                    null,
                                    400
                                );
                            }
                        }
                    }
                }

                // Tạo ItemAllocation cho mỗi inbound item
                foreach (var ii in inboundItems)
                {
                    var allocation = new ItemAllocation
                    {
                        ItemId = ii.ItemId,
                        PalletId = ii.PalletId,
                        PositionX = 0,
                        PositionY = 0,
                        PositionZ = 0,
                        AllocatedAt = DateTime.Now
                    };

                    _context.ItemAllocations.Add(allocation);
                }

                // Cập nhật trạng thái phiếu
                receipt.Status = "completed";
                _context.SaveChanges();

                transaction.Commit();

                try
                {
                    var printData = GetInboundReceiptPrintData(receipt.ReceiptId, accountId, role);
                    if (printData.Success && printData.Data != null)
                    {
                        TryGenerateInboundReceiptPdf(printData.Data);
                    }
                }
                catch
                {
                    // Bỏ qua lỗi sinh PDF để không ảnh hưởng đến kết quả duyệt
                }

                return ApiResponse<object>.Ok(
                    new { ReceiptId = receipt.ReceiptId, Status = receipt.Status },
                    "Duyệt yêu cầu và xếp pallet vào kho thành công"
                );
            }
            catch (DbUpdateException ex)
            {
                return ApiResponse<object>.Fail(
                    $"Lỗi khi duyệt yêu cầu: {ex.InnerException?.Message ?? ex.Message}",
                    "DATABASE_ERROR",
                    null,
                    500
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail(
                    $"Lỗi khi duyệt yêu cầu: {ex.Message}",
                    "INTERNAL_ERROR",
                    null,
                    500
                );
            }
        }

        public ApiResponse<List<InboundRequestListViewModel>> GetInboundRequests(int? accountId, string role, int? warehouseId, int? zoneId, string? status)
        {
            try
            {
                if (accountId == null)
                {
                    return ApiResponse<List<InboundRequestListViewModel>>.Fail(
                        "Token không hợp lệ",
                        "UNAUTHORIZED",
                        null,
                        401
                    );
                }

                var query = _context.InboundReceipts
                    .Include(r => r.Warehouse)
                    .Include(r => r.Customer)
                    .Include(r => r.CreatedByNavigation)
                    .AsQueryable();

                // Nếu là customer, chỉ lấy requests của họ
                if (role == "customer")
                {
                    query = query.Where(r => r.CustomerId == accountId.Value);
                }
                // Nếu là warehouse_owner, có thể lọc theo warehouse của họ
                else if (role == "warehouse_owner")
                {
                    var ownerWarehouses = _context.Warehouses
                        .Where(w => w.OwnerId == accountId.Value)
                        .Select(w => w.WarehouseId)
                        .ToList();
                    query = query.Where(r => ownerWarehouses.Contains(r.WarehouseId));
                }
                // Admin thấy tất cả

                // Filter theo warehouse
                if (warehouseId.HasValue)
                {
                    query = query.Where(r => r.WarehouseId == warehouseId.Value);
                }

                // Filter theo zone (nếu có) để phân biệt rõ từng khu vực trong cùng 1 kho
                if (zoneId.HasValue)
                {
                    query = query.Where(r => r.ZoneId == zoneId.Value);
                }

                // Filter theo status
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(r => r.Status == status);
                }

                var baseReceipts = query
                    .OrderByDescending(r => r.InboundDate)
                    .ToList();

                // Lấy tên zone tương ứng (nếu có)
                var zoneIdList = baseReceipts
                    .Where(r => r.ZoneId.HasValue)
                    .Select(r => r.ZoneId!.Value)
                    .Distinct()
                    .ToList();

                var zonesDict = _context.WarehouseZones
                    .Where(z => zoneIdList.Contains(z.ZoneId))
                    .ToDictionary(z => z.ZoneId, z => z.ZoneName);

                var requests = baseReceipts
                    .Select(r => new InboundRequestListViewModel
                    {
                        ReceiptId = r.ReceiptId,
                        ReceiptNumber = r.ReceiptNumber,
                        WarehouseId = r.WarehouseId,
                        WarehouseName = r.Warehouse.WarehouseName,
                        ZoneId = r.ZoneId,
                        ZoneName = r.ZoneId.HasValue && zonesDict.ContainsKey(r.ZoneId.Value)
                            ? zonesDict[r.ZoneId.Value]
                            : null,
                        CustomerId = r.CustomerId,
                        CustomerName = r.Customer.FullName,
                        TotalItems = r.TotalItems,
                        TotalPallets = r.TotalPallets,
                        InboundDate = r.InboundDate,
                        Status = r.Status,
                        Notes = r.Notes,
                        CreatedByName = r.CreatedByNavigation.FullName,
                        StackMode = r.StackMode
                    })
                    .ToList();

                return ApiResponse<List<InboundRequestListViewModel>>.Ok(
                    requests,
                    "Lấy danh sách yêu cầu gửi hàng thành công"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<List<InboundRequestListViewModel>>.Fail(
                    $"Lỗi khi lấy danh sách yêu cầu: {ex.Message}",
                    "INTERNAL_ERROR",
                    null,
                    500
                );
            }
        }

        public ApiResponse<InboundRequestDetailViewModel> GetInboundRequestDetail(int receiptId, int? accountId, string role)
        {
            try
            {
                if (accountId == null)
                {
                    return ApiResponse<InboundRequestDetailViewModel>.Fail(
                        "Token không hợp lệ",
                        "UNAUTHORIZED",
                        null,
                        401
                    );
                }

                var receipt = _context.InboundReceipts
                    .Include(r => r.Warehouse)
                    .Include(r => r.Customer)
                    .Include(r => r.CreatedByNavigation)
                    .Include(r => r.InboundItems)
                        .ThenInclude(ii => ii.Item)
                            .ThenInclude(i => i.Product)
                    .Include(r => r.InboundItems)
                        .ThenInclude(ii => ii.Pallet)
                    .FirstOrDefault(r => r.ReceiptId == receiptId);

                if (receipt == null)
                {
                    return ApiResponse<InboundRequestDetailViewModel>.Fail(
                        "Không tìm thấy yêu cầu gửi hàng",
                        "NOT_FOUND",
                        null,
                        404
                    );
                }

                // Kiểm tra quyền truy cập
                if (role == "customer" && receipt.CustomerId != accountId.Value)
                {
                    return ApiResponse<InboundRequestDetailViewModel>.Fail(
                        "Bạn không có quyền xem yêu cầu này",
                        "FORBIDDEN",
                        null,
                        403
                    );
                }

                string? zoneName = null;
                if (receipt.ZoneId.HasValue)
                {
                    zoneName = _context.WarehouseZones
                        .Where(z => z.ZoneId == receipt.ZoneId.Value)
                        .Select(z => z.ZoneName)
                        .FirstOrDefault();
                }

                var detail = new InboundRequestDetailViewModel
                {
                    ReceiptId = receipt.ReceiptId,
                    ReceiptNumber = receipt.ReceiptNumber,
                    WarehouseId = receipt.WarehouseId,
                    WarehouseName = receipt.Warehouse.WarehouseName,
                    ZoneId = receipt.ZoneId,
                    ZoneName = zoneName,
                    CustomerId = receipt.CustomerId,
                    CustomerName = receipt.Customer.FullName,
                    TotalItems = receipt.TotalItems,
                    TotalPallets = receipt.TotalPallets,
                    InboundDate = receipt.InboundDate,
                    Status = receipt.Status,
                    Notes = receipt.Notes,
                    CreatedByName = receipt.CreatedByNavigation.FullName,
                    StackMode = receipt.StackMode,
                    Items = receipt.InboundItems.Select(ii => new InboundItemDetailViewModel
                    {
                        InboundItemId = ii.InboundItemId,
                        ItemId = ii.ItemId,
                        QrCode = ii.Item.QrCode,
                        ItemName = ii.Item.ItemName,
                        PalletId = ii.PalletId,
                        PalletBarcode = ii.Pallet.Barcode,
                        ProductId = ii.Item.ProductId,
                        ProductName = ii.Item.Product.ProductName,
                        ProductCode = ii.Item.Product.ProductCode,
                        Quantity = ii.Quantity,
                        ManufacturingDate = ii.Item.ManufacturingDate.HasValue
                            ? ii.Item.ManufacturingDate.Value.ToDateTime(TimeOnly.MinValue)
                            : null,
                        ExpiryDate = ii.Item.ExpiryDate.HasValue
                            ? ii.Item.ExpiryDate.Value.ToDateTime(TimeOnly.MinValue)
                            : null,
                        BatchNumber = ii.Item.BatchNumber,
                        UnitPrice = ii.Item.UnitPrice,
                        TotalAmount = ii.Item.TotalAmount
                    }).ToList()
                };

                return ApiResponse<InboundRequestDetailViewModel>.Ok(
                    detail,
                    "Lấy chi tiết yêu cầu thành công"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<InboundRequestDetailViewModel>.Fail(
                    $"Lỗi khi lấy chi tiết yêu cầu: {ex.Message}",
                    "INTERNAL_ERROR",
                    null,
                    500
                );
            }
        }

        public ApiResponse<InboundReceiptPrintViewModel> GetInboundReceiptPrintData(int receiptId, int? accountId, string role)
        {
            try
            {
                if (accountId == null)
                {
                    return ApiResponse<InboundReceiptPrintViewModel>.Fail(
                        "Token không hợp lệ",
                        "UNAUTHORIZED",
                        null,
                        401
                    );
                }

                var receipt = _context.InboundReceipts
                    .Include(r => r.Warehouse)
                    .Include(r => r.Customer)
                    .Include(r => r.CreatedByNavigation)
                    .Include(r => r.InboundItems)
                        .ThenInclude(ii => ii.Item)
                            .ThenInclude(i => i.Product)
                    .Include(r => r.InboundItems)
                        .ThenInclude(ii => ii.Pallet)
                    .FirstOrDefault(r => r.ReceiptId == receiptId);

                if (receipt == null)
                {
                    return ApiResponse<InboundReceiptPrintViewModel>.Fail(
                        "Không tìm thấy yêu cầu gửi hàng",
                        "NOT_FOUND",
                        null,
                        404
                    );
                }

                if (role == "customer" && receipt.CustomerId != accountId.Value)
                {
                    return ApiResponse<InboundReceiptPrintViewModel>.Fail(
                        "Bạn không có quyền xem yêu cầu này",
                        "FORBIDDEN",
                        null,
                        403
                    );
                }

                string? zoneName = null;
                if (receipt.ZoneId.HasValue)
                {
                    zoneName = _context.WarehouseZones
                        .Where(z => z.ZoneId == receipt.ZoneId.Value)
                        .Select(z => z.ZoneName)
                        .FirstOrDefault();
                }

                var palletIds = receipt.InboundItems
                    .Select(ii => ii.PalletId)
                    .Distinct()
                    .ToList();

                var palletLocations = _context.PalletLocations
                    .Include(pl => pl.Zone)
                    .Where(pl => palletIds.Contains(pl.PalletId))
                    .ToList();

                var latestLocationByPallet = palletLocations
                    .GroupBy(pl => pl.PalletId)
                    .ToDictionary(
                        g => g.Key,
                        g => g
                            .OrderByDescending(x => x.AssignedAt ?? DateTime.MinValue)
                            .First()
                    );

                var palletGroups = receipt.InboundItems
                    .GroupBy(ii => ii.PalletId)
                    .ToList();

                var palletViewModels = new List<InboundReceiptPrintPalletViewModel>();

                foreach (var group in palletGroups)
                {
                    var firstItem = group.First();
                    var pallet = firstItem.Pallet;

                    latestLocationByPallet.TryGetValue(pallet.PalletId, out var loc);

                    var palletVm = new InboundReceiptPrintPalletViewModel
                    {
                        PalletId = pallet.PalletId,
                        PalletBarcode = pallet.Barcode,
                        ZoneId = loc?.ZoneId ?? receipt.ZoneId ?? 0,
                        ZoneName = loc?.Zone?.ZoneName ?? zoneName,
                        LocationCode = loc?.LocationCode,
                        PositionX = loc?.PositionX ?? 0,
                        PositionY = loc?.PositionY ?? 0,
                        PositionZ = loc?.PositionZ ?? 0,
                        IsGround = loc?.IsGround ?? false,
                        StackLevel = loc?.StackLevel ?? 1,
                        ShelfId = loc?.ShelfId,
                        PalletQrContent = $"PALLET|{pallet.PalletId}|{receipt.WarehouseId}",
                        Items = group.Select(ii => new InboundReceiptPrintItemViewModel
                        {
                            InboundItemId = ii.InboundItemId,
                            ItemId = ii.ItemId,
                            QrCode = ii.Item.QrCode,
                            ItemName = ii.Item.ItemName,
                            ProductId = ii.Item.ProductId,
                            ProductCode = ii.Item.Product.ProductCode,
                            ProductName = ii.Item.Product.ProductName,
                            Unit = ii.Item.Product.Unit,
                            Quantity = ii.Quantity,
                            ManufacturingDate = ii.Item.ManufacturingDate.HasValue
                                ? ii.Item.ManufacturingDate.Value.ToDateTime(TimeOnly.MinValue)
                                : null,
                            ExpiryDate = ii.Item.ExpiryDate.HasValue
                                ? ii.Item.ExpiryDate.Value.ToDateTime(TimeOnly.MinValue)
                                : null,
                            BatchNumber = ii.Item.BatchNumber,
                            UnitPrice = ii.Item.UnitPrice,
                            TotalAmount = ii.Item.TotalAmount
                        }).ToList()
                    };

                    palletViewModels.Add(palletVm);
                }

                var printViewModel = new InboundReceiptPrintViewModel
                {
                    ReceiptId = receipt.ReceiptId,
                    ReceiptNumber = receipt.ReceiptNumber,
                    Status = receipt.Status,
                    InboundDate = receipt.InboundDate,
                    WarehouseId = receipt.WarehouseId,
                    WarehouseName = receipt.Warehouse.WarehouseName,
                    ZoneId = receipt.ZoneId,
                    ZoneName = zoneName,
                    CustomerId = receipt.CustomerId,
                    CustomerName = receipt.Customer.FullName,
                    CreatedByName = receipt.CreatedByNavigation.FullName,
                    Notes = receipt.Notes,
                    TotalItems = receipt.TotalItems,
                    TotalPallets = receipt.TotalPallets,
                    StackMode = receipt.StackMode,
                    Pallets = palletViewModels
                };

                return ApiResponse<InboundReceiptPrintViewModel>.Ok(
                    printViewModel,
                    "Lấy dữ liệu in phiếu nhập kho thành công"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<InboundReceiptPrintViewModel>.Fail(
                    $"Lỗi khi lấy dữ liệu in phiếu nhập: {ex.Message}",
                    "INTERNAL_ERROR",
                    null,
                    500
                );
            }
        }

        public ApiResponse<object> UpdateInboundRequestStatus(int receiptId, int? accountId, string role, UpdateInboundRequestStatusRequest request)
        {
            try
            {
                if (accountId == null)
                {
                    return ApiResponse<object>.Fail(
                        "Token không hợp lệ",
                        "UNAUTHORIZED",
                        null,
                        401
                    );
                }

                var receipt = _context.InboundReceipts
                    .FirstOrDefault(r => r.ReceiptId == receiptId);

                if (receipt == null)
                {
                    return ApiResponse<object>.Fail(
                        "Không tìm thấy yêu cầu gửi hàng",
                        "NOT_FOUND",
                        null,
                        404
                    );
                }

                // Kiểm tra quyền: chỉ warehouse_owner hoặc admin mới được cập nhật status
                if (role == "customer")
                {
                    // Customer chỉ có thể cancel yêu cầu của chính họ
                    if (receipt.CustomerId != accountId.Value || request.Status != "cancelled")
                    {
                        return ApiResponse<object>.Fail(
                            "Bạn chỉ có thể hủy yêu cầu của chính mình",
                            "FORBIDDEN",
                            null,
                            403
                        );
                    }
                }

                // Cập nhật status
                receipt.Status = request.Status;
                if (!string.IsNullOrEmpty(request.Notes))
                {
                    receipt.Notes = string.IsNullOrEmpty(receipt.Notes)
                        ? request.Notes
                        : $"{receipt.Notes}\n{request.Notes}";
                }

                _context.SaveChanges();

                return ApiResponse<object>.Ok(
                    new { ReceiptId = receipt.ReceiptId, Status = receipt.Status },
                    "Cập nhật trạng thái thành công"
                );
            }
            catch (DbUpdateException ex)
            {
                return ApiResponse<object>.Fail(
                    $"Lỗi khi cập nhật trạng thái: {ex.InnerException?.Message ?? ex.Message}",
                    "DATABASE_ERROR",
                    null,
                    500
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail(
                    $"Lỗi khi cập nhật trạng thái: {ex.Message}",
                    "INTERNAL_ERROR",
                    null,
                    500
                );
            }
        }
    }
}

