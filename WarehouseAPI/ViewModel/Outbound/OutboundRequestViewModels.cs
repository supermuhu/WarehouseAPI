using System.ComponentModel.DataAnnotations;
using WarehouseAPI.ModelView.Common;

namespace WarehouseAPI.ModelView.Outbound
{
    public class CreateOutboundRequestRequest
    {
        [Required(ErrorMessage = "WarehouseId là bắt buộc")]
        public int WarehouseId { get; set; }

        /// <summary>
        /// Customer thực hiện yêu cầu xuất kho. Nếu không truyền, BE sẽ dùng account hiện tại khi role = customer.
        /// </summary>
        public int? CustomerId { get; set; }

        [Required(ErrorMessage = "Danh sách hàng hóa là bắt buộc")]
        [MinLength(1, ErrorMessage = "Phải có ít nhất 1 hàng hóa")]
        public List<OutboundItemRequest> Items { get; set; } = new List<OutboundItemRequest>();

        public string? Notes { get; set; }
    }

    public class OutboundItemRequest
    {
        [Required(ErrorMessage = "ItemId là bắt buộc")]
        public int ItemId { get; set; }

        [Required(ErrorMessage = "Số lượng là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int Quantity { get; set; }
    }

    public class OutboundAvailablePalletViewModel
    {
        public int PalletId { get; set; }
        public string? Barcode { get; set; }

        public int WarehouseId { get; set; }
        public int ZoneId { get; set; }
        public string? ZoneName { get; set; }
        public int? ShelfId { get; set; }
        public bool IsGround { get; set; }
        public decimal PositionX { get; set; }
        public decimal PositionY { get; set; }
        public decimal PositionZ { get; set; }

        public int ItemId { get; set; }
        public string? ItemName { get; set; }
        public string? ProductCode { get; set; }
        public string? ProductName { get; set; }
        public string? Unit { get; set; }

        public DateTime? FirstInboundDate { get; set; }
        public DateTime? ManufacturingDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int TotalQuantity { get; set; }
    }

    public class OutboundRequestListViewModel
    {
        public int ReceiptId { get; set; }
        public string ReceiptNumber { get; set; } = string.Empty;
        public int WarehouseId { get; set; }
        public string? WarehouseName { get; set; }
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int TotalItems { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? OutboundDate { get; set; }
        public string? Status { get; set; }
        public string? Notes { get; set; }
        public string? CreatedByName { get; set; }
    }

    public class OutboundPickingProgressViewModel
    {
        public int ReceiptId { get; set; }
        public string ReceiptNumber { get; set; } = string.Empty;
        public int WarehouseId { get; set; }
        public string? WarehouseName { get; set; }
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int TotalItems { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? OutboundDate { get; set; }
        public string? Status { get; set; }
        public string? Notes { get; set; }
        public string? CreatedByName { get; set; }

        /// <summary>
        /// Tổng số pallet liên quan tới phiếu xuất này (tính từ các ItemAllocation của các item trong phiếu).
        /// </summary>
        public int TotalPallets { get; set; }

        /// <summary>
        /// Số pallet đã được đánh dấu "Đã lấy" trong bảng outbound_pallet_picks.
        /// </summary>
        public int PickedPallets { get; set; }
    }

    public class OutboundItemDetailViewModel
    {
        public int OutboundItemId { get; set; }
        public int ItemId { get; set; }
        public string QrCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public DateTime? ManufacturingDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? BatchNumber { get; set; }
        public string? Unit { get; set; }
    }

    public class OutboundRequestDetailViewModel
    {
        public int ReceiptId { get; set; }
        public string ReceiptNumber { get; set; } = string.Empty;
        public int WarehouseId { get; set; }
        public string? WarehouseName { get; set; }
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int TotalItems { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? OutboundDate { get; set; }
        public string? Status { get; set; }
        public string? Notes { get; set; }
        public string? CreatedByName { get; set; }
        public List<OutboundItemDetailViewModel> Items { get; set; } = new List<OutboundItemDetailViewModel>();
    }

    public class UpdateOutboundRequestStatusRequest
    {
        [Required(ErrorMessage = "Status là bắt buộc")]
        [RegularExpression("^(pending|completed|cancelled)$", ErrorMessage = "Status phải là pending, completed hoặc cancelled")]
        public string Status { get; set; } = string.Empty;

        public string? Notes { get; set; }
    }

    public class OutboundPalletPickRequest
    {
        [Required(ErrorMessage = "PalletId là bắt buộc")]
        public int PalletId { get; set; }

        public string? Notes { get; set; }
    }

    public class OutboundPalletPickViewModel
    {
        public int PalletId { get; set; }

        public DateTime? PickedAt { get; set; }

        public int PickedBy { get; set; }

        public string PickedByName { get; set; } = string.Empty;
    }
}
