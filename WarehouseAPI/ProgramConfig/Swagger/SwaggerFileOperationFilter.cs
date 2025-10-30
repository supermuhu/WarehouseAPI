using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace TicketWebAPI.ProgramConfig.Swagger
{
    public class SwaggerFileOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.OperationId != null && operation.OperationId.ToLower().Contains("upload"))
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
