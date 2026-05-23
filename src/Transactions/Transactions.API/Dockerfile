# ── Stage 1: Restore ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS restore
WORKDIR /src

COPY ["src/Transactions/Transactions.API/Transactions.API.csproj",           "src/Transactions/Transactions.API/"]
COPY ["src/Transactions/Transactions.Application/Transactions.Application.csproj", "src/Transactions/Transactions.Application/"]
COPY ["src/Transactions/Transactions.Domain/Transactions.Domain.csproj",     "src/Transactions/Transactions.Domain/"]
COPY ["src/Transactions/Transactions.Infrastructure/Transactions.Infrastructure.csproj", "src/Transactions/Transactions.Infrastructure/"]

RUN dotnet restore "src/Transactions/Transactions.API/Transactions.API.csproj"

# ── Stage 2: Build ────────────────────────────────────────────────────────────
FROM restore AS build
WORKDIR /src
COPY . .

RUN dotnet build "src/Transactions/Transactions.API/Transactions.API.csproj" \
    -c Release \
    --no-restore \
    -o /app/build

# ── Stage 3: Publish ──────────────────────────────────────────────────────────
FROM build AS publish
RUN dotnet publish "src/Transactions/Transactions.API/Transactions.API.csproj" \
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

ENTRYPOINT ["dotnet", "Transactions.API.dll"]
