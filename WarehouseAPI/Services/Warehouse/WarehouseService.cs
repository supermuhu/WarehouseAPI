using Microsoft.EntityFrameworkCore;
using WarehouseAPI.Data;
using WarehouseAPI.Helpers;
using WarehouseAPI.ViewModel.Warehouse;

namespace WarehouseAPI.Services.Warehouse
{
    public class WarehouseService : IWarehouseService
    {
        private readonly WarehouseApiContext db;

        public WarehouseService(WarehouseApiContext db)
        {
            this.db = db;
        }

        public ApiResponse GetWarehouse3DData(int warehouseId, int accountId, string role)
        {
            try
            {
                var warehouse = db.Warehouses
                    .Include(w => w.Owner)
                    .FirstOrDefault(w => w.WarehouseId == warehouseId);

                if (warehouse == null)
                {
                    return new ApiResponse(404, "Không tìm thấy kho", null);
                }

                // Kiểm tra quyền truy cập
                bool isOwner = role.ToLower() == "admin" || role.ToLower() == "warehouse_owner" || warehouse.OwnerId == accountId;
                bool isCustomer = role.ToLower() == "customer";

                // Load zones - filter theo role
                var zonesQuery = db.WarehouseZones
                    .Include(z => z.Customer)
                    .Where(z => z.WarehouseId == warehouseId);

                // Nếu là customer, chỉ lấy zones của họ
                if (isCustomer)
                {
                    zonesQuery = zonesQuery.Where(z => z.CustomerId == accountId);
                }

                var zones = zonesQuery
                    .Select(z => new WarehouseZoneViewModel
                    {
                        ZoneId = z.ZoneId,
                        ZoneName = z.ZoneName,
                        CustomerId = z.CustomerId,
                        CustomerName = z.Customer != null ? z.Customer.FullName : null,
                        PositionX = z.PositionX,
                        PositionY = z.PositionY,
                        PositionZ = z.PositionZ,
                        Length = z.Length,
                        Width = z.Width,
                        Height = z.Height,
                        ZoneType = z.ZoneType
                    })
                    .ToList();

                // Load racks with shelves
                var zoneIds = zones.Select(z => z.ZoneId).ToList();
                var racks = db.Racks
                    .Include(r => r.Shelves)
                    .Where(r => zoneIds.Contains(r.ZoneId))
                    .Select(r => new RackViewModel
                    {
                        RackId = r.RackId,
                        ZoneId = r.ZoneId,
                        RackName = r.RackName,
                        PositionX = r.PositionX,
                        PositionY = r.PositionY,
                        PositionZ = r.PositionZ,
                        Length = r.Length,
                        Width = r.Width,
                        Height = r.Height,
                        MaxShelves = r.MaxShelves,
                        Shelves = r.Shelves.Select(s => new ShelfViewModel
                        {
                            ShelfId = s.ShelfId,
                            RackId = s.RackId,
                            ShelfLevel = s.ShelfLevel,
                            PositionY = s.PositionY,
                            Length = s.Length,
                            Width = s.Width,
                            MaxWeight = s.MaxWeight
                        }).ToList()
                    })
                    .ToList();

                // Load pallets with locations
                var pallets = db.PalletLocations
                    .Include(pl => pl.Pallet)
                    .Include(pl => pl.Zone)
                    .Include(pl => pl.Shelf)
                    .Where(pl => zoneIds.Contains(pl.ZoneId))
                    .Select(pl => new PalletLocationViewModel
                    {
                        LocationId = pl.LocationId,
                        PalletId = pl.PalletId,
                        Barcode = pl.Pallet.Barcode,
                        LocationCode = pl.LocationCode,
                        ZoneId = pl.ZoneId,
                        ShelfId = pl.ShelfId,
                        PositionX = pl.PositionX,
                        PositionY = pl.PositionY,
                        PositionZ = pl.PositionZ,
                        IsGround = pl.IsGround,
                        StackLevel = pl.StackLevel,
                        StackedOnPallet = pl.StackedOnPallet,
                        RotationY = pl.RotationY,
                        PalletLength = pl.Pallet.Length,
                        PalletWidth = pl.Pallet.Width,
                        PalletHeight = pl.Pallet.Height,
                        PalletType = pl.Pallet.PalletType,
                        MaxWeight = pl.Pallet.MaxWeight,
                        MaxStackHeight = pl.Pallet.MaxStackHeight
                    })
                    .ToList();

                var gates = db.WarehouseGates
                    .Where(g => g.WarehouseId == warehouseId)
                    .Select(g => new WarehouseGateViewModel
                    {
                        GateId = g.GateId,
                        WarehouseId = g.WarehouseId,
                        GateName = g.GateName,
                        PositionX = g.PositionX,
                        PositionY = g.PositionY,
                        PositionZ = g.PositionZ,
                        Length = g.Length,
                        Width = g.Width,
                        Height = g.Height,
                        GateType = g.GateType
                    })
                    .ToList();

                // Load items on pallets
                var palletIds = pallets.Select(p => p.PalletId).ToList();

                var allocationEntities = db.ItemAllocations
                    .Include(ia => ia.Item)
                    .ThenInclude(i => i.Customer)
                    .Include(ia => ia.Item)
                    .ThenInclude(i => i.Product)
                    .Where(ia => palletIds.Contains(ia.PalletId))
                    .ToList();

                var items = new List<ItemAllocationViewModel>();

                if (allocationEntities.Any())
                {
                    var itemIds = allocationEntities.Select(ia => ia.ItemId).Distinct().ToList();
                    var palletsForItems = allocationEntities.Select(ia => ia.PalletId).Distinct().ToList();

                    // Inbound quantity per (ItemId, PalletId)
                    var inboundItemsForAlloc = db.InboundItems
                        .Where(ii => itemIds.Contains(ii.ItemId) && palletsForItems.Contains(ii.PalletId))
                        .Select(ii => new { ii.ItemId, ii.PalletId, ii.Quantity, ii.InboundItemId })
                        .ToList();

                    var inboundQtyLookup = inboundItemsForAlloc
                        .GroupBy(ii => new { ii.ItemId, ii.PalletId })
                        .ToDictionary(
                            g => (ItemId: g.Key.ItemId, PalletId: g.Key.PalletId),
                            g => g
                                .OrderByDescending(x => x.InboundItemId)
                                .Select(x => x.Quantity)
                                .FirstOrDefault()
                        );

                    // Picked quantity (OUTBOUND_PICK) per (ItemId, PalletId)
                    var pickedHistories = db.ItemLocationHistories
                        .Where(h => itemIds.Contains(h.ItemId)
                                    && h.PalletId.HasValue
                                    && palletsForItems.Contains(h.PalletId.Value)
                                    && h.ActionType == "OUTBOUND_PICK")
                        .Select(h => new { h.ItemId, h.PalletId })
                        .ToList();

                    var pickedQtyLookup = pickedHistories
                        .GroupBy(h => new { h.ItemId, PalletId = h.PalletId!.Value })
                        .ToDictionary(
                            g => (ItemId: g.Key.ItemId, PalletId: g.Key.PalletId),
                            g => g.Count()
                        );

                    foreach (var ia in allocationEntities)
                    {
                        var key = (ItemId: ia.ItemId, PalletId: ia.PalletId);

                        inboundQtyLookup.TryGetValue(key, out var inboundQty);
                        if (inboundQty <= 0)
                        {
                            continue; // Không có inbound cho cặp (Item, Pallet) này
                        }

                        pickedQtyLookup.TryGetValue(key, out var pickedQty);
                        var remainingQty = inboundQty - pickedQty;
                        if (remainingQty <= 0)
                        {
                            continue; // Đã lấy hết hàng trên pallet cho item này
                        }

                        var vm = new ItemAllocationViewModel
                        {
                            AllocationId = ia.AllocationId,
                            ItemId = ia.ItemId,
                            QrCode = ia.Item.QrCode,
                            ItemName = ia.Item.ItemName,
                            ItemType = ia.Item.ItemType,

                            // Product information
                            ProductId = ia.Item.ProductId,
                            ProductCode = ia.Item.Product.ProductCode,
                            ProductName = ia.Item.Product.ProductName,
                            Unit = ia.Item.Product.Unit,
                            Category = ia.Item.Product.Category,
                            StandardLength = ia.Item.Product.StandardLength,
                            StandardWidth = ia.Item.Product.StandardWidth,
                            StandardHeight = ia.Item.Product.StandardHeight,
                            StandardWeight = ia.Item.Product.StandardWeight,
                            ProductDescription = ia.Item.Product.Description,
                            StorageConditions = ia.Item.Product.StorageConditions,

                            // Customer information
                            CustomerId = ia.Item.CustomerId,
                            CustomerName = ia.Item.Customer.FullName,

                            // Pallet information
                            PalletId = ia.PalletId,
                            PositionX = ia.PositionX,
                            PositionY = ia.PositionY,
                            PositionZ = ia.PositionZ,

                            // Item dimensions and properties
                            Length = ia.Item.Length,
                            Width = ia.Item.Width,
                            Height = ia.Item.Height,
                            Weight = ia.Item.Weight,
                            Shape = ia.Item.Shape,
                            PriorityLevel = ia.Item.PriorityLevel,
                            IsHeavy = ia.Item.IsHeavy,
                            IsFragile = ia.Item.IsFragile,

                            // Batch and date information
                            BatchNumber = ia.Item.BatchNumber,
                            ManufacturingDate = ia.Item.ManufacturingDate,
                            ExpiryDate = ia.Item.ExpiryDate,

                            // Commercial information
                            UnitPrice = ia.Item.UnitPrice,
                            TotalAmount = ia.Item.TotalAmount,
                            UnitQuantity = remainingQty
                        };

                        items.Add(vm);
                    }
                }

                // Gắn layout chi tiết (nếu có) từ InboundItemStackUnits cho từng item trên pallet
                if (items.Any())
                {
                    var itemIds = items.Select(i => i.ItemId).Distinct().ToList();
                    var palletsForItems = items.Select(i => i.PalletId).Distinct().ToList();

                    // Lấy inbound_item tương ứng với từng cặp (ItemId, PalletId), ưu tiên inbound mới nhất
                    var inboundItemsForAlloc = db.InboundItems
                        .Where(ii => itemIds.Contains(ii.ItemId) && palletsForItems.Contains(ii.PalletId))
                        .Select(ii => new { ii.InboundItemId, ii.ItemId, ii.PalletId })
                        .ToList();

                    var inboundItemLookup = inboundItemsForAlloc
                        .GroupBy(ii => new { ii.ItemId, ii.PalletId })
                        .ToDictionary(
                            g => (ItemId: g.Key.ItemId, PalletId: g.Key.PalletId),
                            g => g.OrderByDescending(x => x.InboundItemId).First().InboundItemId
                        );

                    var inboundItemIds = inboundItemLookup.Values.Distinct().ToList();

                    if (inboundItemIds.Any())
                    {
                        var units = db.InboundItemStackUnits
                            .Where(u => inboundItemIds.Contains(u.InboundItemId))
                            .ToList();

                        var unitsByInboundItem = units
                            .GroupBy(u => u.InboundItemId)
                            .ToDictionary(
                                g => g.Key,
                                g => g.OrderBy(u => u.UnitIndex).ToList()
                            );

                        // Tra bảng chiều cao pallet theo PalletId để tính chiều cao khối hàng trên pallet
                        var palletHeights = pallets
                            .GroupBy(p => p.PalletId)
                            .ToDictionary(g => g.Key, g => g.First().PalletHeight);

                        foreach (var item in items)
                        {
                            if (!inboundItemLookup.TryGetValue((item.ItemId, item.PalletId), out var inboundItemId))
                            {
                                continue;
                            }

                            if (!unitsByInboundItem.TryGetValue(inboundItemId, out var unitEntities) ||
                                unitEntities == null || unitEntities.Count == 0)
                            {
                                continue;
                            }

                            var stackUnits = unitEntities
                                .Select(u => new ItemStackUnitViewModel
                                {
                                    UnitIndex = u.UnitIndex,
                                    LocalX = u.LocalX,
                                    LocalY = u.LocalY,
                                    LocalZ = u.LocalZ,
                                    Length = u.Length,
                                    Width = u.Width,
                                    Height = u.Height,
                                    RotationY = u.RotationY
                                })
                                .ToList();

                            item.StackUnits = stackUnits;

                            // Từ layout chi tiết tính lại kích thước khối hàng (L/W/H) trên pallet
                            if (stackUnits.Any())
                            {
                                // Chiều dài & rộng: bounding box theo trục X/Z quanh tâm pallet
                                var minX = stackUnits.Min(u => u.LocalX - u.Length / 2m);
                                var maxX = stackUnits.Max(u => u.LocalX + u.Length / 2m);
                                var minZ = stackUnits.Min(u => u.LocalZ - u.Width / 2m);
                                var maxZ = stackUnits.Max(u => u.LocalZ + u.Width / 2m);

                                var blockLength = maxX - minX;
                                var blockWidth = maxZ - minZ;

                                if (blockLength > 0m)
                                {
                                    item.Length = blockLength;
                                }

                                if (blockWidth > 0m)
                                {
                                    item.Width = blockWidth;
                                }

                                // Chiều cao: đỉnh cao nhất trừ đi chiều cao pallet
                                if (palletHeights.TryGetValue(item.PalletId, out var palletHeight))
                                {
                                    var maxTop = stackUnits.Max(u => u.LocalY + u.Height / 2m);
                                    var goodsHeight = maxTop - palletHeight;

                                    if (goodsHeight > 0m)
                                    {
                                        item.Height = goodsHeight;
                                    }
                                }
                            }
                        }
                    }
                }

                // Tính nội dung QR cho từng pallet từ thông tin pallet và danh sách hàng trên pallet
                if (pallets.Any())
                {
                    foreach (var pallet in pallets)
                    {
                        var palletItems = items.Where(i => i.PalletId == pallet.PalletId).ToList();

                        if (!palletItems.Any())
                        {
                            pallet.PalletQrContent = $"PALLET|{pallet.PalletId}|WH={warehouseId}";
                            continue;
                        }

                        var qrLines = new List<string>();

                        qrLines.Add($"Vị trí: ({pallet.PositionX}, {pallet.PositionY}, {pallet.PositionZ})");
                        qrLines.Add($"Kích thước pallet: {pallet.PalletLength}m × {pallet.PalletWidth}m × {pallet.PalletHeight}m");
                        qrLines.Add("------------------");
                        qrLines.Add("Danh sách hàng trên pallet:");

                        foreach (var item in palletItems)
                        {
                            var name = item.ProductName ?? item.ItemName ?? item.QrCode ?? "Hàng hóa";
                            qrLines.Add(name);

                            if (!string.IsNullOrWhiteSpace(item.ProductCode))
                            {
                                qrLines.Add($"Mã SP: {item.ProductCode}");
                            }

                            if (!string.IsNullOrWhiteSpace(item.CustomerName))
                            {
                                qrLines.Add($"Khách hàng: {item.CustomerName}");
                            }

                            string unitSize;
                            if (item.StandardLength.HasValue && item.StandardWidth.HasValue && item.StandardHeight.HasValue)
                            {
                                unitSize = $"{item.StandardLength}m × {item.StandardWidth}m × {item.StandardHeight}m";
                            }
                            else
                            {
                                unitSize = "N/A";
                            }

                            var stackSize = $"{item.Length}m × {item.Width}m × {item.Height}m";

                            var qty = item.UnitQuantity;
                            string qtyText;
                            if (qty.HasValue)
                            {
                                qtyText = string.IsNullOrWhiteSpace(item.Unit)
                                    ? qty.Value.ToString()
                                    : $"{qty.Value} {item.Unit}";
                            }
                            else
                            {
                                qtyText = "N/A";
                            }

                            qrLines.Add($"Kích thước thùng (1 đơn vị): {unitSize}");
                            qrLines.Add($"Kích thước khối hàng trên pallet: {stackSize}");
                            qrLines.Add($"Số lượng đơn vị trên pallet: {qtyText}");

                            string? weightLine = null;
                            var hasStdWeight = item.StandardWeight.HasValue;
                            var hasQty = qty.HasValue;

                            if (hasStdWeight && hasQty)
                            {
                                var totalWeight = item.StandardWeight!.Value * qty!.Value;
                                weightLine = $"Trọng lượng: {totalWeight} kg / Chuẩn: {item.StandardWeight} kg";
                            }
                            else if (item.Weight.HasValue || item.StandardWeight.HasValue)
                            {
                                var parts = new List<string>();
                                if (item.Weight.HasValue)
                                {
                                    parts.Add($"{item.Weight.Value} kg");
                                }
                                if (item.StandardWeight.HasValue)
                                {
                                    parts.Add($"Chuẩn: {item.StandardWeight.Value} kg");
                                }

                                if (parts.Count > 0)
                                {
                                    weightLine = $"Trọng lượng: {string.Join(" / ", parts)}";
                                }
                            }

                            if (!string.IsNullOrEmpty(weightLine))
                            {
                                qrLines.Add(weightLine);
                            }

                            if (item.ManufacturingDate.HasValue)
                            {
                                qrLines.Add($"Ngày sản xuất: {item.ManufacturingDate.Value:yyyy-MM-dd}");
                            }

                            if (item.ExpiryDate.HasValue)
                            {
                                qrLines.Add($"Hạn sử dụng: {item.ExpiryDate.Value:yyyy-MM-dd}");
                            }

                            if (!string.IsNullOrWhiteSpace(item.ProductDescription))
                            {
                                qrLines.Add($"Mô tả sản phẩm: {item.ProductDescription}");
                            }

                            if (!string.IsNullOrWhiteSpace(item.StorageConditions))
                            {
                                qrLines.Add($"Lưu ý bảo quản: {item.StorageConditions}");
                            }

                            if (item.UnitPrice.HasValue || item.TotalAmount.HasValue)
                            {
                                var unitPriceText = item.UnitPrice.HasValue
                                    ? item.UnitPrice.Value.ToString("#,0.##")
                                    : string.Empty;

                                var totalAmountText = item.TotalAmount.HasValue
                                    ? item.TotalAmount.Value.ToString("#,0.##")
                                    : string.Empty;

                                if (!string.IsNullOrEmpty(unitPriceText) && !string.IsNullOrEmpty(totalAmountText))
                                {
                                    qrLines.Add($"Giá: {unitPriceText} / đơn vị – Thành tiền: {totalAmountText}");
                                }
                                else if (!string.IsNullOrEmpty(unitPriceText))
                                {
                                    qrLines.Add($"Giá: {unitPriceText} / đơn vị");
                                }
                                else if (!string.IsNullOrEmpty(totalAmountText))
                                {
                                    qrLines.Add($"Thành tiền: {totalAmountText}");
                                }
                            }

                            if (item.IsFragile == true)
                            {
                                qrLines.Add("⚠ Dễ vỡ");
                            }

                            if (item.IsHeavy == true)
                            {
                                qrLines.Add("⚠ Hàng nặng");
                            }

                            qrLines.Add(string.Empty);
                        }

                        pallet.PalletQrContent = string.Join("\n", qrLines);
                    }
                }

                var result = new Warehouse3DViewModel
                {
                    WarehouseId = warehouse.WarehouseId,
                    WarehouseName = warehouse.WarehouseName,
                    OwnerId = warehouse.OwnerId,
                    OwnerName = warehouse.Owner.FullName,
                    Length = warehouse.Length,
                    Width = warehouse.Width,
                    Height = warehouse.Height,
                    WarehouseType = warehouse.WarehouseType,
                    AllowedItemTypes = warehouse.AllowedItemTypes,
                    Status = warehouse.Status,
                    Zones = zones,
                    Racks = racks,
                    Pallets = pallets,
                    Items = items,
                    CheckinPositionX = warehouse.CheckinPositionX,
                    CheckinPositionY = warehouse.CheckinPositionY,
                    CheckinPositionZ = warehouse.CheckinPositionZ,
                    CheckinLength = warehouse.CheckinLength,
                    CheckinWidth = warehouse.CheckinWidth,
                    CheckinHeight = warehouse.CheckinHeight,
                    Gates = gates
                };

                return new ApiResponse(200, "Lấy dữ liệu kho 3D thành công", result);
            }
            catch (Exception e)
            {
                return new ApiResponse(502, e.Message, null);
            }
        }

