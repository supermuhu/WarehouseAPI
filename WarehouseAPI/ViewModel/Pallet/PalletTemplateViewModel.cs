namespace WarehouseAPI.ModelView.Pallet;

public class PalletTemplateViewModel
{
    public int TemplateId { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public string? PalletType { get; set; }
    public decimal Length { get; set; }
    public decimal Width { get; set; }
    public decimal Height { get; set; }
    public decimal MaxWeight { get; set; }
    public decimal MaxStackHeight { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

