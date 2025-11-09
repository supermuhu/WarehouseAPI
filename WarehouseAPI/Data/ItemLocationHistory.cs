using System;
using System.Collections.Generic;

namespace WarehouseAPI.Data;

public partial class ItemLocationHistory
{
    public int HistoryId { get; set; }

    public int ItemId { get; set; }

    public int? PalletId { get; set; }

    public int? LocationId { get; set; }

    public string ActionType { get; set; } = null!;

    public DateTime? ActionDate { get; set; }

    public int PerformedBy { get; set; }

    public string? Notes { get; set; }

    public virtual Item Item { get; set; } = null!;

    public virtual PalletLocation? Location { get; set; }

    public virtual Pallet? Pallet { get; set; }

    public virtual Account PerformedByNavigation { get; set; } = null!;
}