        public ApiResponse GetAllWarehouses()
        {
            try
            {
                var warehouses = db.Warehouses
                    .Include(w => w.Owner)
                    .Where(w => w.Status == "active")
                    .Select(w => new WarehouseListViewModel
                    {
                        WarehouseId = w.WarehouseId,
                        WarehouseName = w.WarehouseName,
                        OwnerId = w.OwnerId,
                        OwnerName = w.Owner.FullName,
                        Length = w.Length,
                        Width = w.Width,
                        Height = w.Height,
                        WarehouseType = w.WarehouseType,
                        Status = w.Status,
                        CreatedAt = w.CreatedAt
                    })
                    .ToList();

                return new ApiResponse(200, "Lấy danh sách kho thành công", warehouses);
            }
            catch (Exception e)
            {
                return new ApiResponse(502, e.Message, null);
            }
        }

        public ApiResponse GetWarehousesByOwner(int ownerId)
        {
            try
            {
                // Ưu tiên trả về danh sách theo từng zone (khu vực) để FE có thể hiển thị
                // "Kho - Khu vực" giống như luồng customer.
                var zones = db.WarehouseZones
                    .Include(z => z.Warehouse)
                        .ThenInclude(w => w.Owner)
                    .Where(z => z.Warehouse.OwnerId == ownerId && z.Warehouse.Status == "active")
                    .ToList();

                List<WarehouseListViewModel> warehouses;

                if (zones.Any())
                {
                    warehouses = zones
                        .Select(z => new WarehouseListViewModel
                        {
                            WarehouseId = z.WarehouseId,
                            WarehouseName = z.Warehouse.WarehouseName,
                            OwnerId = z.Warehouse.OwnerId,
                            OwnerName = z.Warehouse.Owner.FullName,
                            Length = z.Warehouse.Length,
                            Width = z.Warehouse.Width,
                            Height = z.Warehouse.Height,
                            WarehouseType = z.Warehouse.WarehouseType,
                            Status = z.Warehouse.Status,
                            CreatedAt = z.Warehouse.CreatedAt,
                            ZoneId = z.ZoneId,
                            ZoneName = z.ZoneName,
                            ZoneType = z.ZoneType
                        })
                        .ToList();
                }
                else
                {
                    // Nếu chưa cấu hình zone, fallback về danh sách kho như cũ
                    warehouses = db.Warehouses
                        .Include(w => w.Owner)
                        .Where(w => w.OwnerId == ownerId)
                        .Select(w => new WarehouseListViewModel
                        {
                            WarehouseId = w.WarehouseId,
                            WarehouseName = w.WarehouseName,
                            OwnerId = w.OwnerId,
                            OwnerName = w.Owner.FullName,
                            Length = w.Length,
                            Width = w.Width,
                            Height = w.Height,
                            WarehouseType = w.WarehouseType,
                            Status = w.Status,
                            CreatedAt = w.CreatedAt
                        })
                        .ToList();
                }

                return new ApiResponse(200, "Lấy danh sách kho theo chủ kho thành công", warehouses);
            }
            catch (Exception e)
            {
                return new ApiResponse(502, e.Message, null);
            }
        }

