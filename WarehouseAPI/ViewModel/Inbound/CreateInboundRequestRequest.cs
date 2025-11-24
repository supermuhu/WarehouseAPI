using System.ComponentModel.DataAnnotations;

namespace WarehouseAPI.ModelView.Inbound;

public class CreateInboundRequestRequest
{
    [Required(ErrorMessage = "WarehouseId là bắt buộc")]
    public int WarehouseId { get; set; }

    [Required(ErrorMessage = "Danh sách hàng hóa là bắt buộc")]
    [MinLength(1, ErrorMessage = "Phải có ít nhất 1 hàng hóa")]
    public List<InboundItemRequest> Items { get; set; } = new List<InboundItemRequest>();

    public string? Notes { get; set; }
}

public class InboundItemRequest
{
    [Required(ErrorMessage = "PalletId là bắt buộc")]
    public int PalletId { get; set; }

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

    public string? BatchNumber { get; set; }
}

