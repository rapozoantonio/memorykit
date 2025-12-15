import axios, { AxiosInstance } from 'axios';

export class MemoryKitApiClient {
  private client: AxiosInstance;

  constructor(baseUrl: string, apiKey: string) {
    this.client = axios.create({
      baseURL: baseUrl,
      headers: {
        'X-API-Key': apiKey,
        'Content-Type': 'application/json'
      },
      timeout: 30000
    });
  }

  // Conversation management
  async createConversation(userId: string = 'mcp-user'): Promise<string> {
    const response = await this.client.post('/api/v1/conversations', {
      userId,
      metadata: { source: 'mcp' }
    });
    return response.data.conversationId;
  }

  // Store memory
  async storeMessage(conversationId: string, message: {
    role: 'user' | 'assistant';
    content: string;
    tags?: string[];
  }): Promise<void> {
    await this.client.post(
      `/api/v1/conversations/${conversationId}/messages`,
      message
    );
  }

  // Retrieve messages
  async retrieveMessages(
    conversationId: string,
    limit?: number,
    layer?: string
  ): Promise<any[]> {
    const response = await this.client.get(
      `/api/v1/conversations/${conversationId}/messages`,
      { params: { limit, layer } }
    );
    return response.data.messages;
  }

  // Search memory
  async searchMemory(conversationId: string, query: string): Promise<any> {
    const response = await this.client.post(
      `/api/v1/conversations/${conversationId}/memory/query`,
      { query, includeResponse: false }
    );
    return response.data;
  }

  // Get context
  async getContext(conversationId: string): Promise<string> {
    const response = await this.client.get(
      `/api/v1/conversations/${conversationId}/memory/context`
    );
    return response.data.context;
  }

  // Forget memory
  async forgetMessage(conversationId: string, messageId: string): Promise<void> {
    await this.client.delete(
      `/api/v1/conversations/${conversationId}/messages/${messageId}`
    );
  }

  // Consolidate
  async consolidate(conversationId: string, force = false): Promise<any> {
    const response = await this.client.post(
      `/api/v1/conversations/${conversationId}/consolidate`,
      { force }
    );
    return response.data;
  }
}