        public ApiResponse GetWarehousesByCustomer(int customerId)
        {
            try
            {
                var zones = db.WarehouseZones
                    .Include(z => z.Warehouse)
                        .ThenInclude(w => w.Owner)
                    .Where(z => z.CustomerId == customerId && z.Warehouse.Status == "active")
                    .ToList();

                if (!zones.Any())
                {
                    return new ApiResponse(200, "Khách hàng chưa thuê kho nào", new List<WarehouseListViewModel>());
                }

                var warehouses = zones
                    .Select(z => new WarehouseListViewModel
                    {
                        WarehouseId = z.WarehouseId,
                        WarehouseName = z.Warehouse.WarehouseName,
                        OwnerId = z.Warehouse.OwnerId,
                        OwnerName = z.Warehouse.Owner.FullName,
                        Length = z.Warehouse.Length,
                        Width = z.Warehouse.Width,
                        Height = z.Warehouse.Height,
                        WarehouseType = z.Warehouse.WarehouseType,
                        Status = z.Warehouse.Status,
                        CreatedAt = z.Warehouse.CreatedAt,
                        ZoneId = z.ZoneId,
                        ZoneName = z.ZoneName,
                        ZoneType = z.ZoneType
                    })
                    .ToList();

                return new ApiResponse(200, "Lấy danh sách kho đã thuê thành công", warehouses);
            }
            catch (Exception e)
            {
                return new ApiResponse(502, e.Message, null);
            }
        }

