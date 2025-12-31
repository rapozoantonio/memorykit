# MemoryKit MCP Server

**Give Claude Desktop persistent memory across conversations.**

---

## Quick Start

```bash
# 1. Prerequisites: Docker Desktop + Node.js 18+

# 2. Build Docker image
cd memorykit
docker build -t memorykit-api .

# 3. Install MCP server
cd mcp-server
npm install && npm run build
npm link

# 4. Configure Claude Desktop
# Edit: %APPDATA%\Claude\claude_desktop_config.json (Windows)
# Add MCP server config (see Configuration below)

# 5. Restart Claude Desktop
```

**First successful result:** ~5 minutes

---

## What It Does

The MemoryKit MCP Server enables Claude to:

| Feature            | Description                        |
| ------------------ | ---------------------------------- |
| 💾 **Store**       | Save memories from conversations   |
| 🔍 **Search**      | Find past interactions             |
| 🧠 **Context**     | Get relevant context automatically |
| 🗑️ **Forget**      | Delete specific memories           |
| 📦 **Consolidate** | Optimize memory for efficiency     |

## Architecture

```
Claude Desktop
     │ MCP Protocol (stdio)
     ↓
TypeScript MCP Server (Node.js)
     ├─ 6 MCP Tools
     ├─ API Client (HTTP)
     └─ Process Manager (Docker)
          │
          ↓
     Docker Container
          └─ .NET API (port 5555)
               └─ MemoryKit Core
```

## Prerequisites

