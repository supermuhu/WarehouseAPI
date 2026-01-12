using System.Collections.Generic;

namespace WarehouseAPI.ViewModel.Warehouse
{
    public class RackDto
    {
        public int RackId { get; set; }
        public int ZoneId { get; set; }
        public string? RackName { get; set; }
        public decimal PositionX { get; set; }
        public decimal PositionY { get; set; }
        public decimal PositionZ { get; set; }
        public decimal Length { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }
        public int? MaxShelves { get; set; }
    }

    public class CreateRackRequest
    {
        public string? RackName { get; set; }
        public decimal PositionX { get; set; }
        public decimal PositionZ { get; set; }
        public decimal Length { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }
        public int? MaxShelves { get; set; }
        public decimal RotationY { get; set; }
    }

    public class UpdateRackRequest
    {
        public string? RackName { get; set; }
        public decimal PositionX { get; set; }
        public decimal PositionZ { get; set; }
        public decimal? Height { get; set; }
        public decimal RotationY { get; set; }
    }

    public class RackPositionUpdateItem
    {
        public int RackId { get; set; }
        public decimal PositionX { get; set; }
        public decimal PositionZ { get; set; }
        public decimal RotationY { get; set; }
    }

    public class BulkUpdateRackPositionsRequest
    {
        public List<RackPositionUpdateItem> Racks { get; set; } = new List<RackPositionUpdateItem>();
    }
}
