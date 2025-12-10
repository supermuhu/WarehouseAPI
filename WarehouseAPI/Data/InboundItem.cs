using System;
using System.Collections.Generic;

namespace WarehouseAPI.Data;

public partial class InboundItem
{
    public int InboundItemId { get; set; }

    public int ReceiptId { get; set; }

    public int ItemId { get; set; }

    public int PalletId { get; set; }

    public int Quantity { get; set; }

    public string StackMode { get; set; } = "auto";

    public virtual ICollection<InboundItemStackUnit> InboundItemStackUnits { get; set; } = new List<InboundItemStackUnit>();

    public virtual Item Item { get; set; } = null!;

    public virtual Pallet Pallet { get; set; } = null!;

    public virtual InboundReceipt Receipt { get; set; } = null!;
}
