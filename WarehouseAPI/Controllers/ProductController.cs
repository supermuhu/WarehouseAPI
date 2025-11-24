using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseAPI.Data;
using WarehouseAPI.Helpers;
using WarehouseAPI.ModelView.Common;
using WarehouseAPI.ModelView.Product;

namespace WarehouseAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProductController : ControllerBase
    {
        private readonly WarehouseApiContext _context;

        public ProductController(WarehouseApiContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy danh sách products mà customer có thể sử dụng
        /// Chỉ lấy products có create_user = customer_id hoặc admin
        /// </summary>
        /// <returns>Danh sách products</returns>
        [HttpGet("available")]
        public IActionResult GetAvailableProducts()
        {
            try
            {
                var accountId = Utils.GetCurrentAccountId(User);
                var role = Utils.GetCurrentRole(User);

                if (accountId == null)
                {
                    var errorResult = ApiResponse<List<ProductViewModel>>.Fail(
                        "Token không hợp lệ",
                        "UNAUTHORIZED",
                        null,
                        401
                    );
                    return Unauthorized(errorResult);
                }

                // Lấy danh sách products: của customer hoặc của admin
                var query = _context.Products
                    .Where(p => p.Status == "active");

                // Nếu là customer, chỉ lấy products của họ hoặc của admin
                if (role == "customer")
                {
                    // Lấy admin account IDs
                    var adminIds = _context.Accounts
                        .Where(a => a.Role == "admin")
                        .Select(a => a.AccountId)
                        .ToList();

                    query = query.Where(p => 
                        p.CreateUser == accountId || 
                        (p.CreateUser.HasValue && adminIds.Contains(p.CreateUser.Value))
                    );
                }
                // Nếu là admin hoặc warehouse_owner, lấy tất cả

                var products = query
                    .OrderBy(p => p.ProductName)
                    .Select(p => new ProductViewModel
                    {
                        ProductId = p.ProductId,
                        ProductCode = p.ProductCode,
                        ProductName = p.ProductName,
                        Description = p.Description,
                        Unit = p.Unit,
                        Category = p.Category,
                        StandardLength = p.StandardLength,
                        StandardWidth = p.StandardWidth,
                        StandardHeight = p.StandardHeight,
                        StandardWeight = p.StandardWeight,
                        IsFragile = p.IsFragile,
                        IsHazardous = p.IsHazardous,
                        StorageConditions = p.StorageConditions,
                        CreateUser = p.CreateUser,
                        Status = p.Status
                    })
                    .ToList();

                var result = ApiResponse<List<ProductViewModel>>.Ok(
                    products,
                    "Lấy danh sách products thành công"
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                var result = ApiResponse<List<ProductViewModel>>.Fail(
                    $"Lỗi khi lấy danh sách products: {ex.Message}",
                    "INTERNAL_ERROR",
                    null,
                    500
                );
                return StatusCode(500, result);
            }
        }

        /// <summary>
        /// Tạo custom product mới cho người dùng
        /// Product sẽ được gán create_user = accountId của người tạo
        /// </summary>
        /// <param name="request">Thông tin product cần tạo</param>
        /// <returns>Thông tin product vừa tạo</returns>
        [HttpPost("create")]
        public IActionResult CreateProduct([FromBody] CreateProductRequest request)
        {
            try
            {
                var accountId = Utils.GetCurrentAccountId(User);

                if (accountId == null)
                {
                    var errorResult = ApiResponse<ProductViewModel>.Fail(
                        "Token không hợp lệ",
                        "UNAUTHORIZED",
                        null,
                        401
                    );
                    return Unauthorized(errorResult);
                }

                // Kiểm tra product_code đã tồn tại chưa
                var existingProduct = _context.Products
                    .FirstOrDefault(p => p.ProductCode == request.ProductCode);

                if (existingProduct != null)
                {
                    var errorResult = ApiResponse<ProductViewModel>.Fail(
                        "Mã sản phẩm đã tồn tại trong hệ thống",
                        "DUPLICATE_PRODUCT_CODE",
                        null,
                        400
                    );
                    return BadRequest(errorResult);
                }

                // Tạo product mới
                var product = new Product
                {
                    ProductCode = request.ProductCode,
                    ProductName = request.ProductName,
                    Description = request.Description,
                    Unit = request.Unit,
                    Category = request.Category,
                    StandardLength = request.StandardLength,
                    StandardWidth = request.StandardWidth,
                    StandardHeight = request.StandardHeight,
                    StandardWeight = request.StandardWeight,
                    IsFragile = request.IsFragile ?? false,
                    IsHazardous = request.IsHazardous ?? false,
                    StorageConditions = request.StorageConditions,
                    CreateUser = accountId.Value,
                    Status = "active",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.Products.Add(product);
                _context.SaveChanges();

                var productViewModel = new ProductViewModel
                {
                    ProductId = product.ProductId,
                    ProductCode = product.ProductCode,
                    ProductName = product.ProductName,
                    Description = product.Description,
                    Unit = product.Unit,
                    Category = product.Category,
                    StandardLength = product.StandardLength,
                    StandardWidth = product.StandardWidth,
                    StandardHeight = product.StandardHeight,
                    StandardWeight = product.StandardWeight,
                    IsFragile = product.IsFragile,
                    IsHazardous = product.IsHazardous,
                    StorageConditions = product.StorageConditions,
                    CreateUser = product.CreateUser,
                    Status = product.Status
                };

                var result = ApiResponse<ProductViewModel>.Ok(
                    productViewModel,
                    "Tạo product thành công",
                    201
                );

                return StatusCode(201, result);
            }
            catch (DbUpdateException ex)
            {
                var result = ApiResponse<ProductViewModel>.Fail(
                    $"Lỗi khi tạo product: {ex.InnerException?.Message ?? ex.Message}",
                    "DATABASE_ERROR",
                    null,
                    500
                );
                return StatusCode(500, result);
            }
            catch (Exception ex)
            {
                var result = ApiResponse<ProductViewModel>.Fail(
                    $"Lỗi khi tạo product: {ex.Message}",
                    "INTERNAL_ERROR",
                    null,
                    500
                );
                return StatusCode(500, result);
            }
        }
    }
}

