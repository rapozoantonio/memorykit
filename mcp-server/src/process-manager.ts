import { spawn, ChildProcess } from 'child_process';
import { platform } from 'os';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';
import axios from 'axios';

// ES module compatibility
const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

export interface ProcessManagerConfig {
  useDocker?: boolean;
  dockerComposeFile?: string;
  executablePath?: string;
  port?: number;
  apiKey: string;
  healthCheckRetries?: number;
  healthCheckIntervalMs?: number;
}

export class ProcessManager {
  private process: ChildProcess | null = null;
  private port: number;
  private apiKey: string;
  private executablePath: string;
  private useDocker: boolean;
  private dockerComposeFile: string;
  private containerName = 'memorykit-mcp-api';

  constructor(config: ProcessManagerConfig) {
    this.port = config.port || 5555;
    this.apiKey = config.apiKey;
    this.useDocker = config.useDocker ?? true; // Default to Docker
    this.dockerComposeFile = config.dockerComposeFile || this.getDefaultDockerComposePath();
    this.executablePath = config.executablePath || this.getDefaultExecutable();
  }

  private getDefaultDockerComposePath(): string {
    // Assume docker-compose.yml is in the project root (3 levels up from dist/src)
    return join(__dirname, '..', '..', '..', 'docker-compose.yml');
  }

  private getDefaultExecutable(): string {
    const os = platform();
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
    if (this.useDocker) {
      await this.startDocker();
    } else {
      await this.startExecutable();
    }
  }

  private async startDocker(): Promise<void> {
    console.error(`[ProcessManager] Starting MemoryKit API via Docker on port ${this.port}...`);

    // Check if Docker is available
    const dockerCheck = spawn('docker', ['--version'], { shell: true });
    await new Promise<void>((resolve, reject) => {
      dockerCheck.on('exit', (code) => {
        if (code !== 0) {
          reject(new Error('Docker is not available. Please install Docker Desktop or use --no-docker flag.'));
        } else {
          resolve();
        }
      });
    });

    // Stop any existing container
    await this.stopDocker();

    // Start the API using docker compose with MCP profile
    const composeArgs = [
      '-f', this.dockerComposeFile,
      '--profile', 'mcp',
      'up', '-d', 'mcp-api'
    ];

    console.error(`[ProcessManager] Running: docker-compose ${composeArgs.join(' ')}`);

    this.process = spawn('docker-compose', composeArgs, {
      stdio: ['ignore', 'pipe', 'pipe'],
      shell: true
    });

    let output = '';
    this.process.stdout?.on('data', (data) => {
      output += data.toString();
      console.error(`[Docker] ${data.toString().trim()}`);
    });

    this.process.stderr?.on('data', (data) => {
      output += data.toString();
      console.error(`[Docker] ${data.toString().trim()}`);
    });

    // Wait for docker compose to finish starting the container
    await new Promise<void>((resolve, reject) => {
      this.process?.on('exit', (code) => {
        if (code === 0) {
          resolve();
        } else {
          reject(new Error(`Docker compose failed with code ${code}: ${output}`));
        }
      });
    });

    // Wait for API to be healthy
    await this.waitForHealthy();
    console.error('[ProcessManager] API is ready!');
  }

  private async startExecutable(): Promise<void> {
    console.error(`[ProcessManager] Starting .NET API executable on port ${this.port}...`);

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
    retries = 30,
    intervalMs = 1000
  ): Promise<void> {
    console.error(`[ProcessManager] Waiting for API health check at http://localhost:${this.port}/health...`);
    
    for (let i = 0; i < retries; i++) {
      try {
        const response = await axios.get(`http://localhost:${this.port}/health`, {
          timeout: 2000
        });
        if (response.status === 200) {
          console.error('[ProcessManager] Health check passed!');
          return;
        }
      } catch (error) {
        // Expected during startup
        if (i % 5 === 0) {
          console.error(`[ProcessManager] Health check attempt ${i + 1}/${retries}...`);
        }
      }
      await new Promise(resolve => setTimeout(resolve, intervalMs));
    }
    throw new Error('API health check failed after maximum retries');
  }

  async stop(): Promise<void> {
    if (this.useDocker) {
      await this.stopDocker();
    } else {
      await this.stopExecutable();
    }
  }

  private async stopDocker(): Promise<void> {
    console.error('[ProcessManager] Stopping Docker container...');
    
    const stopProcess = spawn('docker-compose', ['-f', this.dockerComposeFile, '--profile', 'mcp', 'down'], {
      stdio: 'inherit',
      shell: true
    });

    await new Promise<void>((resolve) => {
      stopProcess.on('exit', () => {
        console.error('[ProcessManager] Docker container stopped');
        resolve();
      });
    });
  }

  private async stopExecutable(): Promise<void> {
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

