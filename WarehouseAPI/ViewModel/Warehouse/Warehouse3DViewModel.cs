namespace WarehouseAPI.ViewModel.Warehouse
{
    /// <summary>
    /// ViewModel chính cho dữ liệu kho 3D - trả về cho client
    /// </summary>
    public class Warehouse3DViewModel
    {
        public int WarehouseId { get; set; }
        public string? WarehouseName { get; set; }
        public int OwnerId { get; set; }
        public string? OwnerName { get; set; }
        public decimal Length { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }
        public string? WarehouseType { get; set; }
        public string? AllowedItemTypes { get; set; }
        public string? Status { get; set; }
        
        public List<WarehouseZoneViewModel> Zones { get; set; } = new List<WarehouseZoneViewModel>();
        public List<RackViewModel> Racks { get; set; } = new List<RackViewModel>();
        public List<PalletLocationViewModel> Pallets { get; set; } = new List<PalletLocationViewModel>();
        public List<ItemAllocationViewModel> Items { get; set; } = new List<ItemAllocationViewModel>();
    }

    /// <summary>
    /// ViewModel cho danh sách kho
    /// </summary>
    public class WarehouseListViewModel
    {
        public int WarehouseId { get; set; }
        public string? WarehouseName { get; set; }
        public int OwnerId { get; set; }
        public string? OwnerName { get; set; }
        public decimal Length { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }
        public string? WarehouseType { get; set; }
        public string? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    /// <summary>
    /// ViewModel cho khu vực trong kho
    /// </summary>
    public class WarehouseZoneViewModel
    {
        public int ZoneId { get; set; }
        public string? ZoneName { get; set; }
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public decimal PositionX { get; set; }
        public decimal PositionY { get; set; }
        public decimal PositionZ { get; set; }
        public decimal Length { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }
        public string? ZoneType { get; set; } // ground, rack
    }

    /// <summary>
    /// ViewModel cho kệ
    /// </summary>
    public class RackViewModel
    {
        public int RackId { get; set; }
        public int ZoneId { get; set; }
        public string? RackName { get; set; }
        public decimal PositionX { get; set; }
        public decimal PositionY { get; set; }
        public decimal PositionZ { get; set; }
        public decimal Length { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }
        public int? MaxShelves { get; set; }
        public List<ShelfViewModel> Shelves { get; set; } = new List<ShelfViewModel>();
    }

    /// <summary>
    /// ViewModel cho tầng kệ
    /// </summary>
    public class ShelfViewModel
    {
        public int ShelfId { get; set; }
        public int RackId { get; set; }
        public int ShelfLevel { get; set; }
        public decimal PositionY { get; set; }
        public decimal Length { get; set; }
        public decimal Width { get; set; }
        public decimal? MaxWeight { get; set; }
    }

    /// <summary>
    /// ViewModel cho vị trí pallet
    /// </summary>
    public class PalletLocationViewModel
    {
        public int LocationId { get; set; }
        public int PalletId { get; set; }
        public string? Barcode { get; set; }
        public int ZoneId { get; set; }
        public int? ShelfId { get; set; }
        public decimal PositionX { get; set; }
        public decimal PositionY { get; set; }
        public decimal PositionZ { get; set; }
        public bool? IsGround { get; set; }
        public int? StackLevel { get; set; }
        public int? StackedOnPallet { get; set; }
        public decimal PalletLength { get; set; }
        public decimal PalletWidth { get; set; }
        public decimal PalletHeight { get; set; }
    }

    /// <summary>
    /// ViewModel cho hàng hóa trên pallet
    /// </summary>
    public class ItemAllocationViewModel
    {
        public int AllocationId { get; set; }
        public int ItemId { get; set; }
        public string? QrCode { get; set; }
        public string? ItemName { get; set; }
        public string? ItemType { get; set; }
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int PalletId { get; set; }
        public decimal? PositionX { get; set; }
        public decimal? PositionY { get; set; }
        public decimal? PositionZ { get; set; }
        public decimal Length { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }
        public decimal? Weight { get; set; }
        public string? Shape { get; set; }
        public int? PriorityLevel { get; set; }
        public bool? IsHeavy { get; set; }
        public bool? IsFragile { get; set; }
    }
}
