FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY CodeArena.Domain/CodeArena.Domain.csproj CodeArena.Domain/
COPY CodeArena.Application/CodeArena.Application.csproj CodeArena.Application/
COPY CodeArena.Infrastructure/CodeArena.Infrastructure.csproj CodeArena.Infrastructure/
COPY CodeArena.API/CodeArena.API.csproj CodeArena.API/
RUN dotnet restore CodeArena.API/CodeArena.API.csproj

COPY . .
RUN dotnet publish CodeArena.API/CodeArena.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Note: running as root so named Podman volumes (owned by host user = uid 0 inside
# rootless containers) are writable. Safe for local dev; revisit for prod VPS.
RUN mkdir -p /app/uploads/seed /app/uploads/submissions/src

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "CodeArena.API.dll"]
