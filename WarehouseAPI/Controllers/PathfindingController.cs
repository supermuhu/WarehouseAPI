using Microsoft.AspNetCore.Mvc;
using WarehouseAPI.Services.Pathfinding;

namespace WarehouseAPI.Controllers;

/// <summary>
/// API Controller cho pathfinding - tìm đường đi ngắn nhất trong kho
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PathfindingController : ControllerBase
{
    private readonly IPathfindingService _pathfindingService;
    private readonly ILogger<PathfindingController> _logger;

    public PathfindingController(IPathfindingService pathfindingService, ILogger<PathfindingController> logger)
    {
        _pathfindingService = pathfindingService;
        _logger = logger;
    }

    /// <summary>
    /// Tìm đường đi từ vị trí bắt đầu tới pallet
    /// </summary>
    /// <param name="request">Thông tin request bao gồm warehouseId, palletId và options</param>
    /// <returns>Đường đi bao gồm các điểm và khoảng cách</returns>
    [HttpPost("to-pallet")]
    public async Task<ActionResult<PathResult>> FindPathToPallet([FromBody] FindPathToPalletRequest request)
    {
        if (request.WarehouseId <= 0)
        {
            return BadRequest(new PathResult { Success = false, ErrorMessage = "WarehouseId is required" });
        }

        if (request.PalletId <= 0)
        {
            return BadRequest(new PathResult { Success = false, ErrorMessage = "PalletId is required" });
        }

        _logger.LogInformation("Finding path to pallet {PalletId} in warehouse {WarehouseId}", 
            request.PalletId, request.WarehouseId);

        var result = await _pathfindingService.FindPathToPalletAsync(
            request.WarehouseId, 
            request.PalletId, 
            request.StartPosition, 
            request.Options
        );

        if (!result.Success)
        {
            _logger.LogWarning("Path not found to pallet {PalletId}: {Error}", request.PalletId, result.ErrorMessage);
        }

        return Ok(result);
    }

    /// <summary>
    /// Tìm đường đi giữa 2 điểm bất kỳ trong kho
    /// </summary>
    /// <param name="request">Thông tin request bao gồm warehouseId, start, goal và options</param>
    /// <returns>Đường đi bao gồm các điểm và khoảng cách</returns>
    [HttpPost("between-points")]
    public async Task<ActionResult<PathResult>> FindPathBetweenPoints([FromBody] FindPathBetweenPointsRequest request)
    {
        if (request.WarehouseId <= 0)
        {
            return BadRequest(new PathResult { Success = false, ErrorMessage = "WarehouseId is required" });
        }

        if (request.Start == null || request.Goal == null)
        {
            return BadRequest(new PathResult { Success = false, ErrorMessage = "Start and Goal positions are required" });
        }

        _logger.LogInformation("Finding path from ({StartX},{StartZ}) to ({GoalX},{GoalZ}) in warehouse {WarehouseId}",
            request.Start.X, request.Start.Z, request.Goal.X, request.Goal.Z, request.WarehouseId);

        var result = await _pathfindingService.FindPathBetweenPointsAsync(
            request.WarehouseId, 
            request.Start, 
            request.Goal, 
            request.Options
        );

        if (!result.Success)
        {
            _logger.LogWarning("Path not found between points: {Error}", result.ErrorMessage);
        }

        return Ok(result);
    }

    /// <summary>
    /// Tìm đường đi tới nhiều pallet (cho outbound picking)
    /// </summary>
    /// <param name="request">Thông tin request bao gồm warehouseId, palletIds và options</param>
    /// <returns>Danh sách đường đi tới từng pallet và tổng khoảng cách</returns>
    [HttpPost("to-multiple-pallets")]
    public async Task<ActionResult<MultiPathResult>> FindPathToMultiplePallets([FromBody] FindPathToMultiplePalletsRequest request)
    {
        if (request.WarehouseId <= 0)
        {
            return BadRequest(new MultiPathResult { Success = false, ErrorMessage = "WarehouseId is required" });
        }

        if (request.PalletIds == null || request.PalletIds.Count == 0)
        {
            return BadRequest(new MultiPathResult { Success = false, ErrorMessage = "PalletIds are required" });
        }

        _logger.LogInformation("Finding paths to {Count} pallets in warehouse {WarehouseId}",
            request.PalletIds.Count, request.WarehouseId);

        var result = await _pathfindingService.FindPathToMultiplePalletsAsync(
            request.WarehouseId, 
            request.PalletIds, 
            request.StartPosition, 
            request.Options
        );

        return Ok(result);
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    public ActionResult<object> Health()
    {
        return Ok(new { Status = "OK", Service = "Pathfinding", Timestamp = DateTime.UtcNow });
    }
}
