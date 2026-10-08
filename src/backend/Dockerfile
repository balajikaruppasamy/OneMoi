# OneMoi API — container image for Render (or any Docker host).
# Build locally:  docker build -t onemoi-api src/backend
# Render builds this automatically from GitHub (see render.yaml at the repository root).

# ── 1. Build with the .NET 10 SDK ──
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Restore first (cached as long as project files don't change)
COPY OneMoi.sln global.json ./
COPY OneMoi.Domain/OneMoi.Domain.csproj OneMoi.Domain/
COPY OneMoi.Application/OneMoi.Application.csproj OneMoi.Application/
COPY OneMoi.Infrastructure/OneMoi.Infrastructure.csproj OneMoi.Infrastructure/
COPY OneMoi.Api/OneMoi.Api.csproj OneMoi.Api/
RUN dotnet restore OneMoi.Api/OneMoi.Api.csproj
# Then the source
COPY . .
RUN dotnet publish OneMoi.Api/OneMoi.Api.csproj -c Release -o /app --no-restore

# ── 2. Run on the small ASP.NET runtime image ──
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
# Time-zone data so "today" and operator time windows use India time (IST)
RUN apt-get update && apt-get install -y --no-install-recommends tzdata && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    TZ=Asia/Kolkata
# Render provides $PORT (default 10000); Program.cs listens on it
EXPOSE 10000
USER $APP_UID
ENTRYPOINT ["dotnet", "OneMoi.Api.dll"]
