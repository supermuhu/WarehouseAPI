namespace WarehouseAPI.ModelView.Inbound;

public class InboundRequestDetailViewModel
{
    public int ReceiptId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public int WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public int CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int TotalItems { get; set; }
    public int TotalPallets { get; set; }
    public DateTime? InboundDate { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
    public string? CreatedByName { get; set; }
    public List<InboundItemDetailViewModel> Items { get; set; } = new List<InboundItemDetailViewModel>();
}

public class InboundItemDetailViewModel
{
    public int InboundItemId { get; set; }
    public int ItemId { get; set; }
    public string QrCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public int PalletId { get; set; }
    public string PalletBarcode { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime? ManufacturingDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? BatchNumber { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? TotalAmount { get; set; }
}