        /// <summary>
        /// Lấy danh sách kệ theo zone, có kiểm tra quyền theo accountId/role.
        /// </summary>
        public ApiResponse GetZoneRacks(int zoneId, int accountId, string role)
        {
            try
            {
                var zone = db.WarehouseZones.FirstOrDefault(z => z.ZoneId == zoneId);
                if (zone == null)
                {
                    return new ApiResponse(404, "Không tìm thấy khu vực", null);
                }

                var warehouse = db.Warehouses.FirstOrDefault(w => w.WarehouseId == zone.WarehouseId);
                if (warehouse == null)
                {
                    return new ApiResponse(404, "Không tìm thấy kho", null);
                }

                var normalizedRole = role?.ToLower() ?? string.Empty;
                var isAdmin = normalizedRole == "admin";
                var isOwner = normalizedRole == "warehouse_owner" || warehouse.OwnerId == accountId;
                var isCustomer = normalizedRole == "customer";

                if (isCustomer)
                {
                    if (zone.CustomerId != accountId)
                    {
                        return new ApiResponse(403, "Bạn không có quyền xem kệ của khu vực này", null);
                    }
                }
                else if (!isAdmin && !isOwner)
                {
                    return new ApiResponse(403, "Bạn không có quyền xem kệ của khu vực này", null);
                }

                var racks = db.Racks
                    .Where(r => r.ZoneId == zoneId)
                    .Select(r => new RackDto
                    {
                        RackId = r.RackId,
                        ZoneId = r.ZoneId,
                        RackName = r.RackName,
                        PositionX = r.PositionX,
                        PositionY = r.PositionY,
                        PositionZ = r.PositionZ,
                        Length = r.Length,
                        Width = r.Width,
                        Height = r.Height,
                        MaxShelves = r.MaxShelves
                    })
                    .ToList();

                return new ApiResponse(200, "Lấy danh sách kệ thành công", racks);
            }
            catch (Exception e)
            {
                return new ApiResponse(502, e.Message, null);
            }
        }

