using System.Text.Json.Serialization;

namespace WarehouseAPI.ModelView.Common
{
    public class ApiResponse<T>
    {
        public int StatusCode { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public ApiError? Error { get; set; }

        public static ApiResponse<T> Ok(T data, string message = "Success", int statusCode = 200)
        {
            return new ApiResponse<T>
            {
                StatusCode = statusCode,
                Success = true,
                Message = message,
                Data = data
            };
        }

        public static ApiResponse<T> Fail(string message, string errorCode = "Error", object? validationErrors = null, int statusCode = 400)
        {
            return new ApiResponse<T>
            {
                StatusCode = statusCode,
                Success = false,
                Message = message,
                Data = default,
                Error = new ApiError
                {
                    Code = errorCode,
                    Details = message,
                    ValidationErrors = validationErrors
                }
            };
        }
    }

    public class ApiError
    {
        public string Code { get; set; } = "Error";
        public string? Details { get; set; }
        public object? ValidationErrors { get; set; }

        // Optional fields for quota-related errors
        public int? CurrentImageCount { get; set; }
        public int? MaxImagesAllowed { get; set; }
    }
}

