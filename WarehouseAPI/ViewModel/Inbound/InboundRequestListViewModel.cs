namespace WarehouseAPI.ModelView.Inbound;

public class InboundRequestListViewModel
{
    public int ReceiptId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public int WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public int? ZoneId { get; set; }
    public string? ZoneName { get; set; }
    public int CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int TotalItems { get; set; }
    public int TotalPallets { get; set; }
    public DateTime? InboundDate { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
    public string? CreatedByName { get; set; }
    public string StackMode { get; set; } = "auto";
}

