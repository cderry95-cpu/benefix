FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Corporate TLS interception (e.g. Cloudflare web gateway) breaks NuGet restore.
# If a corporate root CA is supplied as a build secret, trust it; otherwise this
# is a no-op and normal public CAs are used (CI, home networks, etc.).
RUN --mount=type=secret,id=corp-root \
    if [ -f /run/secrets/corp-root ]; then \
        cp /run/secrets/corp-root /usr/local/share/ca-certificates/corp-root.crt && \
        update-ca-certificates; \
        echo "Installed corporate root CA for build."; \
    fi

# Restore first with only project files present so dependency layers stay cached
# across source-only changes.
COPY src/Benefits.Domain/Benefits.Domain.csproj src/Benefits.Domain/
COPY src/Benefits.Application/Benefits.Application.csproj src/Benefits.Application/
COPY src/Benefits.Infrastructure/Benefits.Infrastructure.csproj src/Benefits.Infrastructure/
COPY src/Benefits.Api/Benefits.Api.csproj src/Benefits.Api/

RUN dotnet restore src/Benefits.Api/Benefits.Api.csproj

COPY . .

RUN dotnet publish src/Benefits.Api/Benefits.Api.csproj \
    -c Release \
    --no-restore \
    -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# 'app' (UID 1654) is the built-in non-root user of the .NET images.
COPY --from=build --chown=app:app /app/publish .

USER app

EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

ENTRYPOINT ["dotnet", "Benefits.Api.dll"]