        public ApiResponse CreateRack(int zoneId, int accountId, string role, CreateRackRequest request)
        {
            try
            {
                var zone = db.WarehouseZones.FirstOrDefault(z => z.ZoneId == zoneId);
                if (zone == null)
                {
                    return new ApiResponse(404, "Không tìm thấy khu vực", null);
                }

                if (zone.ZoneType != "rack")
                {
                    return new ApiResponse(400, "Chỉ có thể tạo kệ trong khu vực loại 'rack'", null);
                }

                var warehouse = db.Warehouses.FirstOrDefault(w => w.WarehouseId == zone.WarehouseId);
                if (warehouse == null)
                {
                    return new ApiResponse(404, "Không tìm thấy kho", null);
                }

                var normalizedRole = role?.ToLower() ?? string.Empty;
                var isAdmin = normalizedRole == "admin";
                var isOwner = normalizedRole == "warehouse_owner" || warehouse.OwnerId == accountId;
                var isCustomer = normalizedRole == "customer";

                if (isCustomer)
                {
                    if (zone.CustomerId != accountId)
                    {
                        return new ApiResponse(403, "Bạn không có quyền thêm kệ vào khu vực này", null);
                    }
                }
                else if (!isAdmin && !isOwner)
                {
                    return new ApiResponse(403, "Bạn không có quyền thêm kệ vào khu vực này", null);
                }

                // Tạm thời positionY = zone.PositionY (chân kệ trên nền khu vực)
                var rack = new Rack
                {
                    ZoneId = zoneId,
                    RackName = request.RackName,
                    PositionX = request.PositionX,
                    PositionY = zone.PositionY,
                    PositionZ = request.PositionZ,
                    Length = request.Length,
                    Width = request.Width,
                    Height = request.Height,
                    MaxShelves = request.MaxShelves
                };

                db.Racks.Add(rack);
                db.SaveChanges();

                var dto = new RackDto
                {
                    RackId = rack.RackId,
                    ZoneId = rack.ZoneId,
                    RackName = rack.RackName,
                    PositionX = rack.PositionX,
                    PositionY = rack.PositionY,
                    PositionZ = rack.PositionZ,
                    Length = rack.Length,
                    Width = rack.Width,
                    Height = rack.Height,
                    MaxShelves = rack.MaxShelves
                };

                return new ApiResponse(201, "Tạo kệ thành công", dto);
            }
            catch (Exception e)
            {
                return new ApiResponse(502, e.Message, null);
            }
        }

