using System;
using System.Collections.Generic;

namespace WarehouseAPI.Data;

public partial class WarehouseZone
{
    public int ZoneId { get; set; }

    public int WarehouseId { get; set; }

    public string? ZoneName { get; set; }

    public int? CustomerId { get; set; }

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

    public virtual ICollection<ZoneLayoutConfig> ZoneLayoutConfigs { get; set; } = new List<ZoneLayoutConfig>();
}
