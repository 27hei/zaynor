# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/Zaynor.Api/Zaynor.Api.csproj -c Release -o /app

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .

# SQLite lives on a mounted volume in production (see docker-compose.yml);
# override via ConnectionStrings__Default. Jwt__Key MUST be provided via env.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Zaynor.Api.dll"]
