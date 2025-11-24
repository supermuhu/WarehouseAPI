namespace WarehouseAPI.ModelView.Pallet;

public class PalletViewModel
{
    public int PalletId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public decimal Length { get; set; }
    public decimal Width { get; set; }
    public decimal? Height { get; set; }
    public decimal? MaxWeight { get; set; }
    public decimal? MaxStackHeight { get; set; }
    public string? Status { get; set; }
    public string? PalletType { get; set; }
    public DateTime? CreatedAt { get; set; }
}

