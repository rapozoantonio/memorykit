#!/bin/bash
# Build MemoryKit API executables for MCP deployment (Bash version for Unix systems)

set -e

echo "Building MemoryKit API executables for MCP deployment..."

OUTPUT_DIR="./dist/executables"
PROJECT="./src/MemoryKit.API/MemoryKit.API.csproj"

# Clean output directory
if [ -d "$OUTPUT_DIR" ]; then
    echo "Cleaning output directory..."
    rm -rf "$OUTPUT_DIR"
fi
mkdir -p "$OUTPUT_DIR"

# Build for each platform
PLATFORMS=("linux-x64" "osx-x64" "osx-arm64" "win-x64")

for platform in "${PLATFORMS[@]}"; do
    echo ""
    echo "Building for $platform..."
    
    dotnet publish "$PROJECT" \
        -c Release \
        -r "$platform" \
        --self-contained true \
        -p:PublishSingleFile=true \
        -p:PublishTrimmed=true \
        -p:PublishReadyToRun=true \
        -o "$OUTPUT_DIR/$platform"
    
    # Rename executable to memorykit-api
    if [ "$platform" = "win-x64" ]; then
        if [ -f "$OUTPUT_DIR/$platform/MemoryKit.API.exe" ]; then
            mv "$OUTPUT_DIR/$platform/MemoryKit.API.exe" "$OUTPUT_DIR/$platform/memorykit-api.exe"
        fi
    else
        if [ -f "$OUTPUT_DIR/$platform/MemoryKit.API" ]; then
            mv "$OUTPUT_DIR/$platform/MemoryKit.API" "$OUTPUT_DIR/$platform/memorykit-api"
            chmod +x "$OUTPUT_DIR/$platform/memorykit-api"
        fi
    fi
    
    # Show file size
    if [ "$platform" = "win-x64" ]; then
        ls -lh "$OUTPUT_DIR/$platform/memorykit-api.exe" 2>/dev/null || echo "  Warning: Executable not found"
    else
        ls -lh "$OUTPUT_DIR/$platform/memorykit-api" 2>/dev/null || echo "  Warning: Executable not found"
    fi
done

echo ""
echo "✓ Build complete! Executables in $OUTPUT_DIR"
