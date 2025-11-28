using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseAPI.Helpers;
using WarehouseAPI.ModelView.Product;
using WarehouseAPI.Services.Product;

namespace WarehouseAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductController(IProductService productService)
        {
            _productService = productService;
        }

        /// <summary>
        /// Lấy danh sách products mà customer có thể sử dụng
        /// Chỉ lấy products có create_user = customer_id hoặc admin
        /// </summary>
        /// <returns>Danh sách products</returns>
        [HttpGet("available")]
        public IActionResult GetAvailableProducts()
        {
            var accountId = Utils.GetCurrentAccountId(User);
            var role = Utils.GetCurrentRole(User);

            var result = _productService.GetAvailableProducts(accountId, role);
            if (result.StatusCode == 200)
            {
                return Ok(result);
            }
            return StatusCode(result.StatusCode, result);
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
            var accountId = Utils.GetCurrentAccountId(User);

            if (accountId == null)
            {
                return Unauthorized(new { message = "Token không hợp lệ" });
            }

            var result = _productService.CreateProduct(accountId.Value, request);
            if (result.StatusCode == 201)
            {
                return StatusCode(201, result);
            }
            return StatusCode(result.StatusCode, result);
        }
    }
}

