using System.ComponentModel.DataAnnotations;

namespace WarehouseAPI.ModelView.Pallet;

public class CreatePalletRequest
{
    [Required(ErrorMessage = "Barcode là bắt buộc")]
    public string Barcode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Chiều dài là bắt buộc")]
    [Range(0.01, 1000, ErrorMessage = "Chiều dài phải từ 0.01 đến 1000")]
    public decimal Length { get; set; }

    [Required(ErrorMessage = "Chiều rộng là bắt buộc")]
    [Range(0.01, 1000, ErrorMessage = "Chiều rộng phải từ 0.01 đến 1000")]
    public decimal Width { get; set; }

    [Range(0.01, 100, ErrorMessage = "Chiều cao phải từ 0.01 đến 100")]
    public decimal? Height { get; set; }

    [Range(0.01, 100000, ErrorMessage = "Trọng lượng tối đa phải từ 0.01 đến 100000")]
    public decimal? MaxWeight { get; set; }

    [Range(0.01, 100, ErrorMessage = "Chiều cao xếp chồng tối đa phải từ 0.01 đến 100")]
    public decimal? MaxStackHeight { get; set; }

    public string? PalletType { get; set; }
}

