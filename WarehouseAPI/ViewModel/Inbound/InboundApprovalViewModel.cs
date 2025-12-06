using WarehouseAPI.ViewModel.Warehouse;

namespace WarehouseAPI.ModelView.Inbound;

public class InboundApprovalViewModel
{
    public int ReceiptId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public int WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public int? ZoneId { get; set; }
    public string? ZoneName { get; set; }
    public int CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
    public List<InboundApprovalItemViewModel> Items { get; set; } = new List<InboundApprovalItemViewModel>();
    public string StackMode { get; set; } = "auto";
}

public class InboundApprovalItemViewModel
{
    public int InboundItemId { get; set; }
    public int ItemId { get; set; }

    public int PalletId { get; set; }
    public string PalletBarcode { get; set; } = string.Empty;

    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string? Category { get; set; }

    public int Quantity { get; set; }

    // Kích thước 1 đơn vị sản phẩm (box/bag)
    public decimal? UnitLength { get; set; }
    public decimal? UnitWidth { get; set; }
    public decimal? UnitHeight { get; set; }

    // Kích thước khối hàng trên pallet (từ Item - đã nhập ở bước 4)
    public decimal ItemLength { get; set; }
    public decimal ItemWidth { get; set; }
    public decimal ItemHeight { get; set; }

    // Kích thước pallet
    public decimal PalletLength { get; set; }
    public decimal PalletWidth { get; set; }
    public decimal PalletHeight { get; set; }

    // Gợi ý loại hiển thị
    public bool IsBag { get; set; }

    /// <summary>
    /// Layout chi tiết các đơn vị hàng trên pallet (nếu có), lấy từ InboundItemStackUnits.
    /// Dùng cho viewer 2D/3D để vẽ đúng cách xếp.
    /// </summary>
    public List<ItemStackUnitViewModel>? StackUnits { get; set; }
}
