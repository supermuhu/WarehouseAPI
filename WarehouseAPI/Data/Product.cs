using System;
using System.Collections.Generic;

namespace WarehouseAPI.Data;

public partial class Product
{
    public int ProductId { get; set; }

    public string ProductCode { get; set; } = null!;

    public string ProductName { get; set; } = null!;

    public string? Description { get; set; }

    public string Unit { get; set; } = null!;

    public string? Category { get; set; }

    public decimal? StandardLength { get; set; }

    public decimal? StandardWidth { get; set; }

    public decimal? StandardHeight { get; set; }

    public decimal? StandardWeight { get; set; }

    public bool? IsFragile { get; set; }

    public bool? IsHazardous { get; set; }

    public string? StorageConditions { get; set; }

    public int? CreateUser { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? Status { get; set; }

    public virtual Account? CreateUserNavigation { get; set; }

    public virtual ICollection<Item> Items { get; set; } = new List<Item>();
}
