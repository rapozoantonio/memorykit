# MemoryKit MCP Server - End-User Installation Guide

**Version:** 0.1.0  
**Last Updated:** December 21, 2024

This guide will walk you through installing and configuring the MemoryKit MCP Server for use with Claude Desktop.

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Installation Steps](#installation-steps)
3. [Configuration](#configuration)
4. [Verification](#verification)
5. [Using MemoryKit in Claude Desktop](#using-memorykit-in-claude-desktop)
6. [Troubleshooting](#troubleshooting)
7. [Uninstallation](#uninstallation)
8. [FAQ](#faq)

---

## Prerequisites

Before you begin, ensure you have the following installed:

### 1. Docker Desktop

MemoryKit runs the .NET API in a Docker container for cross-platform compatibility and ease of deployment.

**Download:**

- Windows: https://www.docker.com/products/docker-desktop/
- Mac: https://www.docker.com/products/docker-desktop/
- Linux: https://docs.docker.com/desktop/install/linux-install/

**Installation:**

1. Download the installer for your operating system
2. Run the installer and follow the prompts
3. Start Docker Desktop after installation
4. Verify installation by opening a terminal and running:
   ```bash
   docker --version
   ```
   You should see something like: `Docker version 24.0.7, build afdd53b`

**System Requirements:**

- **Windows:** Windows 10 64-bit: Pro, Enterprise, or Education (Build 19041 or higher) or Windows 11
- **Mac:** macOS 11 or newer (Intel or Apple Silicon)
- **Linux:** 64-bit kernel and KVM virtualization support

### 2. Node.js

The MCP server is built with TypeScript/Node.js.

**Download:** https://nodejs.org/

**Recommended Version:** Node.js 18 LTS or newer

**Verify installation:**

```bash
node --version
npm --version
```

### 3. Claude Desktop

Claude Desktop is required to use the MCP integration.

**Download:** https://claude.ai/download

**Supported Platforms:**

- Windows 10 or newer
- macOS 11 or newer

---

## Installation Steps

### Step 1: Clone the Repository

Open a terminal/command prompt and run:

```bash
git clone https://github.com/yourusername/memorykit.git
cd memorykit
```

If you don't have Git installed, you can [download it here](https://git-scm.com/downloads) or download the repository as a ZIP file from GitHub.

### Step 2: Build the Docker Image

From the repository root directory:

```bash
docker build -t memorykit-api .
```

This will:

- Build the .NET API
- Create a Docker image named `memorykit-api`
- Take approximately 2-5 minutes depending on your internet speed

**Expected output:**

```
Successfully built [image-id]
Successfully tagged memorykit-api:latest
```

**Verify the image:**

```bash
docker images | grep memorykit-api
```

You should see:

```
memorykit-api    latest    [image-id]    X minutes ago    288MB
```

### Step 3: Install MCP Server Dependencies

Navigate to the MCP server directory:

```bash
cd mcp-server
npm install
```

This installs all required Node.js dependencies.

### Step 4: Build the TypeScript Code

Compile the TypeScript code to JavaScript:

```bash
npm run build
```

This creates the `dist/` directory with compiled JavaScript files.

**Verify the build:**

```bash
# Windows
Test-Path dist\index.js

# Mac/Linux
ls -la dist/index.js
```

### Step 5: Link the MCP Server Globally

Create a global npm link so Claude Desktop can find the server:

```bash
npm link
```

**Verify the link:**

```bash
npm list -g --depth=0 | grep memorykit
```

You should see the MemoryKit MCP server listed.

---

## Configuration

### Step 1: Locate Claude Desktop Config File

The configuration file location depends on your operating system:

- **Windows:** `%APPDATA%\Claude\claude_desktop_config.json`
  - Full path typically: `C:\Users\YourUsername\AppData\Roaming\Claude\claude_desktop_config.json`
- **Mac:** `~/Library/Application Support/Claude/claude_desktop_config.json`
- **Linux:** `~/.config/Claude/claude_desktop_config.json`

**If the file doesn't exist:**

1. Create the directory:

   ```bash
   # Windows (PowerShell)
   New-Item -Path "$env:APPDATA\Claude" -ItemType Directory -Force

   # Mac/Linux
   mkdir -p "~/Library/Application Support/Claude"  # Mac
   mkdir -p ~/.config/Claude                         # Linux
   ```

2. Create the file with a basic structure:
   ```json
   {
     "mcpServers": {}
   }
   ```

### Step 2: Add MemoryKit Configuration

Edit the `claude_desktop_config.json` file and add the MemoryKit server configuration:

**Windows:**

```json
{
  "mcpServers": {
    "memorykit": {
      "command": "node",
      "args": [
        "C:\\Users\\YourUsername\\Documents\\memorykit\\mcp-server\\dist\\index.js"
      ],
      "env": {
        "DOCKER_COMPOSE_PATH": "C:\\Users\\YourUsername\\Documents\\memorykit\\docker-compose.yml"
      }
    }
  }
}
```

**Mac:**

```json
{
  "mcpServers": {
    "memorykit": {
      "command": "node",
      "args": ["/Users/yourusername/memorykit/mcp-server/dist/index.js"],
      "env": {
        "DOCKER_COMPOSE_PATH": "/Users/yourusername/memorykit/docker-compose.yml"
      }
    }
  }
}
```

**Linux:**

```json
{
  "mcpServers": {
    "memorykit": {
      "command": "node",
      "args": ["/home/yourusername/memorykit/mcp-server/dist/index.js"],
      "env": {
        "DOCKER_COMPOSE_PATH": "/home/yourusername/memorykit/docker-compose.yml"
      }
    }
  }
}
```

**Important:**

- Replace `YourUsername` / `yourusername` with your actual username
- Use **absolute paths**, not relative paths
- Use the correct path separator for your OS (`\\` for Windows, `/` for Mac/Linux)
- Ensure Docker Desktop is running before starting Claude Desktop

### Step 3: Verify JSON Syntax

Use a JSON validator to ensure your config file is valid:

- Online: https://jsonlint.com/
- Command line: `python -m json.tool claude_desktop_config.json`

Common mistakes:

- Missing commas between properties
- Trailing comma after last property
- Unescaped backslashes in Windows paths (use `\\`)

### Step 4: Restart Claude Desktop

1. Close Claude Desktop completely (check system tray/menu bar)
2. Wait 5 seconds
3. Reopen Claude Desktop

---

## Verification

### Step 1: Check Docker Container

Open a terminal and verify the Docker container started:

```bash
docker ps
```

You should see a container named `memorykit-mcp-api` with status `healthy`:

```
CONTAINER ID   IMAGE            STATUS                    PORTS                    NAMES
abc123def456   memorykit-api    Up 30 seconds (healthy)   0.0.0.0:5555->5555/tcp   memorykit-mcp-api
```

If the container is not running:

1. Check Docker Desktop is running
2. Check Claude Desktop logs for errors
3. See [Troubleshooting](#troubleshooting) section

### Step 2: Test Health Endpoint

Verify the API is responding:

```bash
# Windows (PowerShell)
Invoke-WebRequest -Uri http://localhost:5555/health

# Mac/Linux
curl http://localhost:5555/health
```

Expected response: `Healthy`

### Step 3: Check Available Tools in Claude Desktop

1. Open Claude Desktop
2. Start a new conversation
3. Look for the tool picker icon (🔧 or hammer icon)
4. You should see 6 MemoryKit tools:
   - `store_memory`
   - `retrieve_memory`
   - `search_memory`
   - `forget_memory`
   - `consolidate`
   - `get_context`

**If tools don't appear:**

- Check Claude Desktop logs (see [Troubleshooting](#troubleshooting))
- Verify config file syntax
- Ensure paths are absolute and correct
- Restart Claude Desktop again

---

## Using MemoryKit in Claude Desktop

### Basic Workflow

1. **Start a conversation with memory:**

   ```
   Please use store_memory to remember that I prefer TypeScript over JavaScript for all my projects.
   ```

2. **Retrieve past memories:**

   ```
   Use retrieve_memory to show me what you remember about my programming preferences.
   ```

3. **Search memories semantically:**

   ```
   Use search_memory to find anything we discussed about databases.
   ```

4. **Get relevant context:**

   ```
   Use get_context to bring up relevant background for our current conversation.
   ```

5. **Forget specific information:**

   ```
   Use forget_memory to delete the message about my old email address.
   ```

6. **Consolidate old memories:**
   ```
   Use consolidate to compress old memories and save space.
   ```

### Example Conversations

#### Example 1: Learning User Preferences

**You:**

> Hi Claude! I want you to remember some things about me. Please use store_memory to remember:
>
> - I'm a full-stack developer specializing in React and .NET
> - I prefer TypeScript over JavaScript
> - I work on e-commerce projects
> - I follow clean architecture principles

**Claude:**

> I'll use the store_memory tool to remember these preferences...
>
> [Tool use: store_memory]
>
> Done! I've stored your preferences. I now know you're a full-stack developer working with React and .NET, prefer TypeScript, focus on e-commerce, and follow clean architecture.

#### Example 2: Recalling Past Discussions

**You (in a new conversation):**

> What do you remember about my development preferences?

**Claude:**

> Let me check my memory...
>
> [Tool use: retrieve_memory]
>
> Based on my memory, you're a full-stack developer who specializes in React and .NET. You prefer TypeScript over JavaScript, work primarily on e-commerce projects, and follow clean architecture principles.

#### Example 3: Semantic Search

**You:**

> Use search_memory to find anything we discussed about testing.

**Claude:**

> [Tool use: search_memory]
>
> I found 3 relevant memories about testing:
>
> 1. You mentioned preferring Jest for unit testing
> 2. We discussed integration testing strategies for your e-commerce API
> 3. You asked about E2E testing with Playwright

### Advanced Usage

#### Managing Multiple Conversations

Each conversation has a unique ID. The MCP server automatically tracks conversations:

```
Conversation 1: Project discussion → conversationId: abc-123
Conversation 2: Personal preferences → conversationId: def-456
```

To retrieve from a specific conversation:

```
Use retrieve_memory with conversationId abc-123 to show project discussion history.
```

#### Using Tags for Organization

```
Store this with tags ["work", "urgent"]: I need to deploy the new feature by Friday.
```

Later:

```
Search for memories tagged with "urgent"
```

#### Forcing Consolidation

```
Use consolidate with force=true to compress all old memories immediately.
```

---

## Troubleshooting

### Issue 1: Docker Container Not Starting

**Symptoms:**

- Claude Desktop shows "Error starting MCP server"
- No `memorykit-mcp-api` container when running `docker ps`

**Solutions:**

1. **Verify Docker Desktop is running:**

   - Windows: Check system tray for Docker Desktop icon
   - Mac: Check menu bar for Docker Desktop icon
   - Open Docker Desktop GUI to confirm it's running

2. **Check port 5555 availability:**

   ```bash
   # Windows
   netstat -ano | findstr :5555

   # Mac/Linux
   lsof -i :5555
   ```

   If something is using port 5555:

   - Stop that process
   - Or modify `docker-compose.yml` and `appsettings.json` to use a different port

3. **Verify Docker image exists:**

   ```bash
   docker images | grep memorykit-api
   ```

   If missing, rebuild:

   ```bash
   docker build -t memorykit-api .
   ```

4. **Check Docker logs:**
   ```bash
   docker logs memorykit-mcp-api
   ```

### Issue 2: Tools Not Appearing in Claude Desktop

**Symptoms:**

- Claude Desktop opens but MemoryKit tools are not available
- Tool picker shows no tools or only other MCP servers

**Solutions:**

1. **Verify config file location and syntax:**

   - Check file exists at correct location
   - Validate JSON syntax: https://jsonlint.com/
   - Ensure no trailing commas

2. **Check paths are absolute:**

   ```json
   // ❌ Wrong (relative path)
   "args": ["./mcp-server/dist/index.js"]

   // ✅ Correct (absolute path)
   "args": ["C:\\Users\\YourName\\memorykit\\mcp-server\\dist\\index.js"]
   ```

3. **Check Claude Desktop logs:**

   **Windows:**

   ```powershell
   Get-Content "$env:APPDATA\Claude\logs\mcp-*.log" | Select-Object -Last 50
   ```

   **Mac:**

   ```bash
   tail -n 50 ~/Library/Logs/Claude/mcp-*.log
   ```

   **Linux:**

   ```bash
   tail -n 50 ~/.config/Claude/logs/mcp-*.log
   ```

4. **Restart Claude Desktop properly:**

   - Close all Claude windows
   - Check Task Manager/Activity Monitor and kill any remaining Claude processes
   - Wait 5-10 seconds
   - Reopen Claude Desktop

5. **Test MCP server standalone:**

   ```bash
   node C:\Users\YourName\memorykit\mcp-server\dist\index.js
   ```

   Should start without errors and show MCP server output.

### Issue 3: "Connection Refused" Errors

**Symptoms:**

- Claude shows errors like "connect ECONNREFUSED 127.0.0.1:5555"
- Tools fail when used

**Solutions:**

1. **Verify container is running and healthy:**

   ```bash
   docker ps
   docker inspect memorykit-mcp-api | grep Health
   ```

2. **Check API health endpoint:**

   ```bash
   curl http://localhost:5555/health
   ```

   Should return: `Healthy`

3. **Restart the container:**

   ```bash
   docker-compose --profile mcp restart mcp-api
   ```

4. **Check container logs for errors:**
   ```bash
   docker logs memorykit-mcp-api
   ```

### Issue 4: Permission Errors (Linux/Mac)

**Symptoms:**

- "EACCES: permission denied"
- "Cannot write to directory"

**Solutions:**

1. **Add user to docker group (Linux):**

   ```bash
   sudo usermod -aG docker $USER
   newgrp docker
   ```

2. **Fix npm global directory permissions:**

   ```bash
   mkdir ~/.npm-global
   npm config set prefix '~/.npm-global'
   echo 'export PATH=~/.npm-global/bin:$PATH' >> ~/.bashrc
   source ~/.bashrc
   ```

3. **Re-run npm link:**
   ```bash
   cd memorykit/mcp-server
   npm link
   ```

### Issue 5: Memory Not Persisting

**Symptoms:**

- Memories disappear after closing Claude Desktop
- Previous conversations not found

**Expected Behavior (v0.1):**

- MemoryKit v0.1 uses **InMemory storage**
- Data is lost when the Docker container stops
- This is **by design** for the MVP

**Solutions:**

1. **Docker Deployment (PostgreSQL):**

   - Docker containers use **PostgreSQL for persistent storage**
   - Memories survive container restarts
   - Data is stored in the `memorykit-postgres` container

   ```bash
   docker ps  # Verify postgres container is running
   ```

2. **Standalone API (InMemory):**

   - If running API outside Docker without PostgreSQL configured, it uses InMemory storage
   - Data will be lost when the process stops
   - Configure PostgreSQL connection string to enable persistence

3. **Future Enhancements (v0.2+):**
   - Azure providers (Redis, Table Storage, Cosmos DB)
   - SQLite for lightweight local persistence without Docker

### Issue 6: Slow Performance

**Symptoms:**

- Tools take several seconds to respond
- Claude Desktop feels sluggish when using MemoryKit

**Solutions:**

1. **Check Docker Desktop resource allocation:**

   - Open Docker Desktop → Settings → Resources
   - Allocate more CPU/memory if available
   - Recommended: 2+ CPUs, 2GB+ RAM

2. **Check disk space:**

   ```bash
   docker system df
   ```

   Clean up if needed:

   ```bash
   docker system prune
   ```

3. **Monitor container resources:**

   ```bash
   docker stats memorykit-mcp-api
   ```

4. **Reduce conversation size:**
   ```
   Use consolidate to compress old memories
   ```

---

## Uninstallation

### Step 1: Stop and Remove Docker Container

```bash
docker stop memorykit-mcp-api
docker rm memorykit-mcp-api
```

### Step 2: Remove Docker Image

```bash
docker rmi memorykit-api
```

### Step 3: Unlink npm Package

```bash
cd memorykit/mcp-server
npm unlink
```

### Step 4: Remove Configuration

Edit `claude_desktop_config.json` and remove the `memorykit` entry:

```json
{
  "mcpServers": {
    // Remove this section:
    // "memorykit": { ... }
  }
}
```

### Step 5: Delete Repository (Optional)

```bash
rm -rf memorykit  # Mac/Linux
rmdir /s memorykit  # Windows
```

### Step 6: Restart Claude Desktop

Close and reopen Claude Desktop to apply changes.

---

## FAQ

### Q1: Do I need to keep Docker Desktop running all the time?

**A:** Yes, while using MemoryKit. The MCP server will automatically start/stop the Docker container as needed, but Docker Desktop must be running.

### Q2: Can I use MemoryKit with multiple Claude accounts?

**A:** Currently, MemoryKit v0.1 is single-user. All conversations share the same API instance. Multi-tenant support is planned for v0.3.

### Q3: How much disk space does MemoryKit use?

**A:**

- Docker image: ~288MB
- Running container: Varies based on conversation size (InMemory storage)
- Node.js dependencies: ~50MB

### Q4: Is my data encrypted?

**A:** MemoryKit v0.1 with Docker deployment stores data in PostgreSQL without encryption at rest. Encryption at rest is planned for v0.3. **Do not store highly sensitive information** without additional database-level encryption.

### Q6: Can I export my conversation data?

**A:** Data is stored in PostgreSQL and can be exported using standard database tools (pg_dump). Native export/import API is planned for v0.2.

### Q6: What happens if the Docker container crashes?

**A:** The MCP server will detect the crash and attempt to restart the container. If it fails, you'll see errors in Claude Desktop. Check Docker logs for details.

### Q7: Can I run MemoryKit on a remote server?

**A:** Yes, but you'll need to modify the configuration:

1. Expose port 5555 on the remote server
2. Change `MEMORYKIT_API_URL` in the MCP server environment
3. Ensure network connectivity and firewall rules

Example config:

```json
{
  "mcpServers": {
    "memorykit": {
      "command": "node",
      "args": ["C:\\path\\to\\memorykit\\mcp-server\\dist\\index.js"],
      "env": {
        "MEMORYKIT_API_URL": "http://your-server.com:5555"
      }
    }
  }
}
```

### Q8: How do I update to a newer version?

```bash
cd memorykit
git pull origin main
docker build -t memorykit-api .
cd mcp-server
npm install
npm run build
```

Then restart Claude Desktop.

### Q9: Can I use MemoryKit with other MCP clients?

**A:** Yes! MemoryKit follows the standard MCP protocol. Any MCP-compatible client can use it. Just configure the client to point to the MCP server executable.

### Q10: What LLM does MemoryKit use for embeddings?

**A:** v0.1 includes a **mock LLM provider** for testing. The PostgreSQL schema supports 1536-dimensional embeddings (OpenAI-compatible) with pgvector, but real embedding generation requires Azure OpenAI configuration. Full semantic search with embeddings will be enabled in v0.2.

---

## Getting Help

### Community Support

- **GitHub Issues:** https://github.com/yourusername/memorykit/issues
- **GitHub Discussions:** https://github.com/yourusername/memorykit/discussions

### Reporting Bugs

When reporting bugs, please include:

1. Operating system and version
2. Docker Desktop version (`docker --version`)
3. Node.js version (`node --version`)
4. Claude Desktop version
5. Error messages from Claude Desktop logs
6. Output of `docker logs memorykit-mcp-api`
7. Steps to reproduce

### Feature Requests

Feature requests are welcome! Please:

1. Search existing issues first
2. Describe the use case
3. Explain how it would benefit users

---

## Next Steps

Now that MemoryKit is installed:

1. **Try the example conversations** above
2. **Experiment with different memory operations**
3. **Provide feedback** on GitHub
4. **Star the repository** if you find it useful
5. **Share with the community**

**Welcome to persistent memory for AI! 🧠✨**
