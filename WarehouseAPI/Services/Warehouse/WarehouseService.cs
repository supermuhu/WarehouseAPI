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
                        ZoneId = pl.ZoneId,
                        ShelfId = pl.ShelfId,
                        PositionX = pl.PositionX,
                        PositionY = pl.PositionY,
                        PositionZ = pl.PositionZ,
                        IsGround = pl.IsGround,
                        StackLevel = pl.StackLevel,
                        StackedOnPallet = pl.StackedOnPallet,
                        PalletLength = pl.Pallet.Length,
                        PalletWidth = pl.Pallet.Width,
                        PalletHeight = pl.Pallet.Height ?? 0.15m
                    })
                    .ToList();

                // Load items on pallets
                var palletIds = pallets.Select(p => p.PalletId).ToList();
                var items = db.ItemAllocations
                    .Include(ia => ia.Item)
                    .ThenInclude(i => i.Customer)
                    .Where(ia => palletIds.Contains(ia.PalletId))
                    .Select(ia => new ItemAllocationViewModel
                    {
                        AllocationId = ia.AllocationId,
                        ItemId = ia.ItemId,
                        QrCode = ia.Item.QrCode,
                        ItemName = ia.Item.ItemName,
                        ItemType = ia.Item.ItemType,
                        CustomerId = ia.Item.CustomerId,
                        CustomerName = ia.Item.Customer.FullName,
                        PalletId = ia.PalletId,
                        PositionX = ia.PositionX,
                        PositionY = ia.PositionY,
                        PositionZ = ia.PositionZ,
                        Length = ia.Item.Length,
                        Width = ia.Item.Width,
                        Height = ia.Item.Height,
                        Weight = ia.Item.Weight,
                        Shape = ia.Item.Shape,
                        PriorityLevel = ia.Item.PriorityLevel,
                        IsHeavy = ia.Item.IsHeavy,
                        IsFragile = ia.Item.IsFragile
                    })
                    .ToList();

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
                    Items = items
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
                var warehouses = db.Warehouses
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

                return new ApiResponse(200, "Lấy danh sách kho theo chủ kho thành công", warehouses);
            }
            catch (Exception e)
            {
                return new ApiResponse(502, e.Message, null);
            }
        }
    }
}