        public ApiResponse UpdateRack(int zoneId, int rackId, int accountId, string role, UpdateRackRequest request)
        {
            try
            {
                var rack = db.Racks.FirstOrDefault(r => r.RackId == rackId && r.ZoneId == zoneId);
                if (rack == null)
                {
                    return new ApiResponse(404, "Không tìm thấy kệ", null);
                }

                var zone = db.WarehouseZones.FirstOrDefault(z => z.ZoneId == rack.ZoneId);
                if (zone == null)
                {
                    return new ApiResponse(404, "Không tìm thấy khu vực", null);
                }

                var warehouse = db.Warehouses.FirstOrDefault(w => w.WarehouseId == zone.WarehouseId);
                if (warehouse == null)
                {
                    return new ApiResponse(404, "Không tìm thấy kho", null);
                }

                var normalizedRole = role?.ToLower() ?? string.Empty;
                var isAdmin = normalizedRole == "admin";
                var isOwner = normalizedRole == "warehouse_owner" || warehouse.OwnerId == accountId;
                var isCustomer = normalizedRole == "customer";

                if (isCustomer)
                {
                    if (zone.CustomerId != accountId)
                    {
                        return new ApiResponse(403, "Bạn không có quyền sửa kệ của khu vực này", null);
                    }
                }
                else if (!isAdmin && !isOwner)
                {
                    return new ApiResponse(403, "Bạn không có quyền sửa kệ của khu vực này", null);
                }

                rack.RackName = request.RackName ?? rack.RackName;
                rack.PositionX = request.PositionX;
                rack.PositionZ = request.PositionZ;
                if (request.Height.HasValue)
                {
                    rack.Height = request.Height.Value;
                }

                db.SaveChanges();

                return new ApiResponse(200, "Cập nhật kệ thành công", null);
            }
            catch (Exception e)
            {
                return new ApiResponse(502, e.Message, null);
            }
        }