| Requirement        | Version | Download                                                                              |
| ------------------ | ------- | ------------------------------------------------------------------------------------- |
| **Docker Desktop** | Latest  | [docker.com/products/docker-desktop](https://www.docker.com/products/docker-desktop/) |
| **Node.js**        | 18+     | [nodejs.org](https://nodejs.org/)                                                     |
| **Claude Desktop** | Latest  | [claude.ai/download](https://claude.ai/download)                                      |

**Verify installation:**

```bash
docker --version  # Should show Docker version
node --version    # Should show v18.x or higher
```

## Installation

### Step 1: Clone & Build

```bash
# Clone repository
git clone https://github.com/yourusername/memorykit.git
cd memorykit

# Build Docker image
docker build -t memorykit-api .

# Build MCP server
cd mcp-server
npm install
npm run build
npm link  # Makes globally available
```

### Step 2: Configure Claude Desktop

**Config file location:**

| OS      | Path                                                              |
| ------- | ----------------------------------------------------------------- |
| Windows | `%APPDATA%\Claude\claude_desktop_config.json`                     |
| Mac     | `~/Library/Application Support/Claude/claude_desktop_config.json` |
| Linux   | `~/.config/Claude/claude_desktop_config.json`                     |

**Add this configuration:**

```json
{
  "mcpServers": {
    "memorykit": {
      "command": "node",
      "args": ["/ABSOLUTE/PATH/TO/memorykit/mcp-server/dist/index.js"],
      "env": {
        "DOCKER_COMPOSE_PATH": "/ABSOLUTE/PATH/TO/memorykit/docker-compose.yml"
      }
    }
  }
}
```

⚠️ **Important:** Replace `/ABSOLUTE/PATH/TO/memorykit` with your actual path.

### Step 3: Restart Claude Desktop

1. Close all Claude Desktop windows
2. Quit the application completely
3. Reopen Claude Desktop
4. MCP tools should appear in the interface

## Available Tools

### Tool Reference

| Tool                 | Purpose          | Key Parameters                  |
| -------------------- | ---------------- | ------------------------------- |
| `store_memory`       | Save a message   | role, content, conversationId\* |
| `retrieve_memory`    | Get messages     | conversationId, skip*, take*    |
| `search_memory`      | Semantic search  | query, conversationId           |
| `get_context`        | Get full context | conversationId, query           |
| `forget_memory`      | Delete message   | conversationId, messageId       |
| `consolidate_memory` | Optimize storage | conversationId                  |

\*Optional parameters

### Usage Examples

**Store a memory:**

```
Use store_memory to remember: "User prefers TypeScript over JavaScript"
```

**Retrieve memories:**

```
Use retrieve_memory to get the last 5 messages from conversation abc123
```

**Search memories:**

```
Use search_memory to find all mentions of "TypeScript" in conversation abc123

Search across all memories using semantic search.

**Parameters:**

- `query` (string, required): The search query
- `conversationId` (string, optional): Limit search to specific conversation

**Example:**

```

Use search_memory to find: "What did we discuss about databases?"

```

### 4. get_context

Get relevant context for the current conversation.

**Parameters:**

- `conversationId` (string, required): The conversation ID
- `query` (string, optional): Query to filter context

**Example:**

```

Use get_context to get relevant background for conversation abc123

```

### 5. forget_memory

Delete a specific message from memory.

**Parameters:**

- `conversationId` (string, required): The conversation ID
- `messageId` (string, required): The message ID to delete

**Example:**

```

Use forget_memory to delete message xyz789 from conversation abc123

```

### 6. consolidate

Consolidate old memories to save space.

**Parameters:**

- `conversationId` (string, required): The conversation ID
- `force` (boolean, optional): Force consolidation even if threshold not met

**Example:**

```

Use consolidate on conversation abc123 to compress old memories

````

## Testing

### Run All Tests

```bash
npm test
````

This runs:

- `test-docker.js` - Docker infrastructure tests
- `test-api-client.js` - API client tests
- `test-mcp-tools.js` - MCP tools integration tests

### Test Docker Infrastructure

```bash
node test-docker.js
```

Verifies:

- Process manager can start Docker container
- API becomes healthy
- Health endpoint responds correctly

### Test API Client

```bash
node test-api-client.js
```

Tests all API endpoints:

- Create conversation
- Store messages
- Retrieve messages
- Search/query
- Get context
- Consolidate
- Delete messages

### Test MCP Tools

```bash
node test-mcp-tools.js
```

Comprehensive test suite with 9 test cases covering all tool functionality.

## Troubleshooting

### Docker Container Won't Start

**Problem:** `Error: Docker container failed to start`

**Solutions:**

1. Ensure Docker Desktop is running
2. Check if port 5555 is available:
   ```bash
   netstat -ano | findstr :5555    # Windows
   lsof -i :5555                    # Mac/Linux
   ```
3. Verify Docker image exists:
   ```bash
   docker images | grep memorykit-api
   ```
4. Check Docker logs:
   ```bash
   docker logs memorykit-mcp-api
   ```

### Tools Not Appearing in Claude Desktop

**Problem:** MCP tools don't show up in Claude Desktop UI

**Solutions:**

1. Verify configuration file syntax (valid JSON)
2. Check paths are absolute, not relative
3. Restart Claude Desktop after config changes
4. Check Claude Desktop logs for errors
5. Verify npm link was successful:
   ```bash
   npm list -g memorykit-mcp-server
   ```

### API Connection Errors

**Problem:** `Error connecting to API: connect ECONNREFUSED`

**Solutions:**

1. Verify container is running:
   ```bash
   docker ps | grep memorykit-mcp-api
   ```
2. Check container health:
   ```bash
   docker inspect memorykit-mcp-api | grep Health
   ```
3. Test health endpoint manually:
   ```bash
   curl http://localhost:5555/health
   ```
4. Restart the container:
   ```bash
   docker-compose --profile mcp restart mcp-api
   ```

### Permission Errors on Linux/Mac

**Problem:** `EACCES: permission denied`

**Solutions:**

1. Run Docker as non-root user (add user to docker group)
2. Use sudo for npm link (not recommended)
3. Change npm global directory ownership

### Port Already in Use

**Problem:** `Error: listen EADDRINUSE: address already in use 0.0.0.0:5555`

**Solutions:**

1. Stop existing container:
   ```bash
   docker stop memorykit-mcp-api
   ```
2. Change port in `docker-compose.yml` and `appsettings.json`
3. Kill process using port 5555:

   ```bash
   # Windows
   netstat -ano | findstr :5555
   taskkill /PID <PID> /F

   # Mac/Linux
   lsof -i :5555
   kill -9 <PID>
   ```

## Development

### Project Structure

```
mcp-server/
├── src/
│   ├── index.ts              # Main entry point
│   ├── process-manager.ts    # Docker lifecycle management
│   ├── api-client.ts         # HTTP client for .NET API
│   └── tools/
│       └── index.ts          # MCP tool handlers
├── dist/                     # Compiled JavaScript
├── test-docker.js            # Docker tests
├── test-api-client.js        # API tests
├── test-mcp-tools.js         # Integration tests
├── package.json
├── tsconfig.json
└── README.md
```

### Development Mode

```bash
npm run dev
```

### Adding New Tools

1. Add tool definition to `src/tools/index.ts`:

```typescript
server.setRequestHandler(ListToolsRequestSchema, async () => ({
  tools: [
    // ... existing tools
    {
      name: "my_new_tool",
      description: "Description of what it does",
      inputSchema: {
        type: "object",
        properties: {
          param1: {
            type: "string",
            description: "Description of param1",
          },
        },
        required: ["param1"],
      },
    },
  ],
}));
```

2. Add handler in the `CallToolRequestSchema` switch:

```typescript
case "my_new_tool": {
  const result = await client.myNewMethod(args.param1);
  return {
    content: [{ type: "text", text: JSON.stringify(result, null, 2) }]
  };
}
```

3. Add method to API client if needed (`src/api-client.ts`)

4. Rebuild and test:

```bash
npm run build
npm test
```

### Debugging

Enable verbose logging:

```bash
export DEBUG=memorykit:*    # Mac/Linux
set DEBUG=memorykit:*       # Windows cmd
$env:DEBUG="memorykit:*"    # Windows PowerShell
```

Run with Node.js inspector:

```bash
node --inspect dist/index.js
```

## Environment Variables

- `DOCKER_COMPOSE_PATH` - Path to docker-compose.yml (auto-detected if not set)
- `MEMORYKIT_API_URL` - Override API URL (default: http://localhost:5555)
- `MEMORYKIT_API_KEY` - API authentication key (default: mcp-local-key)
- `DEBUG` - Enable debug logging (e.g., `memorykit:*`)

## Limitations (v0.1)

- **InMemory Storage Only** - Data is lost when container stops
- **Single User** - No multi-tenant support
- **No Encryption** - Conversations stored in plaintext
- **Fixed Port** - Runs on port 5555 (configurable but requires rebuild)
- **Docker Required** - Cannot run without Docker Desktop

## Roadmap

### v0.2

- Azure storage providers (Redis, Table Storage, AI Search)
- Dynamic port allocation
- Multi-user support
- Data export/import

### v0.3

- Encryption at rest
- WebSocket support for real-time updates
- CLI tool for management

## Contributing

Contributions welcome! Please read [CONTRIBUTING.md](../CONTRIBUTING.md) first.

## License

MIT - see [LICENSE](../LICENSE) for details.

## Support

- **Issues:** [GitHub Issues](https://github.com/yourusername/memorykit/issues)
- **Discussions:** [GitHub Discussions](https://github.com/yourusername/memorykit/discussions)

## Acknowledgments

- Built with [Model Context Protocol SDK](https://github.com/anthropics/mcp)
- Powered by [MemoryKit .NET Core](../README.md)
- Inspired by cognitive memory models
