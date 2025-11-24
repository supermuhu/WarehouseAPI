using System;

namespace WarehouseAPI.Data;

public partial class PalletTemplate
{
    public int TemplateId { get; set; }

    public string TemplateName { get; set; } = null!;

    public string? PalletType { get; set; }

    public decimal Length { get; set; }

    public decimal Width { get; set; }

    public decimal Height { get; set; }

    public decimal MaxWeight { get; set; }

    public decimal MaxStackHeight { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

