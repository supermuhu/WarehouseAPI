using System.ComponentModel.DataAnnotations;

namespace WarehouseAPI.ModelView.Inbound;

public class CreateInboundRequestRequest
{
    [Required(ErrorMessage = "WarehouseId là bắt buộc")]
    public int WarehouseId { get; set; }

    /// <summary>
    /// Zone cụ thể trong kho mà yêu cầu này hướng tới. Nếu null, BE sẽ xử lý trên tất cả khu vực của customer.
    /// Đối với customer, nên luôn gửi ZoneId để nghiệp vụ áp dụng đúng khu vực được chọn.
    /// </summary>
    public int? ZoneId { get; set; }

    [Required(ErrorMessage = "Danh sách hàng hóa là bắt buộc")]
    [MinLength(1, ErrorMessage = "Phải có ít nhất 1 hàng hóa")]
    public List<InboundItemRequest> Items { get; set; } = new List<InboundItemRequest>();

    public string StackMode { get; set; } = "auto";

    /// <summary>
    /// Mã template auto stack mà customer chọn (straight/pinwheel).
    /// </summary>
    public string? AutoStackTemplate { get; set; }

    public string? Notes { get; set; }
}

public class InboundItemRequest
{
    public int? PalletId { get; set; }

    public int? PalletTemplateId { get; set; }

    public string? PalletType { get; set; }

    public string StackMode { get; set; } = "auto";

    public string? AutoStackTemplate { get; set; }

    [Required(ErrorMessage = "ProductId là bắt buộc")]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "Số lượng là bắt buộc")]
    [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
    public int Quantity { get; set; }

    [Required(ErrorMessage = "Ngày sản xuất là bắt buộc")]
    public DateTime ManufacturingDate { get; set; }

    public DateTime? ExpiryDate { get; set; }

    [Required(ErrorMessage = "Đơn giá là bắt buộc")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Đơn giá phải lớn hơn 0")]
    public decimal UnitPrice { get; set; }

    [Required(ErrorMessage = "Thành tiền là bắt buộc")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Thành tiền phải lớn hơn 0")]
    public decimal TotalAmount { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Chiều dài phải lớn hơn 0")]
    public decimal? Length { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Chiều rộng phải lớn hơn 0")]
    public decimal? Width { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Chiều cao phải lớn hơn 0")]
    public decimal? Height { get; set; }

    public string? BatchNumber { get; set; }

    public decimal? PalletLength { get; set; }

    public decimal? PalletWidth { get; set; }

    public decimal? PalletHeight { get; set; }

    public decimal? PalletMaxWeight { get; set; }

    public decimal? PalletMaxStackHeight { get; set; }
}
