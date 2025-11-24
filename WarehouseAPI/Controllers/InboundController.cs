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

                        // Tạo items cho mỗi quantity
                        for (int i = 0; i < itemRequest.Quantity; i++)
                        {
                            var qrCode = $"QR-{receiptNumber}-{itemCounter:D4}";
                            
                            // Kiểm tra QR code đã tồn tại chưa
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
                                Length = product.StandardLength ?? 0.5m,
                                Width = product.StandardWidth ?? 0.4m,
                                Height = product.StandardHeight ?? 0.3m,
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
                                UnitPrice = itemRequest.UnitPrice,
                                TotalAmount = itemRequest.TotalAmount / itemRequest.Quantity, // Chia đều cho mỗi item
                                CreatedAt = DateTime.Now
                            };

                            _context.Items.Add(item);
                            _context.SaveChanges();

                            // Tạo inbound_item
                            var inboundItem = new InboundItem
                            {
                                ReceiptId = inboundReceipt.ReceiptId,
                                ItemId = item.ItemId,
                                PalletId = itemRequest.PalletId,
                                Quantity = 1
                            };

                            _context.InboundItems.Add(inboundItem);
                            itemCounter++;
                        }
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

