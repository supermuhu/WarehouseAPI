using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using WarehouseAPI.Data;
using WarehouseAPI.ProgramConfig;

var builder = WebApplication.CreateBuilder(args);
var myAllowSpecificOrigins = "AllowAll";
builder.Services.AddHttpContextAccessor();

// Add Database Context
builder.Services.AddDbContext<WarehouseApiContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Warehouse")));

// Add Dependency Injection
builder.Services.AddScoped();
builder.Services.AddSingleton();
builder.Services.AddTransient();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Warehouse API", Version = "v1" });
    
    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});
// Add JWT Authentication
var secretKey = builder.Configuration["AppSettings:SecretKey"] ?? "YourDefaultSecretKeyHere123456789012";
var secretKeyBytes = Encoding.UTF8.GetBytes(secretKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(secretKeyBytes),
            ClockSkew = TimeSpan.Zero
        };
        // Xử lý token từ header Authorization - chấp nhận cả "Bearer {token}" và "{token}"
        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.Request.Headers["Authorization"].FirstOrDefault();
                if (!string.IsNullOrEmpty(token))
                {
                    // Nếu token không có prefix "Bearer ", sử dụng token trực tiếp
                    if (!token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Token = token;
                    }
                    // Nếu có "Bearer ", middleware sẽ tự động xử lý, không cần làm gì
                }
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                // Tắt www-authenticate header mặc định
                context.HandleResponse();
                // Trả về 401 với response body tùy chỉnh
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                var response = System.Text.Json.JsonSerializer.Serialize(new
                {
                    statusCode = 401,
                    message = "Unauthorized",
                    error = "Token không hợp lệ hoặc đã hết hạn"
                });
                return context.Response.WriteAsync(response);
            }
        };
    });

// Cấu hình authorization để cho phép anonymous access mặc định
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = null; // Không yêu cầu authentication mặc định
});

builder.Services.AddCors(options =>
{
    options.AddPolicy(myAllowSpecificOrigins,
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyHeader()
                   .AllowAnyMethod();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Warehouse API v1");
        });
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors(myAllowSpecificOrigins);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
//"Warehouse": "Data Source=.\\sqlexpress;Initial Catalog=WarehouseAPI;Integrated Security=True;Trust Server Certificate=True" 
//Scaffold-DbContext "Name=ConnectionStrings:Warehouse" Microsoft.EntityFrameworkCore.SqlServer -OutputDir Data -f