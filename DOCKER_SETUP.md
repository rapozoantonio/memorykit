
# Docker + PostgreSQL Setup for MemoryKit

## Quick Start

### 1. Start MemoryKit with Docker

```bash
# Build and start all services (API + PostgreSQL + Redis)
docker-compose up -d

# Check status
docker-compose ps

# View logs
docker logs memorykit-api -f
```

### 2. Run Tests

```bash
# Test persistence and API functionality
.\docker-test.ps1
```

### 3. Connect Claude Desktop via MCP

#### Option A: Use Docker API (Recommended)

The API is already running in Docker at `http://localhost:8080`

1. Build the MCP server:

```bash
cd mcp-server
npm install
npm run build
```

2. Configure Claude Desktop by editing `%APPDATA%\Claude\claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "memorykit": {
      "command": "node",
      "args": [
        "C:\\Users\\rapoz\\Documents\\web-dev\\memorykit\\mcp-server\\dist\\index.js"
      ],
      "env": {
        "MEMORYKIT_API_KEY": "mcp-local-key",
        "MEMORYKIT_API_URL": "http://localhost:8080"
      }
    }
  }
}
```

**Note:** Use double backslashes (`\\`) in Windows paths!

3. Restart Claude Desktop

4. Test in Claude by asking:
   - "Store a memory that I prefer PostgreSQL databases"
   - "What do you remember about my database preferences?"

## Architecture

```
┌─────────────────┐
│  Claude Desktop │
└────────┬────────┘
         │ MCP Protocol
         ▼
┌─────────────────┐
│  MCP Server     │ (Node.js)
│  (port: stdio)  │
└────────┬────────┘
         │ HTTP REST
         ▼
┌─────────────────┐
│  MemoryKit API  │ (Docker Container)
│  (port: 8080)   │
└────────┬────────┘
         │
         ├──► PostgreSQL (port: 5432) - Persistent storage
         └──► Redis (port: 6379) - Cache

```

## Data Persistence

- **PostgreSQL Volume**: `postgres_data` - Persists all memories
- **Redis Volume**: `redis-data` - Persists cache
- **Location**: Docker managed volumes

To inspect data:

```bash
# Access PostgreSQL
docker exec -it memorykit-postgres psql -U memorykit -d memorykit

# Query memories
SELECT * FROM working_memories LIMIT 5;
SELECT * FROM semantic_facts LIMIT 5;
```

## Configuration

### Environment Variables (docker-compose.yml)

- `MemoryKit__StorageProvider=PostgreSQL` - Use PostgreSQL instead of SQLite
- `ConnectionStrings__PostgreSQL` - Database connection string
- `ApiKeys__ValidKeys__0=mcp-local-key` - API key for MCP server
- `ApiKeys__UserMappings__mcp-local-key=mcp-user` - User mapping

### Ports

- `8080` - MemoryKit API (HTTP)
- `5555` - Alternative API port
- `5432` - PostgreSQL
- `6379` - Redis

## Testing

### Manual API Test

```bash
# Store memory
curl -X POST http://localhost:8080/api/memories/working \
  -H "X-API-Key: mcp-local-key" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "mcp-user",
    "conversationId": "test-001",
    "content": "Docker test memory",
    "importance": 0.8
  }'

# Retrieve memories
curl http://localhost:8080/api/memories/working/mcp-user \
  -H "X-API-Key: mcp-local-key"
```

### Verify Persistence

```bash
# Restart API
docker-compose restart api

# Check data still exists
curl http://localhost:8080/api/memories/working/mcp-user \
  -H "X-API-Key: mcp-local-key"
```

## Troubleshooting

### API won't start

```bash
# Check logs
docker logs memorykit-api

# Check PostgreSQL health
docker exec memorykit-postgres pg_isready -U memorykit
```

### Can't connect from Claude

1. Verify API is running: `curl http://localhost:8080/health`
2. Check MCP server build: `cd mcp-server && npm run build`
3. Verify Claude config path: `%APPDATA%\Claude\claude_desktop_config.json`
4. Restart Claude Desktop completely

### Data not persisting

```bash
# Check volumes
docker volume ls | findstr memorykit

# Inspect PostgreSQL
docker exec memorykit-postgres psql -U memorykit -d memorykit -c "\dt"
```

## Cleanup

```bash
# Stop containers
docker-compose down

# Remove volumes (deletes all data!)
docker-compose down -v

# Remove images
docker rmi memorykit-api:latest
```
