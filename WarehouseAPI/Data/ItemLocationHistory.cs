using System;
using System.Collections.Generic;

namespace WarehouseAPI.Data;

public partial class ItemLocationHistory
{
    public string HistoryId { get; set; } = null!;

    public string ItemId { get; set; } = null!;

    public string? PalletId { get; set; }

    public string? LocationId { get; set; }

    public string ActionType { get; set; } = null!;

    public DateTime? ActionDate { get; set; }

    public string PerformedBy { get; set; } = null!;

    public string? Notes { get; set; }

    public virtual Item Item { get; set; } = null!;

    public virtual PalletLocation? Location { get; set; }

    public virtual Pallet? Pallet { get; set; }

    public virtual Account PerformedByNavigation { get; set; } = null!;
}
