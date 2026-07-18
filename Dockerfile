# Single-image build: the SPA is compiled same-origin and embedded into the
# API's wwwroot, so one container serves the whole product (used by Railway
# and any single-service host; docker-compose still offers the split setup).

# ---- Frontend build ----
FROM node:20-alpine AS webbuild
WORKDIR /web
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ .
# Empty API origin => the SPA calls /api relatively (same origin, no CORS)
ENV VITE_API_URL=
RUN npm run build

# ---- Backend build ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS apibuild
WORKDIR /src
COPY backend/ .
RUN dotnet publish src/Zaynor.Api/Zaynor.Api.csproj -c Release -o /app

# ---- Runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=apibuild /app .
COPY --from=webbuild /web/dist ./wwwroot
EXPOSE 8080
# Binds to the platform-provided PORT (Railway etc.), defaulting to 8080.
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet Zaynor.Api.dll"]
