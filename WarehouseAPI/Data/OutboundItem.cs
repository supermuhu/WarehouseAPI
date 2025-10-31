using System;
using System.Collections.Generic;

namespace WarehouseAPI.Data;

public partial class OutboundItem
{
    public string OutboundItemId { get; set; } = null!;

    public string ReceiptId { get; set; } = null!;

    public string ItemId { get; set; } = null!;

    public int? Quantity { get; set; }

    public DateTime? RemovedAt { get; set; }

    public virtual Item Item { get; set; } = null!;

    public virtual OutboundReceipt Receipt { get; set; } = null!;
}
