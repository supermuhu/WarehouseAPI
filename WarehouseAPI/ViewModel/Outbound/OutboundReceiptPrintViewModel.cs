using System;
using System.Collections.Generic;

namespace WarehouseAPI.ModelView.Outbound
{
    public class OutboundReceiptPrintViewModel
    {
        public int ReceiptId { get; set; }
        public string ReceiptNumber { get; set; } = string.Empty;
        public string? Status { get; set; }
        public DateTime? OutboundDate { get; set; }

        public int WarehouseId { get; set; }
        public string? WarehouseName { get; set; }

        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }

        public string? CreatedByName { get; set; }
        public string? Notes { get; set; }

        public int TotalItems { get; set; }

        public List<OutboundReceiptPrintItemViewModel> Items { get; set; } = new List<OutboundReceiptPrintItemViewModel>();
    }

    public class OutboundReceiptPrintItemViewModel
    {
        public int OutboundItemId { get; set; }
        public int ItemId { get; set; }
        public string QrCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;

        public int ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;

        public string? Unit { get; set; }

        public int Quantity { get; set; }

        public DateTime? ManufacturingDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? BatchNumber { get; set; }

        public decimal? UnitPrice { get; set; }
        public decimal? TotalAmount { get; set; }
    }
}
