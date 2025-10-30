using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;
using Microsoft.OpenApi.Any;

namespace TicketWebAPI.ProgramConfig.Swagger
{
    public class SwaggerFileOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // Check if this action has IFormFile parameters
            var hasFormFileParams = context.MethodInfo.GetParameters()
                .Any(p => p.ParameterType.Name.Contains("IFormFile"));

            if (!hasFormFileParams)
                return;

            // Handle the specific ImageController.UploadImage method
            if (context.MethodInfo.DeclaringType?.Name == "ImageController" &&
                context.MethodInfo.Name == "UploadImage")
            {
                operation.Parameters.Clear();
                operation.RequestBody = new OpenApiRequestBody
                {
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["multipart/form-data"] = new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema
                            {
                                Type = "object",
                                Properties = new Dictionary<string, OpenApiSchema>
                                {
                                    ["uploadType"] = new OpenApiSchema
                                    {
                                        Type = "string",
                                        Description = "Upload type: 'file' for file uploads, 'url' for URL downloads",
                                        Enum = new List<IOpenApiAny> { new OpenApiString("file"), new OpenApiString("url") }
                                    },
                                    ["imageUrl"] = new OpenApiSchema
                                    {
                                        Type = "string",
                                        Description = "Image URL (required for URL uploads)",
                                        Nullable = true
                                    },
                                    ["targetFolder"] = new OpenApiSchema
                                    {
                                        Type = "string",
                                        Description = "Target folder for storage (default: 'images')",
                                        Nullable = true
                                    },
                                    ["description"] = new OpenApiSchema
                                    {
                                        Type = "string",
                                        Description = "Optional description for the image",
                                        Nullable = true
                                    },
                                    ["image"] = new OpenApiSchema
                                    {
                                        Type = "string",
                                        Format = "binary",
                                        Description = "Image file (required for file uploads)",
                                        Nullable = true
                                    }
                                },
                                Required = new HashSet<string> { "uploadType" }
                            }
                        }
                    }
                };
            }
            // Handle other upload methods with simple file upload
            else
            {
                operation.Parameters.Clear();
                operation.RequestBody = new OpenApiRequestBody
                {
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["multipart/form-data"] = new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema
                            {
                                Type = "object",
                                Properties = new Dictionary<string, OpenApiSchema>
                                {
                                    ["file"] = new OpenApiSchema
                                    {
                                        Type = "string",
                                        Format = "binary"
                                    }
                                },
                                Required = new HashSet<string> { "file" }
                            }
                        }
                    }
                };
            }
        }
    }
}