        public ApiResponse BulkUpdateRackPositions(int zoneId, int accountId, string role, BulkUpdateRackPositionsRequest request)
        {
            try
            {
                var zone = db.WarehouseZones.FirstOrDefault(z => z.ZoneId == zoneId);
                if (zone == null)
                {
                    return new ApiResponse(404, "Không tìm thấy khu vực", null);
                }

                var warehouse = db.Warehouses.FirstOrDefault(w => w.WarehouseId == zone.WarehouseId);
                if (warehouse == null)
                {
                    return new ApiResponse(404, "Không tìm thấy kho", null);
                }

                var normalizedRole = role?.ToLower() ?? string.Empty;
                var isAdmin = normalizedRole == "admin";
                var isOwner = normalizedRole == "warehouse_owner" || warehouse.OwnerId == accountId;
                var isCustomer = normalizedRole == "customer";

                if (isCustomer)
                {
                    if (zone.CustomerId != accountId)
                    {
                        return new ApiResponse(403, "Bạn không có quyền cập nhật bố trí kệ của khu vực này", null);
                    }
                }
                else if (!isAdmin && !isOwner)
                {
                    return new ApiResponse(403, "Bạn không có quyền cập nhật bố trí kệ của khu vực này", null);
                }

                var rackIds = request.Racks.Select(r => r.RackId).ToList();
                var racks = db.Racks.Where(r => rackIds.Contains(r.RackId) && r.ZoneId == zoneId).ToList();

                foreach (var item in request.Racks)
                {
                    var rack = racks.FirstOrDefault(r => r.RackId == item.RackId);
                    if (rack == null) continue;

                    rack.PositionX = item.PositionX;
                    rack.PositionZ = item.PositionZ;
                }

                db.SaveChanges();

                return new ApiResponse(200, "Cập nhật vị trí kệ thành công", null);
            }
            catch (Exception e)
            {
                return new ApiResponse(502, e.Message, null);
            }
        }

