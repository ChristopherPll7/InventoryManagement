FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Directory.Build.props", "."]
COPY ["src/InventoryManagement.Domain/InventoryManagement.Domain.csproj", "src/InventoryManagement.Domain/"]
COPY ["src/InventoryManagement.Application/InventoryManagement.Application.csproj", "src/InventoryManagement.Application/"]
COPY ["src/InventoryManagement.Infrastructure/InventoryManagement.Infrastructure.csproj", "src/InventoryManagement.Infrastructure/"]
COPY ["src/InventoryManagement.Api/InventoryManagement.Api.csproj", "src/InventoryManagement.Api/"]
RUN dotnet restore "src/InventoryManagement.Api/InventoryManagement.Api.csproj"
COPY . .
RUN dotnet publish "src/InventoryManagement.Api/InventoryManagement.Api.csproj" -c Release -o /app/publish --no-restore
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "InventoryManagement.Api.dll"]
