namespace WarehouseAPI.ModelView.Product;

public class ProductViewModel
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? Category { get; set; }
    public decimal? StandardLength { get; set; }
    public decimal? StandardWidth { get; set; }
    public decimal? StandardHeight { get; set; }
    public decimal? StandardWeight { get; set; }
    public bool? IsFragile { get; set; }
    public bool? IsHazardous { get; set; }
    public bool? IsNonStackable { get; set; }
    public string? StorageConditions { get; set; }
    public int? CreateUser { get; set; }
    public string? Status { get; set; }
}

