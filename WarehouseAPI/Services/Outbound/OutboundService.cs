using Microsoft.EntityFrameworkCore;
using WarehouseAPI.Data;
using WarehouseAPI.ModelView.Common;
using WarehouseAPI.ModelView.Outbound;

namespace WarehouseAPI.Services.Outbound
{
    public class OutboundService : IOutboundService
    {
        private readonly WarehouseApiContext _context;

        public OutboundService(WarehouseApiContext context)
        {
            _context = context;
        }

        public ApiResponse<List<OutboundAvailablePalletViewModel>> GetAvailablePallets(
            int? accountId,
            string role,
            int warehouseId,
            int? customerId)
        {
            try
            {
                if (accountId == null)
                {
                    return ApiResponse<List<OutboundAvailablePalletViewModel>>.Fail(
                        "Token không hợp lệ",
                        "UNAUTHORIZED",
                        null,
                        401
                    );
                }

                var warehouse = _context.Warehouses
                    .Include(w => w.Owner)
                    .FirstOrDefault(w => w.WarehouseId == warehouseId && w.Status == "active");

                if (warehouse == null)
                {
                    return ApiResponse<List<OutboundAvailablePalletViewModel>>.Fail(
                        "Kho không tồn tại hoặc không hoạt động",
                        "WAREHOUSE_NOT_FOUND",
                        null,
                        404
                    );
                }

                var normalizedRole = (role ?? string.Empty).ToLowerInvariant();
                var isAdmin = normalizedRole == "admin";
                var isOwner = normalizedRole == "warehouse_owner" || warehouse.OwnerId == accountId.Value;
                var isCustomer = normalizedRole == "customer";

                if (!isAdmin && !isOwner && !isCustomer)
                {
                    return ApiResponse<List<OutboundAvailablePalletViewModel>>.Fail(
                        "Bạn không có quyền xem dữ liệu xuất kho của kho này",
                        "FORBIDDEN",
                        null,
                        403
                    );
                }

                int? effectiveCustomerId = null;
                if (isCustomer)
                {
                    effectiveCustomerId = accountId.Value;
                }
                else if (customerId.HasValue)
                {
                    effectiveCustomerId = customerId.Value;
                }

                var zonesQuery = _context.WarehouseZones.Where(z => z.WarehouseId == warehouseId);
                if (isCustomer)
                {
                    zonesQuery = zonesQuery.Where(z => z.CustomerId == accountId.Value);
                }

                var zoneIds = zonesQuery.Select(z => z.ZoneId).ToList();
                if (!zoneIds.Any())
                {
                    return ApiResponse<List<OutboundAvailablePalletViewModel>>.Ok(
                        new List<OutboundAvailablePalletViewModel>(),
                        "Không có khu vực nào khả dụng trong kho này",
                        200
                    );
                }

                var baseQuery =
                    from ia in _context.ItemAllocations
                    join item in _context.Items on ia.ItemId equals item.ItemId
                    join prod in _context.Products on item.ProductId equals prod.ProductId
                    join pl in _context.PalletLocations on ia.PalletId equals pl.PalletId
                    join zone in _context.WarehouseZones on pl.ZoneId equals zone.ZoneId
                    join pallet in _context.Pallets on pl.PalletId equals pallet.PalletId
                    where zoneIds.Contains(pl.ZoneId)
                    select new
                    {
                        ia,
                        item,
                        prod,
                        pl,
                        zone,
                        pallet
                    };

                if (effectiveCustomerId.HasValue)
                {
                    var cid = effectiveCustomerId.Value;
                    baseQuery = baseQuery.Where(x => x.item.CustomerId == cid);
                }

                var baseList = baseQuery.ToList();
                if (!baseList.Any())
                {
                    return ApiResponse<List<OutboundAvailablePalletViewModel>>.Ok(
                        new List<OutboundAvailablePalletViewModel>(),
                        "Không có pallet nào phù hợp để xuất kho",
                        200
                    );
                }

                // Chỉ cho phép xuất các pallet "ở trên cùng" (không có pallet nào khác đang xếp chồng lên)
                var palletIds = baseList
                    .Select(x => x.pl.PalletId)
                    .Distinct()
                    .ToList();

                if (palletIds.Any())
                {
                    var blockedPalletIds = _context.PalletLocations
                        .Where(pl => pl.StackedOnPallet.HasValue && palletIds.Contains(pl.StackedOnPallet.Value))
                        .Select(pl => pl.StackedOnPallet!.Value)
                        .Distinct()
                        .ToHashSet();

                    baseList = baseList
                        .Where(row => !blockedPalletIds.Contains(row.pl.PalletId))
                        .ToList();

                    if (!baseList.Any())
                    {
                        return ApiResponse<List<OutboundAvailablePalletViewModel>>.Ok(
                            new List<OutboundAvailablePalletViewModel>(),
                            "Không có pallet nào khả dụng để xuất kho (tất cả đều đang là pallet đáy trong stack)",
                            200
                        );
                    }
                }

                var itemIds = baseList.Select(x => x.item.ItemId).Distinct().ToList();

                var inboundItems = _context.InboundItems
                    .Include(ii => ii.Receipt)
                    .Where(ii => itemIds.Contains(ii.ItemId) && ii.Receipt.WarehouseId == warehouseId)
                    .ToList();

                var inboundLookup = inboundItems
                    .GroupBy(ii => ii.ItemId)
                    .ToDictionary(
                        g => g.Key,
                        g => new
                        {
                            Quantity = g
                                .OrderByDescending(ii => ii.InboundItemId)
                                .Select(ii => ii.Quantity)
                                .FirstOrDefault(),
                            FirstInboundDate = g
                                .Select(ii => ii.Receipt.InboundDate)
                                .Where(d => d.HasValue)
                                .Select(d => d!.Value)
                                .DefaultIfEmpty()
                                .Min()
                        }
                    );

                var outboundItems = _context.OutboundItems
                    .Include(oi => oi.Receipt)
                    .Where(oi => itemIds.Contains(oi.ItemId)
                                 && oi.Receipt.WarehouseId == warehouseId
                                 && oi.Receipt.Status != "cancelled")
                    .ToList();

                var outboundLookup = outboundItems
                    .GroupBy(oi => oi.ItemId)
                    .ToDictionary(
                        g => g.Key,
                        g => g
                            .Where(oi => oi.Quantity.HasValue)
                            .Select(oi => oi.Quantity!.Value)
                            .DefaultIfEmpty(0)
                            .Sum()
                    );

                var result = new List<OutboundAvailablePalletViewModel>();

                foreach (var row in baseList)
                {
                    if (!inboundLookup.TryGetValue(row.item.ItemId, out var inboundStat))
                    {
                        continue;
                    }

                    var inboundQty = inboundStat.Quantity;
                    outboundLookup.TryGetValue(row.item.ItemId, out var removedQty);
                    var availableQty = inboundQty - removedQty;

                    if (availableQty <= 0)
                    {
                        continue;
                    }

                    var vm = new OutboundAvailablePalletViewModel
                    {
                        PalletId = row.pl.PalletId,
                        Barcode = row.pallet.Barcode,
                        WarehouseId = warehouseId,
                        ZoneId = row.zone.ZoneId,
                        ZoneName = row.zone.ZoneName,
                        ShelfId = row.pl.ShelfId,
                        IsGround = row.pl.IsGround,
                        PositionX = row.pl.PositionX,
                        PositionY = row.pl.PositionY,
                        PositionZ = row.pl.PositionZ,
                        ItemId = row.item.ItemId,
                        ItemName = row.item.ItemName,
                        ProductCode = row.prod.ProductCode,
                        ProductName = row.prod.ProductName,
                        Unit = row.prod.Unit,
                        FirstInboundDate = inboundStat.FirstInboundDate,
                        ManufacturingDate = row.item.ManufacturingDate.HasValue
                            ? row.item.ManufacturingDate.Value.ToDateTime(TimeOnly.MinValue)
                            : null,
                        ExpiryDate = row.item.ExpiryDate.HasValue
                            ? row.item.ExpiryDate.Value.ToDateTime(TimeOnly.MinValue)
                            : null,
                        TotalQuantity = availableQty
                    };

                    result.Add(vm);
                }

                result = result
                    .OrderBy(r => r.FirstInboundDate ?? DateTime.MaxValue)
                    .ThenBy(r => r.TotalQuantity)
                    .ThenBy(r => r.ItemId)
                    .ToList();

                return ApiResponse<List<OutboundAvailablePalletViewModel>>.Ok(
                    result,
                    "Lấy danh sách pallet khả dụng để xuất kho thành công",
                    200
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<List<OutboundAvailablePalletViewModel>>.Fail(
                    $"Lỗi khi lấy danh sách pallet khả dụng: {ex.Message}",
                    "INTERNAL_ERROR",
                    null,
                    500
                );
            }
        }

        public ApiResponse<object> CreateOutboundRequest(
            int? accountId,
            string role,
            CreateOutboundRequestRequest request)
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

                if (request.Items == null || !request.Items.Any())
                {
                    return ApiResponse<object>.Fail(
                        "Danh sách hàng hóa không được để trống",
                        "EMPTY_ITEMS",
                        null,
                        400
                    );
                }

                var warehouse = _context.Warehouses
                    .Include(w => w.Owner)
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

                var normalizedRole = (role ?? string.Empty).ToLowerInvariant();
                var isAdmin = normalizedRole == "admin";
                var isOwner = normalizedRole == "warehouse_owner" || warehouse.OwnerId == accountId.Value;
                var isCustomer = normalizedRole == "customer";

                if (!isAdmin && !isOwner && !isCustomer)
                {
                    return ApiResponse<object>.Fail(
                        "Bạn không có quyền tạo yêu cầu xuất kho tại kho này",
                        "FORBIDDEN",
                        null,
                        403
                    );
                }

                int customerId;
                if (isCustomer)
                {
                    customerId = accountId.Value;
                }
                else
                {
                    if (!request.CustomerId.HasValue)
                    {
                        return ApiResponse<object>.Fail(
                            "Vui lòng chọn khách hàng cho yêu cầu xuất kho",
                            "MISSING_CUSTOMER",
                            null,
                            400
                        );
                    }

                    customerId = request.CustomerId.Value;
                }

                var itemIds = request.Items
                    .Select(i => i.ItemId)
                    .Distinct()
                    .ToList();

                var items = _context.Items
                    .Include(i => i.Customer)
                    .Include(i => i.Product)
                    .Where(i => itemIds.Contains(i.ItemId))
                    .ToList();

                if (items.Count != itemIds.Count)
                {
                    return ApiResponse<object>.Fail(
                        "Một hoặc nhiều Item không tồn tại",
                        "ITEM_NOT_FOUND",
                        null,
                        404
                    );
                }

                var invalidItems = items
                    .Where(i => i.CustomerId != customerId)
                    .ToList();

                if (invalidItems.Any())
                {
                    return ApiResponse<object>.Fail(
                        "Một hoặc nhiều Item không thuộc về khách hàng được chọn",
                        "ITEM_CUSTOMER_MISMATCH",
                        null,
                        403
                    );
                }

                var inboundItems = _context.InboundItems
                    .Include(ii => ii.Receipt)
                    .Where(ii => itemIds.Contains(ii.ItemId) && ii.Receipt.WarehouseId == request.WarehouseId)
                    .ToList();

                var inboundLookup = inboundItems
                    .GroupBy(ii => ii.ItemId)
                    .ToDictionary(
                        g => g.Key,
                        g => g
                            .OrderByDescending(ii => ii.InboundItemId)
                            .Select(ii => ii.Quantity)
                            .FirstOrDefault()
                    );

                var outboundItems = _context.OutboundItems
                    .Include(oi => oi.Receipt)
                    .Where(oi => itemIds.Contains(oi.ItemId)
                                 && oi.Receipt.WarehouseId == request.WarehouseId
                                 && oi.Receipt.Status != "cancelled")
                    .ToList();

                var outboundLookup = outboundItems
                    .GroupBy(oi => oi.ItemId)
                    .ToDictionary(
                        g => g.Key,
                        g => g
                            .Where(oi => oi.Quantity.HasValue)
                            .Select(oi => oi.Quantity!.Value)
                            .DefaultIfEmpty(0)
                            .Sum()
                    );

                foreach (var reqItem in request.Items)
                {
                    if (!inboundLookup.TryGetValue(reqItem.ItemId, out var inboundQty))
                    {
                        return ApiResponse<object>.Fail(
                            $"Item {reqItem.ItemId} không có dữ liệu nhập kho tại kho này",
                            "NO_INBOUND_DATA",
                            null,
                            400
                        );
                    }

                    outboundLookup.TryGetValue(reqItem.ItemId, out var removedQty);
                    var availableQty = inboundQty - removedQty;

                    if (availableQty <= 0)
                    {
                        return ApiResponse<object>.Fail(
                            $"Item {reqItem.ItemId} không còn tồn kho để xuất",
                            "OUT_OF_STOCK",
                            null,
                            400
                        );
                    }

                    if (reqItem.Quantity > availableQty)
                    {
                        return ApiResponse<object>.Fail(
                            $"Số lượng yêu cầu xuất ({reqItem.Quantity}) vượt quá số lượng còn lại ({availableQty}) cho Item {reqItem.ItemId}",
                            "INSUFFICIENT_QUANTITY",
                            null,
                            400
                        );
                    }
                }

                using var transaction = _context.Database.BeginTransaction();
                try
                {
                    var tempId = Guid.NewGuid().ToString("N");
                    var receiptNumber = $"OUT-{DateTime.Now:yyyyMMdd}-{tempId.Substring(0, 8)}";

                    var totalItems = request.Items.Sum(i => i.Quantity);

                    var receipt = new OutboundReceipt
                    {
                        WarehouseId = request.WarehouseId,
                        CustomerId = customerId,
                        ReceiptNumber = receiptNumber,
                        TotalItems = totalItems,
                        OutboundDate = DateTime.Now,
                        Notes = request.Notes,
                        CreatedBy = accountId.Value,
                        Status = "pending"
                    };

                    _context.OutboundReceipts.Add(receipt);
                    _context.SaveChanges();

                    foreach (var reqItem in request.Items)
                    {
                        var outboundItem = new OutboundItem
                        {
                            ReceiptId = receipt.ReceiptId,
                            ItemId = reqItem.ItemId,
                            Quantity = reqItem.Quantity,
                            RemovedAt = null
                        };

                        _context.OutboundItems.Add(outboundItem);
                    }

                    _context.SaveChanges();
                    transaction.Commit();

                    return ApiResponse<object>.Ok(
                        new
                        {
                            receipt.ReceiptId,
                            receipt.ReceiptNumber,
                            receipt.WarehouseId,
                            receipt.CustomerId,
                            receipt.TotalItems,
                            receipt.Status,
                            receipt.OutboundDate
                        },
                        "Tạo yêu cầu xuất kho thành công",
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
                    $"Lỗi cơ sở dữ liệu khi tạo yêu cầu xuất kho: {ex.InnerException?.Message ?? ex.Message}",
                    "DATABASE_ERROR",
                    null,
                    500
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail(
                    $"Lỗi khi tạo yêu cầu xuất kho: {ex.Message}",
                    "INTERNAL_ERROR",
                    null,
                    500
                );
            }
        }

        public ApiResponse<List<OutboundRequestListViewModel>> GetOutboundRequests(
            int? accountId,
            string role,
            int? warehouseId)
        {
            try
            {
                if (accountId == null)
                {
                    return ApiResponse<List<OutboundRequestListViewModel>>.Fail(
                        "Token không hợp lệ",
                        "UNAUTHORIZED",
                        null,
                        401
                    );
                }

                var normalizedRole = (role ?? string.Empty).ToLowerInvariant();
                var isAdmin = normalizedRole == "admin";
                var isCustomer = normalizedRole == "customer";
                var isOwner = normalizedRole == "warehouse_owner";

                var query = _context.OutboundReceipts
                    .Include(r => r.Warehouse)
                    .Include(r => r.Customer)
                    .Include(r => r.CreatedByNavigation)
                    .AsQueryable();

                if (warehouseId.HasValue)
                {
                    query = query.Where(r => r.WarehouseId == warehouseId.Value);
                }

                if (isCustomer)
                {
                    query = query.Where(r => r.CustomerId == accountId.Value);
                }
                else if (isOwner)
                {
                    query = query.Where(r => r.Warehouse.OwnerId == accountId.Value);
                }
                else if (!isAdmin)
                {
                    return ApiResponse<List<OutboundRequestListViewModel>>.Fail(
                        "Bạn không có quyền xem danh sách yêu cầu xuất kho",
                        "FORBIDDEN",
                        null,
                        403
                    );
                }

                var data = query
                    .OrderByDescending(r => r.OutboundDate ?? DateTime.MinValue)
                    .ThenByDescending(r => r.ReceiptId)
                    .Select(r => new OutboundRequestListViewModel
                    {
                        ReceiptId = r.ReceiptId,
                        ReceiptNumber = r.ReceiptNumber,
                        WarehouseId = r.WarehouseId,
                        WarehouseName = r.Warehouse.WarehouseName,
                        CustomerId = r.CustomerId,
                        CustomerName = r.Customer.FullName,
                        TotalItems = r.TotalItems,
                        CreatedAt = r.OutboundDate,
                        OutboundDate = r.CompletedDate,
                        Status = r.Status,
                        Notes = r.Notes,
                        CreatedByName = r.CreatedByNavigation.FullName
                    })
                    .ToList();

                return ApiResponse<List<OutboundRequestListViewModel>>.Ok(
                    data,
                    "Lấy danh sách yêu cầu xuất kho thành công",
                    200
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<List<OutboundRequestListViewModel>>.Fail(
                    $"Lỗi khi lấy danh sách yêu cầu xuất kho: {ex.Message}",
                    "INTERNAL_ERROR",
                    null,
                    500
                );
            }
        }

        public ApiResponse<List<OutboundPickingProgressViewModel>> GetOutboundPickingRequests(
            int? accountId,
            string role,
            int? warehouseId)
        {
            try
            {
                if (accountId == null)
                {
                    return ApiResponse<List<OutboundPickingProgressViewModel>>.Fail(
                        "Token không hợp lệ",
                        "UNAUTHORIZED",
                        null,
                        401
                    );
                }

                var normalizedRole = (role ?? string.Empty).ToLowerInvariant();
                var isAdmin = normalizedRole == "admin";
                var isCustomer = normalizedRole == "customer";
                var isOwner = normalizedRole == "warehouse_owner";

                var query = _context.OutboundReceipts
                    .Include(r => r.Warehouse)
                    .Include(r => r.Customer)
                    .Include(r => r.CreatedByNavigation)
                    .Where(r => r.Status == "completed")
                    .AsQueryable();

                if (warehouseId.HasValue)
                {
                    query = query.Where(r => r.WarehouseId == warehouseId.Value);
                }

                if (isCustomer)
                {
                    query = query.Where(r => r.CustomerId == accountId.Value);
                }
                else if (isOwner)
                {
                    query = query.Where(r => r.Warehouse.OwnerId == accountId.Value);
                }
                else if (!isAdmin)
                {
                    return ApiResponse<List<OutboundPickingProgressViewModel>>.Fail(
                        "Bạn không có quyền xem danh sách phiếu xuất đang lấy hàng",
                        "FORBIDDEN",
                        null,
                        403
                    );
                }

                var receipts = query.ToList();
                if (!receipts.Any())
                {
                    return ApiResponse<List<OutboundPickingProgressViewModel>>.Ok(
                        new List<OutboundPickingProgressViewModel>(),
                        "Không có phiếu xuất nào đang trong quá trình lấy hàng",
                        200
                    );
                }

                var receiptIds = receipts.Select(r => r.ReceiptId).ToList();

                // Tính tổng số pallet liên quan tới từng phiếu xuất (dựa trên ItemAllocations, không phụ thuộc PalletLocations)
                var totalPalletsByReceipt = _context.OutboundItems
                    .Where(oi => receiptIds.Contains(oi.ReceiptId))
                    .Join(
                        _context.ItemAllocations,
                        oi => oi.ItemId,
                        ia => ia.ItemId,
                        (oi, ia) => new { oi.ReceiptId, ia.PalletId }
                    )
                    .GroupBy(x => x.ReceiptId)
                    .Select(g => new
                    {
                        ReceiptId = g.Key,
                        TotalPallets = g
                            .Select(x => x.PalletId)
                            .Distinct()
                            .Count()
                    })
                    .ToDictionary(x => x.ReceiptId, x => x.TotalPallets);

                if (!totalPalletsByReceipt.Any())
                {
                    return ApiResponse<List<OutboundPickingProgressViewModel>>.Ok(
                        new List<OutboundPickingProgressViewModel>(),
                        "Không có phiếu xuất nào có pallet để lấy hàng",
                        200
                    );
                }

                // Số pallet đã được đánh dấu "Đã lấy" theo từng phiếu
                var pickedByReceipt = _context.OutboundPalletPicks
                    .Where(p => receiptIds.Contains(p.ReceiptId))
                    .GroupBy(p => p.ReceiptId)
                    .Select(g => new
                    {
                        ReceiptId = g.Key,
                        PickedPallets = g
                            .Select(p => p.PalletId)
                            .Distinct()
                            .Count()
                    })
                    .ToDictionary(x => x.ReceiptId, x => x.PickedPallets);

                var result = new List<OutboundPickingProgressViewModel>();

                foreach (var r in receipts)
                {
                    if (!totalPalletsByReceipt.TryGetValue(r.ReceiptId, out var totalPallets) || totalPallets <= 0)
                    {
                        continue;
                    }

                    pickedByReceipt.TryGetValue(r.ReceiptId, out var pickedPallets);

                    // Chỉ lấy các phiếu đã duyệt nhưng chưa lấy xong (PickedPallets < TotalPallets)
                    if (pickedPallets >= totalPallets)
                    {
                        continue;
                    }

                    var vm = new OutboundPickingProgressViewModel
                    {
                        ReceiptId = r.ReceiptId,
                        ReceiptNumber = r.ReceiptNumber,
                        WarehouseId = r.WarehouseId,
                        WarehouseName = r.Warehouse?.WarehouseName,
                        CustomerId = r.CustomerId,
                        CustomerName = r.Customer?.FullName,
                        TotalItems = r.TotalItems,
                        CreatedAt = r.OutboundDate,
                        OutboundDate = r.CompletedDate,
                        Status = r.Status,
                        Notes = r.Notes,
                        CreatedByName = r.CreatedByNavigation?.FullName,
                        TotalPallets = totalPallets,
                        PickedPallets = pickedPallets
                    };

                    result.Add(vm);
                }

                result = result
                    .OrderByDescending(r => r.CreatedAt ?? DateTime.MinValue)
                    .ThenByDescending(r => r.ReceiptId)
                    .ToList();

                return ApiResponse<List<OutboundPickingProgressViewModel>>.Ok(
                    result,
                    "Lấy danh sách phiếu xuất đang lấy hàng thành công",
                    200
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<List<OutboundPickingProgressViewModel>>.Fail(
                    $"Lỗi khi lấy danh sách phiếu xuất đang lấy hàng: {ex.Message}",
                    "INTERNAL_ERROR",
                    null,
                    500
                );
            }
        }

        public ApiResponse<OutboundRequestDetailViewModel> GetOutboundRequestDetail(
            int receiptId,
            int? accountId,
            string role)
        {
            try
            {
                if (accountId == null)
                {
                    return ApiResponse<OutboundRequestDetailViewModel>.Fail(
                        "Token không hợp lệ",
                        "UNAUTHORIZED",
                        null,
                        401
                    );
                }

                var receipt = _context.OutboundReceipts
                    .Include(r => r.Warehouse)
                    .Include(r => r.Customer)
                    .Include(r => r.CreatedByNavigation)
                    .Include(r => r.OutboundItems)
                        .ThenInclude(oi => oi.Item)
                            .ThenInclude(i => i.Product)
                    .FirstOrDefault(r => r.ReceiptId == receiptId);

                if (receipt == null)
                {
                    return ApiResponse<OutboundRequestDetailViewModel>.Fail(
                        "Không tìm thấy yêu cầu xuất kho",
                        "NOT_FOUND",
                        null,
                        404
                    );
                }

                if (role == "customer" && receipt.CustomerId != accountId.Value)
                {
                    return ApiResponse<OutboundRequestDetailViewModel>.Fail(
                        "Bạn không có quyền xem yêu cầu này",
                        "FORBIDDEN",
                        null,
                        403
                    );
                }

                var items = receipt.OutboundItems
                    .Select(oi => new OutboundItemDetailViewModel
                    {
                        OutboundItemId = oi.OutboundItemId,
                        ItemId = oi.ItemId,
                        QrCode = oi.Item.QrCode,
                        ItemName = oi.Item.ItemName,
                        ProductId = oi.Item.ProductId,
                        ProductName = oi.Item.Product.ProductName,
                        ProductCode = oi.Item.Product.ProductCode,
                        Quantity = oi.Quantity ?? 0,
                        ManufacturingDate = oi.Item.ManufacturingDate.HasValue
                            ? oi.Item.ManufacturingDate.Value.ToDateTime(TimeOnly.MinValue)
                            : null,
                        ExpiryDate = oi.Item.ExpiryDate.HasValue
                            ? oi.Item.ExpiryDate.Value.ToDateTime(TimeOnly.MinValue)
                            : null,
                        BatchNumber = oi.Item.BatchNumber,
                        Unit = oi.Item.Product.Unit
                    })
                    .ToList();

                var detail = new OutboundRequestDetailViewModel
                {
                    ReceiptId = receipt.ReceiptId,
                    ReceiptNumber = receipt.ReceiptNumber,
                    WarehouseId = receipt.WarehouseId,
                    WarehouseName = receipt.Warehouse.WarehouseName,
                    CustomerId = receipt.CustomerId,
                    CustomerName = receipt.Customer.FullName,
                    TotalItems = receipt.TotalItems,
                    CreatedAt = receipt.OutboundDate,
                    OutboundDate = receipt.CompletedDate,
                    Status = receipt.Status,
                    Notes = receipt.Notes,
                    CreatedByName = receipt.CreatedByNavigation.FullName,
                    Items = items
                };

                return ApiResponse<OutboundRequestDetailViewModel>.Ok(
                    detail,
                    "Lấy chi tiết yêu cầu xuất kho thành công"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<OutboundRequestDetailViewModel>.Fail(
                    $"Lỗi khi lấy chi tiết yêu cầu xuất kho: {ex.Message}",
                    "INTERNAL_ERROR",
                    null,
                    500
                );
            }
        }

        public ApiResponse<OutboundReceiptPrintViewModel> GetOutboundReceiptPrintData(
            int receiptId,
            int? accountId,
            string role)
        {
            try
            {
                if (accountId == null)
                {
                    return ApiResponse<OutboundReceiptPrintViewModel>.Fail(
                        "Token không hợp lệ",
                        "UNAUTHORIZED",
                        null,
                        401
                    );
                }

                var receipt = _context.OutboundReceipts
                    .Include(r => r.Warehouse)
                    .Include(r => r.Customer)
                    .Include(r => r.CreatedByNavigation)
                    .Include(r => r.OutboundItems)
                        .ThenInclude(oi => oi.Item)
                            .ThenInclude(i => i.Product)
                    .FirstOrDefault(r => r.ReceiptId == receiptId);

                if (receipt == null)
                {
                    return ApiResponse<OutboundReceiptPrintViewModel>.Fail(
                        "Không tìm thấy yêu cầu xuất kho",
                        "NOT_FOUND",
                        null,
                        404
                    );
                }

                var normalizedRole = (role ?? string.Empty).ToLowerInvariant();
                var isAdmin = normalizedRole == "admin";
                var isOwner = normalizedRole == "warehouse_owner" && receipt.Warehouse.OwnerId == accountId.Value;
                var isCustomer = normalizedRole == "customer" && receipt.CustomerId == accountId.Value;

                if (!isAdmin && !isOwner && !isCustomer)
                {
                    return ApiResponse<OutboundReceiptPrintViewModel>.Fail(
                        "Bạn không có quyền xem phiếu xuất này",
                        "FORBIDDEN",
                        null,
                        403
                    );
                }

                var items = receipt.OutboundItems
                    .Select(oi => new OutboundReceiptPrintItemViewModel
                    {
                        OutboundItemId = oi.OutboundItemId,
                        ItemId = oi.ItemId,
                        QrCode = oi.Item.QrCode ?? string.Empty,
                        ItemName = oi.Item.ItemName,
                        ProductId = oi.Item.ProductId,
                        ProductCode = oi.Item.Product.ProductCode,
                        ProductName = oi.Item.Product.ProductName,
                        Unit = oi.Item.Product.Unit,
                        Quantity = oi.Quantity ?? 0,
                        ManufacturingDate = oi.Item.ManufacturingDate.HasValue
                            ? oi.Item.ManufacturingDate.Value.ToDateTime(TimeOnly.MinValue)
                            : null,
                        ExpiryDate = oi.Item.ExpiryDate.HasValue
                            ? oi.Item.ExpiryDate.Value.ToDateTime(TimeOnly.MinValue)
                            : null,
                        BatchNumber = oi.Item.BatchNumber,
                        UnitPrice = oi.Item.UnitPrice,
                        TotalAmount = oi.Item.UnitPrice.HasValue && oi.Quantity.HasValue
                            ? oi.Item.UnitPrice.Value * oi.Quantity.Value
                            : null
                    })
                    .ToList();

                var printData = new OutboundReceiptPrintViewModel
                {
                    ReceiptId = receipt.ReceiptId,
                    ReceiptNumber = receipt.ReceiptNumber,
                    Status = receipt.Status,
                    OutboundDate = receipt.OutboundDate,
                    WarehouseId = receipt.WarehouseId,
                    WarehouseName = receipt.Warehouse.WarehouseName,
                    CustomerId = receipt.CustomerId,
                    CustomerName = receipt.Customer.FullName,
                    CreatedByName = receipt.CreatedByNavigation.FullName ?? receipt.CreatedByNavigation.Username,
                    Notes = receipt.Notes,
                    TotalItems = receipt.TotalItems,
                    Items = items
                };

                return ApiResponse<OutboundReceiptPrintViewModel>.Ok(
                    printData,
                    "Lấy dữ liệu in phiếu xuất kho thành công",
                    200
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<OutboundReceiptPrintViewModel>.Fail(
                    $"Lỗi khi lấy dữ liệu in phiếu xuất kho: {ex.Message}",
                    "INTERNAL_ERROR",
                    null,
                    500
                );
            }
        }

        public ApiResponse<object> UpdateOutboundRequestStatus(
            int receiptId,
            int? accountId,
            string role,
            UpdateOutboundRequestStatusRequest request)
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

                var receipt = _context.OutboundReceipts
                    .FirstOrDefault(r => r.ReceiptId == receiptId);

                if (receipt == null)
                {
                    return ApiResponse<object>.Fail(
                        "Không tìm thấy yêu cầu xuất kho",
                        "NOT_FOUND",
                        null,
                        404
                    );
                }

                if (role == "customer")
                {
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

                receipt.Status = request.Status;
                if (request.Status == "completed" && !receipt.CompletedDate.HasValue)
                {
                    receipt.CompletedDate = DateTime.Now;
                }

                if (!string.IsNullOrEmpty(request.Notes))
                {
                    receipt.Notes = string.IsNullOrEmpty(receipt.Notes)
                        ? request.Notes
                        : $"{receipt.Notes}\n{request.Notes}";
                }

                _context.SaveChanges();

                return ApiResponse<object>.Ok(
                    new { ReceiptId = receipt.ReceiptId, Status = receipt.Status },
                    "Cập nhật trạng thái yêu cầu xuất kho thành công"
                );
            }
            catch (DbUpdateException ex)
            {
                return ApiResponse<object>.Fail(
                    $"Lỗi khi cập nhật trạng thái yêu cầu xuất kho: {ex.InnerException?.Message ?? ex.Message}",
                    "DATABASE_ERROR",
                    null,
                    500
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail(
                    $"Lỗi khi cập nhật trạng thái yêu cầu xuất kho: {ex.Message}",
                    "INTERNAL_ERROR",
                    null,
                    500
                );
            }
        }

        public ApiResponse<List<OutboundPalletPickViewModel>> GetOutboundPalletPicks(
            int receiptId,
            int? accountId,
            string role)
        {
            try
            {
                if (accountId == null)
                {
                    return ApiResponse<List<OutboundPalletPickViewModel>>.Fail(
                        "Token không hợp lệ",
                        "UNAUTHORIZED",
                        null,
                        401
                    );
                }

                var receipt = _context.OutboundReceipts
                    .Include(r => r.Warehouse)
                    .Include(r => r.Customer)
                    .FirstOrDefault(r => r.ReceiptId == receiptId);

                if (receipt == null)
                {
                    return ApiResponse<List<OutboundPalletPickViewModel>>.Fail(
                        "Không tìm thấy yêu cầu xuất kho",
                        "NOT_FOUND",
                        null,
                        404
                    );
                }

                var normalizedRole = (role ?? string.Empty).ToLowerInvariant();
                var isAdmin = normalizedRole == "admin";
                var isOwner = normalizedRole == "warehouse_owner" && receipt.Warehouse.OwnerId == accountId.Value;
                var isCustomer = normalizedRole == "customer" && receipt.CustomerId == accountId.Value;

                if (!isAdmin && !isOwner && !isCustomer)
                {
                    return ApiResponse<List<OutboundPalletPickViewModel>>.Fail(
                        "Bạn không có quyền xem thông tin lấy pallet của phiếu này",
                        "FORBIDDEN",
                        null,
                        403
                    );
                }

                var picks = _context.OutboundPalletPicks
                    .Where(p => p.ReceiptId == receiptId)
                    .Include(p => p.PickedByNavigation)
                    .ToList();

                var data = picks
                    .Select(p => new OutboundPalletPickViewModel
                    {
                        PalletId = p.PalletId,
                        PickedAt = p.PickedAt,
                        PickedBy = p.PickedBy,
                        PickedByName = p.PickedByNavigation.FullName ?? p.PickedByNavigation.Username
                    })
                    .ToList();

                return ApiResponse<List<OutboundPalletPickViewModel>>.Ok(
                    data,
                    "Lấy danh sách pallet đã lấy thành công"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<List<OutboundPalletPickViewModel>>.Fail(
                    $"Lỗi khi lấy danh sách pallet đã lấy: {ex.Message}",
                    "INTERNAL_ERROR",
                    null,
                    500
                );
            }
        }

        public ApiResponse<object> MarkPalletPicked(
            int receiptId,
            int? accountId,
            string role,
            OutboundPalletPickRequest request)
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

                var receipt = _context.OutboundReceipts
                    .Include(r => r.Warehouse)
                    .Include(r => r.OutboundItems)
                    .FirstOrDefault(r => r.ReceiptId == receiptId);

                if (receipt == null)
                {
                    return ApiResponse<object>.Fail(
                        "Không tìm thấy yêu cầu xuất kho",
                        "NOT_FOUND",
                        null,
                        404
                    );
                }

                var normalizedRole = (role ?? string.Empty).ToLowerInvariant();
                var isAdmin = normalizedRole == "admin";
                var isOwner = normalizedRole == "warehouse_owner" && receipt.Warehouse.OwnerId == accountId.Value;

                if (!isAdmin && !isOwner)
                {
                    return ApiResponse<object>.Fail(
                        "Bạn không có quyền đánh dấu pallet đã lấy cho phiếu này",
                        "FORBIDDEN",
                        null,
                        403
                    );
                }

                // Kiểm tra pallet có chứa hàng thuộc phiếu outbound này không
                var outboundItemIds = receipt.OutboundItems
                    .Select(oi => oi.ItemId)
                    .Distinct()
                    .ToList();

                var hasItemOnPallet = _context.ItemAllocations
                    .Any(a => outboundItemIds.Contains(a.ItemId) && a.PalletId == request.PalletId);

                if (!hasItemOnPallet)
                {
                    return ApiResponse<object>.Fail(
                        "Pallet không chứa hàng thuộc phiếu xuất này",
                        "PALLET_NOT_IN_OUTBOUND",
                        null,
                        400
                    );
                }

                var existing = _context.OutboundPalletPicks
                    .FirstOrDefault(p => p.ReceiptId == receiptId && p.PalletId == request.PalletId);

                if (existing == null)
                {
                    // Chỉ trừ đúng số lượng còn thiếu theo phiếu xuất trên pallet,
                    // không xóa toàn bộ hàng nếu pallet còn dư.

                    // Các allocation trên pallet cho các mặt hàng thuộc phiếu xuất này
                    var palletAllocations = _context.ItemAllocations
                        .Where(a => outboundItemIds.Contains(a.ItemId) && a.PalletId == request.PalletId)
                        .ToList();

                    if (!palletAllocations.Any())
                    {
                        return ApiResponse<object>.Fail(
                            "Pallet không còn hàng thuộc phiếu xuất này",
                            "PALLET_NO_REMAINING_ITEMS",
                            null,
                            400
                        );
                    }

                    var palletLocation = _context.PalletLocations
                        .FirstOrDefault(pl => pl.PalletId == request.PalletId);

                    // Gom OutboundItems theo ItemId để tính tổng số lượng cần xuất cho từng mặt hàng
                    var requiredByItem = receipt.OutboundItems
                        .GroupBy(oi => oi.ItemId)
                        .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity ?? 0));

                    var noteReceiptToken = $"(ID={receipt.ReceiptId})";
                    var totalRemoved = 0;

                    // Xử lý theo từng ItemId có trên pallet (thực tế pallet của bạn chỉ có 1 loại hàng)
                    foreach (var group in palletAllocations.GroupBy(a => a.ItemId))
                    {
                        var itemId = group.Key;
                        if (!requiredByItem.TryGetValue(itemId, out var requiredQty) || requiredQty <= 0)
                        {
                            continue;
                        }

                        // Đã pick bao nhiêu đơn vị cho item này trong phiếu này rồi
                        var alreadyPicked = _context.ItemLocationHistories
                            .Count(h => h.ItemId == itemId
                                        && h.ActionType == "OUTBOUND_PICK"
                                        && h.Notes != null
                                        && h.Notes.Contains(noteReceiptToken));

                        var remainingQty = requiredQty - alreadyPicked;
                        if (remainingQty <= 0)
                        {
                            continue; // Đã đủ số lượng cho mặt hàng này
                        }

                        var inboundQty = _context.InboundItems
                            .Where(ii => ii.ItemId == itemId && ii.PalletId == request.PalletId)
                            .OrderByDescending(ii => ii.InboundItemId)
                            .Select(ii => ii.Quantity)
                            .FirstOrDefault();

                        if (inboundQty <= 0)
                        {
                            continue;
                        }

                        var pickedAllReceipts = _context.ItemLocationHistories
                            .Count(h => h.ItemId == itemId
                                        && h.PalletId == request.PalletId
                                        && h.ActionType == "OUTBOUND_PICK");

                        var physicalRemaining = inboundQty - pickedAllReceipts;
                        if (physicalRemaining <= 0)
                        {
                            continue;
                        }

                        var toPick = Math.Min(remainingQty, physicalRemaining);
                        if (toPick <= 0)
                        {
                            continue;
                        }

                        for (var i = 0; i < toPick; i++)
                        {
                            var history = new ItemLocationHistory
                            {
                                ItemId = itemId,
                                PalletId = request.PalletId,
                                LocationId = palletLocation?.LocationId,
                                ActionType = "OUTBOUND_PICK",
                                ActionDate = DateTime.Now,
                                PerformedBy = accountId.Value,
                                Notes = $"Outbound receipt {receipt.ReceiptNumber} (ID={receipt.ReceiptId}) - pallet picked"
                            };

                            _context.ItemLocationHistories.Add(history);
                        }

                        totalRemoved += toPick;
                    }

                    if (totalRemoved <= 0)
                    {
                        return ApiResponse<object>.Fail(
                            "Phiếu xuất này đã đủ số lượng, không còn lượng cần lấy thêm từ pallet.",
                            "NO_REMAINING_QUANTITY",
                            null,
                            400
                        );
                    }

                    var pick = new OutboundPalletPick
                    {
                        ReceiptId = receiptId,
                        PalletId = request.PalletId,
                        PickedBy = accountId.Value,
                        PickedAt = DateTime.Now,
                        Notes = request.Notes
                    };

                    _context.OutboundPalletPicks.Add(pick);
                    _context.SaveChanges();

                    // Sau khi cập nhật lịch sử OUTBOUND_PICK và xóa ItemAllocations tương ứng,
                    // kiểm tra tồn vật lý trên pallet (InboundItems - tất cả OUTBOUND_PICK)
                    // Nếu không còn đơn vị hàng nào thì xóa luôn PalletLocation để 3D không hiển thị pallet trống
                    TryRemovePalletIfPhysicallyEmpty(request.PalletId);
                }

                return ApiResponse<object>.Ok(
                    new { ReceiptId = receiptId, request.PalletId },
                    "Đã đánh dấu pallet là 'Đã lấy' và trừ tồn kho trên pallet theo đúng số lượng phiếu xuất"
                );
            }
            catch (DbUpdateException ex)
            {
                return ApiResponse<object>.Fail(
                    $"Lỗi khi lưu trạng thái lấy pallet: {ex.InnerException?.Message ?? ex.Message}",
                    "DATABASE_ERROR",
                    null,
                    500
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail(
                    $"Lỗi khi đánh dấu pallet đã lấy: {ex.Message}",
                    "INTERNAL_ERROR",
                    null,
                    500
                );
            }
        }

        private void TryRemovePalletIfPhysicallyEmpty(int palletId)
        {
            // Lấy tổng số lượng nhập cho từng (ItemId, PalletId) trên pallet này
            var inboundItemsForPallet = _context.InboundItems
                .Where(ii => ii.PalletId == palletId)
                .Select(ii => new { ii.ItemId, ii.PalletId, ii.Quantity, ii.InboundItemId })
                .ToList();

            if (!inboundItemsForPallet.Any())
            {
                // Không có dữ liệu inbound cho pallet này -> không đủ thông tin để tính tồn vật lý
                return;
            }

            var inboundQtyLookup = inboundItemsForPallet
                .GroupBy(ii => new { ii.ItemId, ii.PalletId })
                .ToDictionary(
                    g => (ItemId: g.Key.ItemId, PalletId: g.Key.PalletId),
                    g => g
                        .OrderByDescending(x => x.InboundItemId)
                        .Select(x => x.Quantity)
                        .FirstOrDefault()
                );

            var itemIds = inboundQtyLookup.Keys
                .Select(k => k.ItemId)
                .Distinct()
                .ToList();

            // Lấy toàn bộ lịch sử OUTBOUND_PICK trên pallet này cho các ItemId tương ứng
            var pickedHistories = _context.ItemLocationHistories
                .Where(h => itemIds.Contains(h.ItemId)
                            && h.PalletId.HasValue
                            && h.PalletId.Value == palletId
                            && h.ActionType == "OUTBOUND_PICK")
                .Select(h => new { h.ItemId, h.PalletId })
                .ToList();

            var pickedQtyLookup = pickedHistories
                .GroupBy(h => new { h.ItemId, PalletId = h.PalletId!.Value })
                .ToDictionary(
                    g => (ItemId: g.Key.ItemId, PalletId: g.Key.PalletId),
                    g => g.Count()
                );

            var hasRemainingPhysicalStock = false;

            foreach (var kvp in inboundQtyLookup)
            {
                var key = kvp.Key;
                var inboundQty = kvp.Value;

                if (inboundQty <= 0)
                {
                    continue;
                }

                pickedQtyLookup.TryGetValue((key.ItemId, key.PalletId), out var pickedQty);
                var remainingQty = inboundQty - pickedQty;

                if (remainingQty > 0)
                {
                    hasRemainingPhysicalStock = true;
                    break;
                }
            }

            if (hasRemainingPhysicalStock)
            {
                return;
            }

            // Không còn tồn vật lý trên bất kỳ mặt hàng nào của pallet -> xóa PalletLocation
            var palletLocations = _context.PalletLocations
                .Where(pl => pl.PalletId == palletId)
                .ToList();

            if (palletLocations.Any())
            {
                var locationIds = palletLocations
                    .Select(pl => pl.LocationId)
                    .ToList();

                var historiesToDetach = _context.ItemLocationHistories
                    .Where(h => h.LocationId.HasValue
                                && locationIds.Contains(h.LocationId.Value))
                    .ToList();

                if (historiesToDetach.Any())
                {
                    foreach (var h in historiesToDetach)
                    {
                        h.LocationId = null;
                    }

                    _context.SaveChanges();
                }

                _context.PalletLocations.RemoveRange(palletLocations);
                _context.SaveChanges();
            }
        }
    }
}
