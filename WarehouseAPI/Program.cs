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
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy(myAllowSpecificOrigins,
        builder =>
        {
            builder.WithOrigins(
                       "http://localhost:4000",
                       "http://26.65.103.147:4000",
                       "http://192.168.1.241:4000"
                   )
                   .AllowAnyHeader()
                   .AllowAnyMethod()
                   .AllowCredentials();
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
//Scaffold-DbContext "Name=ConnectionStrings:Warehouse" Microsoft.EntityFrameworkCore.SqlServer -OutputDir Data -f