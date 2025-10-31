using System;
using System.Collections.Generic;

namespace WarehouseAPI.Data;

public partial class Rack
{
    public string RackId { get; set; } = null!;

    public string ZoneId { get; set; } = null!;

    public string? RackName { get; set; }

    public decimal PositionX { get; set; }

    public decimal PositionY { get; set; }

    public decimal PositionZ { get; set; }

    public decimal Length { get; set; }

    public decimal Width { get; set; }

    public decimal Height { get; set; }

    public int? MaxShelves { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Shelf> Shelves { get; set; } = new List<Shelf>();

    public virtual WarehouseZone Zone { get; set; } = null!;
}
