import { spawn, ChildProcess } from 'child_process';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';
import axios from 'axios';

// ES module compatibility
const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

export interface ProcessManagerConfig {
  executablePath?: string;
  port?: number;
  apiKey: string;
  healthCheckRetries?: number;
  healthCheckIntervalMs?: number;
  useDotnetRun?: boolean; // For development testing
}

export class ProcessManager {
  private process: ChildProcess | null = null;
  private port: number;
  private apiKey: string;
  private executablePath: string;
  private useDotnetRun: boolean;

  constructor(config: ProcessManagerConfig) {
    this.port = config.port || 5555;
    this.apiKey = config.apiKey;
    this.useDotnetRun = config.useDotnetRun || false;
    this.executablePath = config.executablePath || this.getDefaultExecutable();
  }

  private getDefaultExecutable(): string {
    if (this.useDotnetRun) {
      // Return path to the .csproj file
      return join(__dirname, '..', '..', 'src', 'MemoryKit.API', 'MemoryKit.API.csproj');
    }

    const os = process.platform;
    const arch = process.arch;
    
    let platformDir: string;
    if (os === 'linux') {
      platformDir = 'linux-x64';
    } else if (os === 'darwin') {
      platformDir = arch === 'arm64' ? 'osx-arm64' : 'osx-x64';
    } else if (os === 'win32') {
      platformDir = 'win-x64';
    } else {
      throw new Error(`Unsupported platform: ${os}`);
    }

    const exeName = os === 'win32' ? 'memorykit-api.exe' : 'memorykit-api';
    return join(__dirname, '..', 'executables', platformDir, exeName);
  }

  async start(): Promise<void> {
    console.error(`[ProcessManager] Starting .NET API on port ${this.port}...`);

    if (this.useDotnetRun) {
      // Use dotnet run for development
      const projectDir = join(__dirname, '..', '..', 'src', 'MemoryKit.API');
      console.error(`[ProcessManager] Using 'dotnet run' in ${projectDir}`);
      
      this.process = spawn('dotnet', ['run', '--urls', `http://localhost:${this.port}`], {
        cwd: projectDir,
        stdio: ['ignore', 'pipe', 'pipe'],
        env: {
          ...process.env,
          ASPNETCORE_ENVIRONMENT: 'Production',
          MEMORYKIT__STORAGEPROVIDER: 'InMemory'
        }
      });
    } else {
      // Use pre-built executable
      this.process = spawn(this.executablePath, [
        '--urls', `http://localhost:${this.port}`,
        '--environment', 'Production'
      ], {
        stdio: ['ignore', 'pipe', 'pipe'],
        env: {
          ...process.env,
          ASPNETCORE_ENVIRONMENT: 'Production',
          MEMORYKIT__STORAGEPROVIDER: 'InMemory'
        }
      });
    }

    // Log output for debugging
    this.process.stdout?.on('data', (data) => {
      console.error(`[API] ${data.toString().trim()}`);
    });

    this.process.stderr?.on('data', (data) => {
      console.error(`[API Error] ${data.toString().trim()}`);
    });

    this.process.on('exit', (code) => {
      console.error(`[ProcessManager] API process exited with code ${code}`);
      this.process = null;
    });

    // Wait for API to be ready
    await this.waitForHealthy();
    console.error('[ProcessManager] API is ready!');
  }

  private async waitForHealthy(
    retries = 60, // Increased for dotnet run which is slower
    intervalMs = 1000
  ): Promise<void> {
    for (let i = 0; i < retries; i++) {
      try {
        const response = await axios.get(`http://localhost:${this.port}/health`, {
          timeout: 2000
        });
        if (response.status === 200) {
          return;
        }
      } catch (error) {
        // Expected during startup
      }
      await new Promise(resolve => setTimeout(resolve, intervalMs));
    }
    throw new Error('API health check failed after maximum retries');
  }

  async stop(): Promise<void> {
    if (this.process) {
      console.error('[ProcessManager] Stopping API process...');
      this.process.kill('SIGTERM');
      
      // Wait for graceful shutdown
      await new Promise<void>((resolve) => {
        const timeout = setTimeout(() => {
          if (this.process) {
            this.process.kill('SIGKILL');
          }
          resolve();
        }, 5000);

        this.process?.on('exit', () => {
          clearTimeout(timeout);
          resolve();
        });
      });
      
      this.process = null;
    }
  }

  getBaseUrl(): string {
    return `http://localhost:${this.port}`;
  }

  getApiKey(): string {
    return this.apiKey;
  }
}
