using System;
using System.Collections.Generic;

namespace WarehouseAPI.Data;

public partial class InboundReceipt
{
    public int ReceiptId { get; set; }

    public int WarehouseId { get; set; }

    public int? ZoneId { get; set; }

    public int CustomerId { get; set; }

    public string ReceiptNumber { get; set; } = null!;

    public int TotalItems { get; set; }

    public int TotalPallets { get; set; }

    public DateTime? InboundDate { get; set; }

    public string? Notes { get; set; }

    public int CreatedBy { get; set; }

    public string Status { get; set; } = null!;

    public string? AutoStackTemplate { get; set; }

    public string StackMode { get; set; } = null!;

    public virtual Account CreatedByNavigation { get; set; } = null!;

    public virtual Account Customer { get; set; } = null!;

    public virtual ICollection<InboundItem> InboundItems { get; set; } = new List<InboundItem>();

    public virtual Warehouse Warehouse { get; set; } = null!;
}
