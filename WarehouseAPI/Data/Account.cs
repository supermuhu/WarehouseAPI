using System;
using System.Collections.Generic;

namespace WarehouseAPI.Data;

public partial class Account
{
    public int AccountId { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? FullName { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string Role { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<InboundReceipt> InboundReceiptCreatedByNavigations { get; set; } = new List<InboundReceipt>();

    public virtual ICollection<InboundReceipt> InboundReceiptCustomers { get; set; } = new List<InboundReceipt>();

    public virtual ICollection<ItemLocationHistory> ItemLocationHistories { get; set; } = new List<ItemLocationHistory>();

    public virtual ICollection<Item> Items { get; set; } = new List<Item>();

    public virtual ICollection<OutboundReceipt> OutboundReceiptCreatedByNavigations { get; set; } = new List<OutboundReceipt>();

    public virtual ICollection<OutboundReceipt> OutboundReceiptCustomers { get; set; } = new List<OutboundReceipt>();

    public virtual ICollection<WarehouseZone> WarehouseZones { get; set; } = new List<WarehouseZone>();

    public virtual ICollection<Warehouse> Warehouses { get; set; } = new List<Warehouse>();
}
