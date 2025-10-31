using System;
using System.Collections.Generic;

namespace WarehouseAPI.Data;

public partial class InboundReceipt
{
    public string ReceiptId { get; set; } = null!;

    public string WarehouseId { get; set; } = null!;

    public string CustomerId { get; set; } = null!;

    public string ReceiptNumber { get; set; } = null!;

    public int TotalItems { get; set; }

    public int TotalPallets { get; set; }

    public DateTime? InboundDate { get; set; }

    public string? Notes { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string? Status { get; set; }

    public virtual Account CreatedByNavigation { get; set; } = null!;

    public virtual Account Customer { get; set; } = null!;

    public virtual ICollection<InboundItem> InboundItems { get; set; } = new List<InboundItem>();

    public virtual Warehouse Warehouse { get; set; } = null!;
}
