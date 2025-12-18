using System;

namespace WarehouseAPI.Data;

public partial class OutboundPalletPick
{
    public int PickId { get; set; }

    public int ReceiptId { get; set; }

    public int PalletId { get; set; }

    public DateTime? PickedAt { get; set; }

    public int PickedBy { get; set; }

    public string? Notes { get; set; }

    public virtual OutboundReceipt Receipt { get; set; } = null!;

    public virtual Pallet Pallet { get; set; } = null!;

    public virtual Account PickedByNavigation { get; set; } = null!;
}
