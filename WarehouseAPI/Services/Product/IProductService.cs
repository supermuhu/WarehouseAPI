using WarehouseAPI.ModelView.Common;
using WarehouseAPI.ModelView.Product;

namespace WarehouseAPI.Services.Product
{
    public interface IProductService
    {
        ApiResponse<List<ProductViewModel>> GetAvailableProducts(int? accountId, string role);
        ApiResponse<ProductViewModel> CreateProduct(int accountId, CreateProductRequest request);
    }
}

