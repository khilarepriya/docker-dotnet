FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app

COPY src/DotNetApp/ ./

EXPOSE 6060

HEALTHCHECK --interval=30s --timeout=5s --start-period=5s --retries=3 \
  CMD curl --fail http://localhost:6060/health || exit 1

ENTRYPOINT ["dotnet", "DotNetApp.dll"]