        public ApiResponse DeleteRack(int zoneId, int rackId, int accountId, string role)
        {
            try
            {
                var rack = db.Racks.FirstOrDefault(r => r.RackId == rackId && r.ZoneId == zoneId);
                if (rack == null)
                {
                    return new ApiResponse(404, "Không tìm thấy kệ", null);
                }

                var zone = db.WarehouseZones.FirstOrDefault(z => z.ZoneId == rack.ZoneId);
                if (zone == null)
                {
                    return new ApiResponse(404, "Không tìm thấy khu vực", null);
                }

                var warehouse = db.Warehouses.FirstOrDefault(w => w.WarehouseId == zone.WarehouseId);
                if (warehouse == null)
                {
                    return new ApiResponse(404, "Không tìm thấy kho", null);
                }

                var normalizedRole = role?.ToLower() ?? string.Empty;
                var isAdmin = normalizedRole == "admin";
                var isOwner = normalizedRole == "warehouse_owner" || warehouse.OwnerId == accountId;
                var isCustomer = normalizedRole == "customer";

                if (isCustomer)
                {
                    if (zone.CustomerId != accountId)
                    {
                        return new ApiResponse(403, "Bạn không có quyền xóa kệ của khu vực này", null);
                    }
                }
                else if (!isAdmin && !isOwner)
                {
                    return new ApiResponse(403, "Bạn không có quyền xóa kệ của khu vực này", null);
                }

                // Kiểm tra ràng buộc liên quan
                bool hasShelves = db.Shelves.Any(s => s.RackId == rack.RackId);
                bool hasPallets = db.PalletLocations.Any(pl => pl.Shelf != null && pl.Shelf.RackId == rack.RackId);

                if (hasShelves || hasPallets)
                {
                    return new ApiResponse(400, "Không thể xóa kệ vì vẫn còn tầng kệ hoặc pallet đang sử dụng", null);
                }

                db.Racks.Remove(rack);
                db.SaveChanges();

                return new ApiResponse(200, "Xóa kệ thành công", null);
            }
            catch (Exception e)
            {
                return new ApiResponse(502, e.Message, null);
            }
        }
    }
}
