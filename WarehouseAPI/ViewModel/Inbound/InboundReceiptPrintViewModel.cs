using System;
using System.Collections.Generic;

namespace WarehouseAPI.ModelView.Inbound
{
    public class InboundReceiptPrintViewModel
    {
        public int ReceiptId { get; set; }
        public string ReceiptNumber { get; set; } = string.Empty;
        public string? Status { get; set; }
        public DateTime? InboundDate { get; set; }

        public int WarehouseId { get; set; }
        public string? WarehouseName { get; set; }

        public int? ZoneId { get; set; }
        public string? ZoneName { get; set; }

        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }

        public string? CreatedByName { get; set; }
        public string? Notes { get; set; }

        public int TotalItems { get; set; }
        public int TotalPallets { get; set; }

        public string StackMode { get; set; } = "auto";

        public List<InboundReceiptPrintPalletViewModel> Pallets { get; set; } = new List<InboundReceiptPrintPalletViewModel>();
    }

    public class InboundReceiptPrintPalletViewModel
    {
        public int PalletId { get; set; }
        public string PalletBarcode { get; set; } = string.Empty;

        public int ZoneId { get; set; }
        public string? ZoneName { get; set; }
        public string? LocationCode { get; set; }
        public decimal PositionX { get; set; }
        public decimal PositionY { get; set; }
        public decimal PositionZ { get; set; }
        public bool IsGround { get; set; }
        public int StackLevel { get; set; }
        public int? ShelfId { get; set; }

        public string PalletQrContent { get; set; } = string.Empty;

        public List<InboundReceiptPrintItemViewModel> Items { get; set; } = new List<InboundReceiptPrintItemViewModel>();
    }

    public class InboundReceiptPrintItemViewModel
    {
        public int InboundItemId { get; set; }
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
