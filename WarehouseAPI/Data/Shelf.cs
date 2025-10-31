using System;
using System.Collections.Generic;

namespace WarehouseAPI.Data;

public partial class Shelf
{
    public string ShelfId { get; set; } = null!;

    public string RackId { get; set; } = null!;

    public int ShelfLevel { get; set; }

    public decimal PositionY { get; set; }

    public decimal Length { get; set; }

    public decimal Width { get; set; }

    public decimal? MaxWeight { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<PalletLocation> PalletLocations { get; set; } = new List<PalletLocation>();

    public virtual Rack Rack { get; set; } = null!;
}
