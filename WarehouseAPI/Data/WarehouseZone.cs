using System;
using System.Collections.Generic;

namespace WarehouseAPI.Data;

public partial class WarehouseZone
{
    public string ZoneId { get; set; } = null!;

    public string WarehouseId { get; set; } = null!;

    public string? ZoneName { get; set; }

    public string? CustomerId { get; set; }

    public decimal PositionX { get; set; }

    public decimal PositionY { get; set; }

    public decimal PositionZ { get; set; }

    public decimal Length { get; set; }

    public decimal Width { get; set; }

    public decimal Height { get; set; }

    public string ZoneType { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual Account? Customer { get; set; }

    public virtual ICollection<PalletLocation> PalletLocations { get; set; } = new List<PalletLocation>();

    public virtual ICollection<Rack> Racks { get; set; } = new List<Rack>();

    public virtual Warehouse Warehouse { get; set; } = null!;
}
