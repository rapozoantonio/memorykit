# MemoryKit MCP Server

Persistent memory layer for AI agents using the Model Context Protocol (MCP).

## Development

### Install Dependencies

```bash
npm install
```

### Build

```bash
npm run build
```

### Development Mode

```bash
npm run dev
```

## Project Structure

```
src/
├── index.ts              # Entry point, MCP server setup
├── api-client.ts         # HTTP client for .NET API
├── process-manager.ts    # Sidecar process lifecycle
└── tools/
    └── index.ts          # MCP tool handlers
```

## Status

🚧 **Under Development** - Part of CHUNK 3-5 implementation

## License

MIT
