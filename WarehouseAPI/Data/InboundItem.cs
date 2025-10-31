using System;
using System.Collections.Generic;

namespace WarehouseAPI.Data;

public partial class InboundItem
{
    public string InboundItemId { get; set; } = null!;

    public string ReceiptId { get; set; } = null!;

    public string ItemId { get; set; } = null!;

    public string PalletId { get; set; } = null!;

    public int? Quantity { get; set; }

    public virtual Item Item { get; set; } = null!;

    public virtual Pallet Pallet { get; set; } = null!;

    public virtual InboundReceipt Receipt { get; set; } = null!;
}
