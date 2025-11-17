# =============================================================================
# Multi-stage Dockerfile for MemoryKit API
# Optimized for production with minimal image size and security hardening
# =============================================================================

# =============================================================================
# Stage 1: Build
# =============================================================================
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Copy solution and project files
COPY MemoryKit.sln ./
COPY src/MemoryKit.Domain/MemoryKit.Domain.csproj ./src/MemoryKit.Domain/
COPY src/MemoryKit.Application/MemoryKit.Application.csproj ./src/MemoryKit.Application/
COPY src/MemoryKit.Infrastructure/MemoryKit.Infrastructure.csproj ./src/MemoryKit.Infrastructure/
COPY src/MemoryKit.API/MemoryKit.API.csproj ./src/MemoryKit.API/

# Restore dependencies (cached layer)
RUN dotnet restore

# Copy source code
COPY src/ ./src/

# Build and publish
RUN dotnet publish src/MemoryKit.API/MemoryKit.API.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    -p:PublishReadyToRun=true \
    -p:PublishSingleFile=false \
    -p:DebugType=None \
    -p:DebugSymbols=false

# =============================================================================
# Stage 2: Runtime
# =============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime

# Install required packages for production
RUN apk add --no-cache \
    ca-certificates \
    tzdata \
    icu-libs

# Create non-root user
RUN addgroup -g 1000 memorykit && \
    adduser -D -u 1000 -G memorykit memorykit

# Set working directory
WORKDIR /app

# Copy published app from build stage
COPY --from=build --chown=memorykit:memorykit /app/publish .

# Switch to non-root user
USER memorykit

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    TZ=UTC

# Set entrypoint
ENTRYPOINT ["dotnet", "MemoryKit.API.dll"]
