using Microsoft.EntityFrameworkCore;
using WarehouseAPI.Data;
using WarehouseAPI.ModelView.Common;
using WarehouseAPI.ModelView.Product;

namespace WarehouseAPI.Services.Product
{
    public class ProductService : IProductService
    {
        private readonly WarehouseApiContext _context;

        public ProductService(WarehouseApiContext context)
        {
            _context = context;
        }

        public ApiResponse<List<ProductViewModel>> GetAvailableProducts(int? accountId, string role)
        {
            try
            {
                if (accountId == null)
                {
                    return ApiResponse<List<ProductViewModel>>.Fail(
                        "Token không hợp lệ",
                        "UNAUTHORIZED",
                        null,
                        401
                    );
                }

                // Lấy danh sách products: của customer hoặc của admin
                var query = _context.Products
                    .Where(p => p.Status == "active");

                // Nếu là customer, chỉ lấy products của họ hoặc của admin
                if (role == "customer")
                {
                    // Lấy admin account IDs - sử dụng materialize trước để tránh lỗi mapping
                    var adminIds = _context.Accounts
                        .Where(a => a.Role == "admin")
                        .Select(a => a.AccountId)
                        .AsEnumerable()
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

                return ApiResponse<List<ProductViewModel>>.Ok(
                    products,
                    "Lấy danh sách products thành công"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<List<ProductViewModel>>.Fail(
                    $"Lỗi khi lấy danh sách products: {ex.Message}",
                    "INTERNAL_ERROR",
                    null,
                    500
                );
            }
        }

        public ApiResponse<ProductViewModel> CreateProduct(int accountId, CreateProductRequest request)
        {
            try
            {
                // Kiểm tra product_code đã tồn tại chưa
                var existingProduct = _context.Products
                    .FirstOrDefault(p => p.ProductCode == request.ProductCode);

                if (existingProduct != null)
                {
                    return ApiResponse<ProductViewModel>.Fail(
                        "Mã sản phẩm đã tồn tại trong hệ thống",
                        "DUPLICATE_PRODUCT_CODE",
                        null,
                        400
                    );
                }

                // Tạo product mới
                var product = new WarehouseAPI.Data.Product
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
                    CreateUser = accountId,
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

                return ApiResponse<ProductViewModel>.Ok(
                    productViewModel,
                    "Tạo product thành công",
                    201
                );
            }
            catch (DbUpdateException ex)
            {
                return ApiResponse<ProductViewModel>.Fail(
                    $"Lỗi khi tạo product: {ex.InnerException?.Message ?? ex.Message}",
                    "DATABASE_ERROR",
                    null,
                    500
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<ProductViewModel>.Fail(
                    $"Lỗi khi tạo product: {ex.Message}",
                    "INTERNAL_ERROR",
                    null,
                    500
                );
            }
        }
    }
}

