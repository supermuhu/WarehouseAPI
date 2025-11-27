using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseAPI.Data;
using WarehouseAPI.Helpers;
using WarehouseAPI.ModelView.Common;
using WarehouseAPI.ModelView.Inbound;

namespace WarehouseAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class InboundController : ControllerBase
    {
        private readonly WarehouseApiContext _context;

        public InboundController(WarehouseApiContext context)
        {
            _context = context;
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
            try
            {
                var accountId = Utils.GetCurrentAccountId(User);
                var role = Utils.GetCurrentRole(User);

                if (accountId == null)
                {
                    var errorResult = ApiResponse<object>.Fail(
                        "Token không hợp lệ",
                        "UNAUTHORIZED",
                        null,
                        401
                    );
                    return Unauthorized(errorResult);
                }

                // Kiểm tra warehouse có tồn tại không
                var warehouse = _context.Warehouses
                    .FirstOrDefault(w => w.WarehouseId == request.WarehouseId && w.Status == "active");

                if (warehouse == null)
                {
                    var errorResult = ApiResponse<object>.Fail(
                        "Kho không tồn tại hoặc không hoạt động",
                        "WAREHOUSE_NOT_FOUND",
                        null,
                        404
                    );
                    return NotFound(errorResult);
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
                    var errorResult = ApiResponse<object>.Fail(
                        "Mỗi pallet chỉ được phép gắn một hàng hóa trong một yêu cầu",
                        "DUPLICATE_PALLET_ITEMS",
                        null,
                        400
                    );
                    return BadRequest(errorResult);
                }

                // Kiểm tra pallets có tồn tại và available không
                var pallets = _context.Pallets
                    .Where(p => palletIds.Contains(p.PalletId))
                    .ToList();

                if (pallets.Count != palletIds.Count)
                {
                    var errorResult = ApiResponse<object>.Fail(
                        "Một hoặc nhiều pallet không tồn tại",
                        "PALLET_NOT_FOUND",
                        null,
                        404
                    );
                    return NotFound(errorResult);
                }

                // Kiểm tra products có tồn tại và active không
                var products = _context.Products
                    .Where(p => productIds.Contains(p.ProductId) && p.Status == "active")
                    .ToList();

                if (products.Count != productIds.Count)
                {
                    var errorResult = ApiResponse<object>.Fail(
                        "Một hoặc nhiều product không tồn tại hoặc không hoạt động",
                        "PRODUCT_NOT_FOUND",
                        null,
                        404
                    );
                    return NotFound(errorResult);
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
                    var errorResult = ApiResponse<object>.Fail(
                        "Một hoặc nhiều product không thuộc về bạn hoặc admin",
                        "PRODUCT_ACCESS_DENIED",
                        null,
                        403
                    );
                    return StatusCode(403, errorResult);
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

                        var item = new Item
                        {
                            QrCode = qrCode,
                            ProductId = itemRequest.ProductId,
                            CustomerId = customerId,
                            ItemName = product.ProductName,
                            ItemType = "box", // Có thể lấy từ product hoặc để mặc định
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

                    var result = ApiResponse<object>.Ok(
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

                    return StatusCode(201, result);
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (DbUpdateException ex)
            {
                var result = ApiResponse<object>.Fail(
                    $"Lỗi khi tạo yêu cầu gửi hàng: {ex.InnerException?.Message ?? ex.Message}",
                    "DATABASE_ERROR",
                    null,
                    500
                );
                return StatusCode(500, result);
            }
            catch (Exception ex)
            {
                var result = ApiResponse<object>.Fail(
                    $"Lỗi khi tạo yêu cầu gửi hàng: {ex.Message}",
                    "INTERNAL_ERROR",
                    null,
                    500
                );
                return StatusCode(500, result);
            }
        }

        /// <summary>
        /// Lấy dữ liệu phục vụ màn hình duyệt yêu cầu (warehouse_owner)
        /// Bao gồm thông tin pallet, product, kích thước khối hàng và kích thước 1 đơn vị sản phẩm
        /// </summary>
        /// <param name="receiptId">ID phiếu inbound</param>
        [HttpGet("{receiptId}/approval-view")]
        public IActionResult GetInboundApprovalView(int receiptId)
        {
            try
            {
                var accountId = Utils.GetCurrentAccountId(User);
                var role = Utils.GetCurrentRole(User);

                if (accountId == null)
                {
                    var errorResult = ApiResponse<InboundApprovalViewModel>.Fail(
                        "Token không hợp lệ",
                        "UNAUTHORIZED",
                        null,
                        401
                    );
                    return Unauthorized(errorResult);
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
                    var errorResult = ApiResponse<InboundApprovalViewModel>.Fail(
                        "Không tìm thấy yêu cầu gửi hàng",
                        "NOT_FOUND",
                        null,
                        404
                    );
                    return NotFound(errorResult);
                }

                // Quyền: customer chỉ xem được yêu cầu của mình, owner/admin xem được kho của họ
                if (role == "customer" && receipt.CustomerId != accountId.Value)
                {
                    var errorResult = ApiResponse<InboundApprovalViewModel>.Fail(
                        "Bạn không có quyền xem yêu cầu này",
                        "FORBIDDEN",
                        null,
                        403
                    );
                    return StatusCode(403, errorResult);
                }

                var approval = new InboundApprovalViewModel
                {
                    ReceiptId = receipt.ReceiptId,
                    ReceiptNumber = receipt.ReceiptNumber,
                    WarehouseId = receipt.WarehouseId,
                    WarehouseName = receipt.Warehouse.WarehouseName,
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

                var result = ApiResponse<InboundApprovalViewModel>.Ok(
                    approval,
                    "Lấy dữ liệu duyệt yêu cầu thành công"
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                var result = ApiResponse<InboundApprovalViewModel>.Fail(
                    $"Lỗi khi lấy dữ liệu duyệt yêu cầu: {ex.Message}",
                    "INTERNAL_ERROR",
                    null,
                    500
                );
                return StatusCode(500, result);
            }
        }

        /// <summary>
        /// Duyệt yêu cầu inbound: sắp xếp pallet vào các khu vực của customer và tạo ItemAllocation
        /// Sau khi duyệt, yêu cầu sẽ chuyển sang trạng thái completed và xuất hiện trong kho 3D
        /// </summary>
        /// <param name="receiptId">ID phiếu inbound</param>
        [HttpPost("{receiptId}/approve")]
        public IActionResult ApproveInboundRequest(int receiptId)
        {
            try
            {
                var accountId = Utils.GetCurrentAccountId(User);
                var role = Utils.GetCurrentRole(User);

                if (accountId == null)
                {
                    var errorResult = ApiResponse<object>.Fail(
                        "Token không hợp lệ",
                        "UNAUTHORIZED",
                        null,
                        401
                    );
                    return Unauthorized(errorResult);
                }

                if (role != "warehouse_owner" && role != "admin")
                {
                    var errorResult = ApiResponse<object>.Fail(
                        "Chỉ chủ kho hoặc admin mới được duyệt yêu cầu",
                        "FORBIDDEN",
                        null,
                        403
                    );
                    return StatusCode(403, errorResult);
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
                    var errorResult = ApiResponse<object>.Fail(
                        "Không tìm thấy yêu cầu gửi hàng",
                        "NOT_FOUND",
                        null,
                        404
                    );
                    return NotFound(errorResult);
                }

                if (receipt.Status != "pending")
                {
                    var errorResult = ApiResponse<object>.Fail(
                        "Chỉ có thể duyệt các yêu cầu ở trạng thái pending",
                        "INVALID_STATUS",
                        null,
                        400
                    );
                    return BadRequest(errorResult);
                }

                // Lấy các khu vực thuộc về customer trong kho này
                var zones = _context.WarehouseZones
                    .Where(z => z.WarehouseId == receipt.WarehouseId && z.CustomerId == receipt.CustomerId)
                    .ToList();

                if (!zones.Any())
                {
                    var errorResult = ApiResponse<object>.Fail(
                        "Khách hàng chưa có khu vực nào trong kho để xếp pallet",
                        "NO_ZONES_FOR_CUSTOMER",
                        null,
                        400
                    );
                    return BadRequest(errorResult);
                }

                var zoneIds = zones.Select(z => z.ZoneId).ToList();

                // Các pallet location hiện có trong các zone này
                var existingLocations = _context.PalletLocations
                    .Include(pl => pl.Pallet)
                    .Where(pl => zoneIds.Contains(pl.ZoneId))
                    .ToList();

                // Các inbound items cần xếp
                var inboundItems = receipt.InboundItems.ToList();

                // Kiểm tra pallet đã được gán location chưa
                var inboundPalletIds = inboundItems.Select(ii => ii.PalletId).Distinct().ToList();
                var alreadyLocated = _context.PalletLocations
                    .Where(pl => inboundPalletIds.Contains(pl.PalletId))
                    .Select(pl => pl.PalletId)
                    .Distinct()
                    .ToList();

                if (alreadyLocated.Any())
                {
                    var errorResult = ApiResponse<object>.Fail(
                        "Một hoặc nhiều pallet trong yêu cầu đã được xếp trong kho",
                        "PALLET_ALREADY_ALLOCATED",
                        null,
                        400
                    );
                    return BadRequest(errorResult);
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
                    bool placed = false;

                    foreach (var zone in zones)
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
                                _context.PalletLocations.Add(location);
                                placed = true;
                            }
                        }

                        if (placed) break;
                    }

                    if (!placed)
                    {
                        var errorResult = ApiResponse<object>.Fail(
                            "Không đủ không gian trống trong các khu vực của khách hàng để xếp tất cả pallet",
                            "NO_SPACE_FOR_PALLET",
                            null,
                            400
                        );
                        return BadRequest(errorResult);
                    }
                }

                // Tạo ItemAllocation đơn giản: mỗi InboundItem tạo 1 allocation cho cả khối hàng trên pallet
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

                var okResult = ApiResponse<object>.Ok(
                    new { ReceiptId = receipt.ReceiptId, Status = receipt.Status },
                    "Duyệt yêu cầu và xếp pallet vào kho thành công"
                );

                return Ok(okResult);
            }
            catch (DbUpdateException ex)
            {
                var result = ApiResponse<object>.Fail(
                    $"Lỗi khi duyệt yêu cầu: {ex.InnerException?.Message ?? ex.Message}",
                    "DATABASE_ERROR",
                    null,
                    500
                );
                return StatusCode(500, result);
            }
            catch (Exception ex)
            {
                var result = ApiResponse<object>.Fail(
                    $"Lỗi khi duyệt yêu cầu: {ex.Message}",
                    "INTERNAL_ERROR",
                    null,
                    500
                );
                return StatusCode(500, result);
            }
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
            try
            {
                var accountId = Utils.GetCurrentAccountId(User);
                var role = Utils.GetCurrentRole(User);

                if (accountId == null)
                {
                    var errorResult = ApiResponse<List<InboundRequestListViewModel>>.Fail(
                        "Token không hợp lệ",
                        "UNAUTHORIZED",
                        null,
                        401
                    );
                    return Unauthorized(errorResult);
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

                var requests = query
                    .OrderByDescending(r => r.InboundDate)
                    .Select(r => new InboundRequestListViewModel
                    {
                        ReceiptId = r.ReceiptId,
                        ReceiptNumber = r.ReceiptNumber,
                        WarehouseId = r.WarehouseId,
                        WarehouseName = r.Warehouse.WarehouseName,
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

                var result = ApiResponse<List<InboundRequestListViewModel>>.Ok(
                    requests,
                    "Lấy danh sách yêu cầu gửi hàng thành công"
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                var result = ApiResponse<List<InboundRequestListViewModel>>.Fail(
                    $"Lỗi khi lấy danh sách yêu cầu: {ex.Message}",
                    "INTERNAL_ERROR",
                    null,
                    500
                );
                return StatusCode(500, result);
            }
        }

        /// <summary>
        /// Lấy chi tiết một yêu cầu gửi hàng
        /// </summary>
        /// <param name="receiptId">ID của receipt</param>
        /// <returns>Chi tiết inbound request</returns>
        [HttpGet("{receiptId}")]
        public IActionResult GetInboundRequestDetail(int receiptId)
        {
            try
            {
                var accountId = Utils.GetCurrentAccountId(User);
                var role = Utils.GetCurrentRole(User);

                if (accountId == null)
                {
                    var errorResult = ApiResponse<InboundRequestDetailViewModel>.Fail(
                        "Token không hợp lệ",
                        "UNAUTHORIZED",
                        null,
                        401
                    );
                    return Unauthorized(errorResult);
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
                    var errorResult = ApiResponse<InboundRequestDetailViewModel>.Fail(
                        "Không tìm thấy yêu cầu gửi hàng",
                        "NOT_FOUND",
                        null,
                        404
                    );
                    return NotFound(errorResult);
                }

                // Kiểm tra quyền truy cập
                if (role == "customer" && receipt.CustomerId != accountId.Value)
                {
                    var errorResult = ApiResponse<InboundRequestDetailViewModel>.Fail(
                        "Bạn không có quyền xem yêu cầu này",
                        "FORBIDDEN",
                        null,
                        403
                    );
                    return StatusCode(403, errorResult);
                }

                var detail = new InboundRequestDetailViewModel
                {
                    ReceiptId = receipt.ReceiptId,
                    ReceiptNumber = receipt.ReceiptNumber,
                    WarehouseId = receipt.WarehouseId,
                    WarehouseName = receipt.Warehouse.WarehouseName,
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

                var result = ApiResponse<InboundRequestDetailViewModel>.Ok(
                    detail,
                    "Lấy chi tiết yêu cầu thành công"
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                var result = ApiResponse<InboundRequestDetailViewModel>.Fail(
                    $"Lỗi khi lấy chi tiết yêu cầu: {ex.Message}",
                    "INTERNAL_ERROR",
                    null,
                    500
                );
                return StatusCode(500, result);
            }
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
            try
            {
                var accountId = Utils.GetCurrentAccountId(User);
                var role = Utils.GetCurrentRole(User);

                if (accountId == null)
                {
                    var errorResult = ApiResponse<object>.Fail(
                        "Token không hợp lệ",
                        "UNAUTHORIZED",
                        null,
                        401
                    );
                    return Unauthorized(errorResult);
                }

                var receipt = _context.InboundReceipts
                    .FirstOrDefault(r => r.ReceiptId == receiptId);

                if (receipt == null)
                {
                    var errorResult = ApiResponse<object>.Fail(
                        "Không tìm thấy yêu cầu gửi hàng",
                        "NOT_FOUND",
                        null,
                        404
                    );
                    return NotFound(errorResult);
                }

                // Kiểm tra quyền: chỉ warehouse_owner hoặc admin mới được cập nhật status
                if (role == "customer")
                {
                    // Customer chỉ có thể cancel yêu cầu của chính họ
                    if (receipt.CustomerId != accountId.Value || request.Status != "cancelled")
                    {
                        var errorResult = ApiResponse<object>.Fail(
                            "Bạn chỉ có thể hủy yêu cầu của chính mình",
                            "FORBIDDEN",
                            null,
                            403
                        );
                        return StatusCode(403, errorResult);
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

                var result = ApiResponse<object>.Ok(
                    new { ReceiptId = receipt.ReceiptId, Status = receipt.Status },
                    "Cập nhật trạng thái thành công"
                );

                return Ok(result);
            }
            catch (DbUpdateException ex)
            {
                var result = ApiResponse<object>.Fail(
                    $"Lỗi khi cập nhật trạng thái: {ex.InnerException?.Message ?? ex.Message}",
                    "DATABASE_ERROR",
                    null,
                    500
                );
                return StatusCode(500, result);
            }
            catch (Exception ex)
            {
                var result = ApiResponse<object>.Fail(
                    $"Lỗi khi cập nhật trạng thái: {ex.Message}",
                    "INTERNAL_ERROR",
                    null,
                    500
                );
                return StatusCode(500, result);
            }
        }
    }
}

