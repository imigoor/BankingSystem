# ── Stage 1: Restore ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS restore
WORKDIR /src

COPY ["src/Clients/Clients.API/Clients.API.csproj",                 "src/Clients/Clients.API/"]
COPY ["src/Clients/Clients.Application/Clients.Application.csproj", "src/Clients/Clients.Application/"]
COPY ["src/Clients/Clients.Domain/Clients.Domain.csproj",           "src/Clients/Clients.Domain/"]
COPY ["src/Clients/Clients.Infrastructure/Clients.Infrastructure.csproj", "src/Clients/Clients.Infrastructure/"]

RUN dotnet restore "src/Clients/Clients.API/Clients.API.csproj"

# ── Stage 2: Build ────────────────────────────────────────────────────────────
FROM restore AS build
WORKDIR /src
COPY . .

RUN dotnet build "src/Clients/Clients.API/Clients.API.csproj" \
    -c Release \
    --no-restore \
    -o /app/build

# ── Stage 3: Publish ──────────────────────────────────────────────────────────
FROM build AS publish
RUN dotnet publish "src/Clients/Clients.API/Clients.API.csproj" \
    -c Release \
    --no-restore \
    --no-build \
    -o /app/publish \
    /p:UseAppHost=false

# ── Stage 4: Runtime (minimal image) ─────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Non-root user for security
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
USER appuser

COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Clients.API.dll"]
