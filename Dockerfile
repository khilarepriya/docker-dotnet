# Build stage
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# Copy everything and publish the main project
COPY . .
RUN dotnet restore "src/DotNetApp/DotNetApp.csproj"
RUN dotnet publish "src/DotNetApp/DotNetApp.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app

# Copy published output
COPY --from=build /app/publish .

# Expose port
EXPOSE 6060

# Health check
HEALTHCHECK --interval=30s --timeout=5s --start-period=5s --retries=3 \
  CMD curl --fail http://localhost:6060/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "DotNetApp.dll"]

