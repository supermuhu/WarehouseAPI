using System.ComponentModel.DataAnnotations;

namespace WarehouseAPI.ModelView.Product;

public class CreateProductRequest
{
    [Required(ErrorMessage = "Mã sản phẩm là bắt buộc")]
    [StringLength(100, ErrorMessage = "Mã sản phẩm không được vượt quá 100 ký tự")]
    public string ProductCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
    [StringLength(300, ErrorMessage = "Tên sản phẩm không được vượt quá 300 ký tự")]
    public string ProductName { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required(ErrorMessage = "Đơn vị tính là bắt buộc")]
    [StringLength(50, ErrorMessage = "Đơn vị tính không được vượt quá 50 ký tự")]
    public string Unit { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Danh mục không được vượt quá 100 ký tự")]
    public string? Category { get; set; }

    [Range(0.01, 1000, ErrorMessage = "Chiều dài phải từ 0.01 đến 1000")]
    public decimal? StandardLength { get; set; }

    [Range(0.01, 1000, ErrorMessage = "Chiều rộng phải từ 0.01 đến 1000")]
    public decimal? StandardWidth { get; set; }

    [Range(0.01, 1000, ErrorMessage = "Chiều cao phải từ 0.01 đến 1000")]
    public decimal? StandardHeight { get; set; }

    [Range(0.01, 100000, ErrorMessage = "Trọng lượng phải từ 0.01 đến 100000")]
    public decimal? StandardWeight { get; set; }

    public bool? IsFragile { get; set; }

    public bool? IsHazardous { get; set; }

    public bool? IsNonStackable { get; set; }

    public string? StorageConditions { get; set; }
}

