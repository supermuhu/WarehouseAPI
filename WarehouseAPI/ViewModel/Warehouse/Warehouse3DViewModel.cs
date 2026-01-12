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
        public string? Address { get; set; }
        public string? AllowedItemTypes { get; set; }
        public string? Status { get; set; }
        public bool IsRentable { get; set; }
        public decimal? CheckinPositionX { get; set; }
        public decimal? CheckinPositionY { get; set; }
        public decimal? CheckinPositionZ { get; set; }
        public decimal? CheckinLength { get; set; }
        public decimal? CheckinWidth { get; set; }
        public decimal? CheckinHeight { get; set; }
        
        public List<WarehouseZoneViewModel> Zones { get; set; } = new List<WarehouseZoneViewModel>();
        public List<RackViewModel> Racks { get; set; } = new List<RackViewModel>();
        public List<PalletLocationViewModel> Pallets { get; set; } = new List<PalletLocationViewModel>();
        public List<ItemAllocationViewModel> Items { get; set; } = new List<ItemAllocationViewModel>();
        public List<WarehouseGateViewModel> Gates { get; set; } = new List<WarehouseGateViewModel>();
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
        public string? Address { get; set; }
        public decimal Length { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }
        public string? WarehouseType { get; set; }
        public string? Status { get; set; }
        public bool IsRentable { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? ZoneId { get; set; }
        public string? ZoneName { get; set; }
        public string? ZoneType { get; set; }
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
        public decimal RotationY { get; set; }
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
        public string? LocationCode { get; set; }
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

        public decimal RotationY { get; set; }

        public string? PalletType { get; set; }
        public decimal? MaxWeight { get; set; }
        public decimal? MaxStackHeight { get; set; }

        public string? PalletQrContent { get; set; }
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
        
        // Product information
        public int ProductId { get; set; }
        public string? ProductCode { get; set; }
        public string? ProductName { get; set; }
        public string? Unit { get; set; }
        public string? Category { get; set; }
        public decimal? StandardLength { get; set; }
        public decimal? StandardWidth { get; set; }
        public decimal? StandardHeight { get; set; }
        public decimal? StandardWeight { get; set; }
        public string? ProductDescription { get; set; }
        public string? StorageConditions { get; set; }
        
        // Customer information
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        
        // Pallet information
        public int PalletId { get; set; }
        public decimal? PositionX { get; set; }
        public decimal? PositionY { get; set; }
        public decimal? PositionZ { get; set; }
        
        // Item dimensions and properties
        public decimal Length { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }
        public decimal? Weight { get; set; }
        public string? Shape { get; set; }
        public int? PriorityLevel { get; set; }
        public bool? IsHeavy { get; set; }
        public bool? IsFragile { get; set; }
        
        // Batch and date information
        public string? BatchNumber { get; set; }
        public DateOnly? ManufacturingDate { get; set; }
        public DateOnly? ExpiryDate { get; set; }

        // Commercial information
        public decimal? UnitPrice { get; set; }
        public decimal? TotalAmount { get; set; }

        /// <summary>
        /// Số lượng đơn vị hàng (ước tính) trên pallet, dùng cho hiển thị 3D.
        /// </summary>
        public int? UnitQuantity { get; set; }

        /// <summary>
        /// Layout chi tiết các đơn vị hàng trên pallet (nếu có), lấy từ InboundItemStackUnits.
        /// </summary>
        public List<ItemStackUnitViewModel>? StackUnits { get; set; }
    }

    /// <summary>
    /// ViewModel cho từng đơn vị hàng trong layout xếp chồng trên pallet.
    /// </summary>
    public class ItemStackUnitViewModel
    {
        public int UnitIndex { get; set; }
        public decimal LocalX { get; set; }
        public decimal LocalY { get; set; }
        public decimal LocalZ { get; set; }
        public decimal Length { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }
        public decimal RotationY { get; set; }
    }

    public class WarehouseGateViewModel
    {
        public int GateId { get; set; }
        public int WarehouseId { get; set; }
        public string? GateName { get; set; }
        public decimal PositionX { get; set; }
        public decimal PositionY { get; set; }
        public decimal PositionZ { get; set; }
        public decimal? Length { get; set; }
        public decimal? Width { get; set; }
        public decimal? Height { get; set; }
        public string GateType { get; set; } = string.Empty;
    }
}
