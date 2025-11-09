using System;
using System.Collections.Generic;

namespace WarehouseAPI.Data;

public partial class OutboundItem
{
    public int OutboundItemId { get; set; }

    public int ReceiptId { get; set; }

    public int ItemId { get; set; }

    public int? Quantity { get; set; }

    public DateTime? RemovedAt { get; set; }

    public virtual Item Item { get; set; } = null!;

    public virtual OutboundReceipt Receipt { get; set; } = null!;
}
