# Stage build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy toàn bộ solution con của API vào
COPY . .

# Restore & publish
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

# Stage runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

# Lắng nghe trên port 8080 trong container
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Thay WarehouseAPI.dll nếu tên project khác
ENTRYPOINT ["dotnet", "WarehouseAPI.dll"]