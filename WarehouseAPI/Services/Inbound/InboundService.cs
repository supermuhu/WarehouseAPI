using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WarehouseAPI.Data;
using WarehouseAPI.ModelView.Common;
using WarehouseAPI.ModelView.Inbound;

namespace WarehouseAPI.Services.Inbound
{
    public class InboundService : IInboundService
    {
        private readonly WarehouseApiContext _context;

        public InboundService(WarehouseApiContext context)
        {
            _context = context;
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
                    customerId = accountId.Value; // Tạm thời dùng accountId
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

                    // Tạo inbound receipt
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
                        Status = "pending"
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
                            IsFragile = product.IsFragile ?? false,
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
                    Items = receipt.InboundItems.Select(ii =>
                    {
                        var product = ii.Item.Product;
                        var pallet = ii.Pallet;

                        // Heuristic xác định hàng bao
                        var unitLower = product.Unit.ToLower();
                        var categoryLower = product.Category?.ToLower();
                        bool isBag = unitLower.Contains("bao") || (categoryLower != null && categoryLower.Contains("bao"));

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
                            Quantity = ii.Quantity ?? 1,
                            UnitLength = product.StandardLength,
                            UnitWidth = product.StandardWidth,
                            UnitHeight = product.StandardHeight,
                            ItemLength = ii.Item.Length,
                            ItemWidth = ii.Item.Width,
                            ItemHeight = ii.Item.Height,
                            PalletLength = pallet.Length,
                            PalletWidth = pallet.Width,
                            PalletHeight = pallet.Height ?? 0.15m,
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
                bool hasBoxItems = receipt.InboundItems.Any(ii =>
                {
                    var t = ii.Item.ItemType?.ToLower() ?? "box";
                    return t == "box";
                });

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
                        .ThenBy(ii => ii.Item.PriorityLevel ?? 5)
                        .ThenByDescending(ii => ii.Item.Weight ?? 0);

                    orderedItems = preferredItems.Concat(otherItems);
                }
                else
                {
                    // Hành vi cũ: dùng priority nếu có, sau đó đến trọng lượng/priority level
                    orderedItems = receipt.InboundItems
                        .OrderBy(ii => preferredDict.ContainsKey(ii.PalletId) ? preferredDict[ii.PalletId] : 0m)
                        .ThenByDescending(ii => ii.Item.IsHeavy)
                        .ThenBy(ii => ii.Item.PriorityLevel ?? 5)
                        .ThenByDescending(ii => ii.Item.Weight ?? 0);
                }

                // Các inbound items cần xếp - sau khi áp dụng thứ tự ưu tiên
                var inboundItems = orderedItems.ToList();

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
                        foreach (var zone in candidateZones.OrderBy(z => z.PositionZ).ThenBy(z => z.PositionX))
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
                        foreach (var zone in candidateZones.OrderBy(z => z.PositionZ).ThenBy(z => z.PositionX))
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

                            foreach (var zone in candidateZones.OrderBy(z => z.PositionZ).ThenBy(z => z.PositionX))
                            {
                                var zoneExisting = existingLocations.Where(l => l.ZoneId == zone.ZoneId && l.IsGround == true && l.StackLevel == 1).ToList();
                                var zoneNew = newLocations.Where(l => l.ZoneId == zone.ZoneId && l.IsGround == true && l.StackLevel == 1).ToList();

                                foreach (var baseLoc in zoneExisting.Concat(zoneNew))
                                {
                                    var basePallet = baseLoc.Pallet;

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

                                    var baseHeight = basePallet.Height ?? 0.15m;
                                    var newHeight = pallet.Height ?? 0.15m;
                                    var totalPalletHeight = baseHeight + newHeight;

                                    var baseMaxStack = basePallet.MaxStackHeight ?? 1.5m;
                                    var newMaxStack = pallet.MaxStackHeight ?? 1.5m;
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
                        StackLevel = l.StackLevel ?? 1,
                        StackedOnPalletId = l.StackedOnPallet,
                        IsGround = l.IsGround ?? false
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
                        .ThenBy(ii => ii.Item.PriorityLevel ?? 5)
                        .ThenByDescending(ii => ii.Item.Weight ?? 0);
                    orderedItems = preferredItems.Concat(otherItems);
                }
                else
                {
                    orderedItems = receipt.InboundItems
                        .OrderBy(ii => preferredDict.ContainsKey(ii.PalletId) ? preferredDict[ii.PalletId] : 0m)
                        .ThenByDescending(ii => ii.Item.IsHeavy)
                        .ThenBy(ii => ii.Item.PriorityLevel ?? 5)
                        .ThenByDescending(ii => ii.Item.Weight ?? 0);
                }

                var inboundItems = orderedItems.ToList();

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
                        foreach (var zone in candidateZones.OrderBy(z => z.PositionZ).ThenBy(z => z.PositionX))
                        {
                            if (!racksByZone.TryGetValue(zone.ZoneId, out var zoneRacks) || !zoneRacks.Any())
                                continue;

                            foreach (var rack in zoneRacks.OrderBy(r => r.PositionZ).ThenBy(r => r.PositionX))
                            {
                                foreach (var shelf in rack.Shelves.OrderBy(s => s.ShelfLevel))
                                {
                                    var shelfExisting = existingLocations.Where(l => l.ShelfId == shelf.ShelfId).ToList();
                                    var shelfNew = newLocations.Where(l => l.ShelfId == shelf.ShelfId).ToList();
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
                        foreach (var zone in candidateZones.OrderBy(z => z.PositionZ).ThenBy(z => z.PositionX))
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
                            foreach (var zone in candidateZones.OrderBy(z => z.PositionZ).ThenBy(z => z.PositionX))
                            {
                                var zoneExisting = existingLocations.Where(l => l.ZoneId == zone.ZoneId && l.IsGround == true && l.StackLevel == 1).ToList();
                                var zoneNew = newLocations.Where(l => l.ZoneId == zone.ZoneId && l.IsGround == true && l.StackLevel == 1).ToList();

                                foreach (var baseLoc in zoneExisting.Concat(zoneNew))
                                {
                                    var basePallet = baseLoc.Pallet;
                                    if (pallet.Length > basePallet.Length || pallet.Width > basePallet.Width)
                                        continue;

                                    bool hasTop = existingLocations.Any(l => l.StackedOnPallet == basePallet.PalletId)
                                        || newLocations.Any(l => l.StackedOnPallet == basePallet.PalletId);
                                    if (hasTop) continue;

                                    var baseHeight = basePallet.Height ?? 0.15m;
                                    var newHeight = pallet.Height ?? 0.15m;
                                    var totalPalletHeight = baseHeight + newHeight;
                                    var baseMaxStack = basePallet.MaxStackHeight ?? 1.5m;
                                    var newMaxStack = pallet.MaxStackHeight ?? 1.5m;
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

        public ApiResponse<List<InboundRequestListViewModel>> GetInboundRequests(int? accountId, string role, int? warehouseId, string? status)
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
                        CreatedByName = r.CreatedByNavigation.FullName
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
                        Quantity = ii.Quantity ?? 1,
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

