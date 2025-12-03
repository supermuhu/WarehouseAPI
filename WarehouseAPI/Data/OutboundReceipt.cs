using System;
using System.Collections.Generic;

namespace WarehouseAPI.Data;

public partial class OutboundReceipt
{
    public int ReceiptId { get; set; }

    public int WarehouseId { get; set; }

    public int CustomerId { get; set; }

    public string ReceiptNumber { get; set; } = null!;

    public int TotalItems { get; set; }

    public DateTime? OutboundDate { get; set; }

    public string? Notes { get; set; }

    public int CreatedBy { get; set; }

    public string Status { get; set; } = null!;

    public virtual Account CreatedByNavigation { get; set; } = null!;

    public virtual Account Customer { get; set; } = null!;

    public virtual Warehouse Warehouse { get; set; } = null!;

    public virtual ICollection<OutboundItem> OutboundItems { get; set; } = new List<OutboundItem>();
}
