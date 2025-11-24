using System.ComponentModel.DataAnnotations;

namespace WarehouseAPI.ModelView.Pallet;

public class CreatePalletFromTemplateRequest
{
    [Required(ErrorMessage = "Barcode là bắt buộc")]
    public string Barcode { get; set; } = string.Empty;

    public string? PalletType { get; set; }
}

