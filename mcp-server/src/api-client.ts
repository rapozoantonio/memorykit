import axios, { AxiosInstance } from "axios";

export class MemoryKitApiClient {
  private client: AxiosInstance;

  constructor(baseUrl: string, apiKey: string) {
    this.client = axios.create({
      baseURL: baseUrl,
      headers: {
        "X-API-Key": apiKey,
        "Content-Type": "application/json",
      },
      timeout: 30000,
    });
  }

  // Conversation management
  async createConversation(
    title: string = "MCP Conversation"
  ): Promise<string> {
    const response = await this.client.post("/api/v1/conversations", {
      Title: title,
    });
    return response.data.Id;
  }

  // Store memory
  async storeMessage(
    conversationId: string,
    message: {
      role: "user" | "assistant";
      content: string;
      tags?: string[];
    }
  ): Promise<string> {
    // Convert role string to enum number: user=0, assistant=1
    const roleNum = message.role === "user" ? 0 : 1;
    const response = await this.client.post(
      `/api/v1/conversations/${conversationId}/messages`,
      {
        Role: roleNum,
        Content: message.content,
        Tags: message.tags,
      }
    );
    return response.data.Id;
  }

  // Retrieve messages
  async retrieveMessages(
    conversationId: string,
    limit?: number,
    layer?: string
  ): Promise<any> {
    const response = await this.client.get(
      `/api/v1/conversations/${conversationId}/messages`,
      { params: { limit, layer } }
    );
    return response.data; // Returns {Messages[], Total, HasMore}
  }

  // Search/Query memory
  async searchMemory(conversationId: string, query: string): Promise<any> {
    const response = await this.client.post(
      `/api/v1/conversations/${conversationId}/query`,
      { Question: query }
    );
    return response.data; // Returns {Answer, Sources}
  }

  // Get context
  async getContext(conversationId: string): Promise<any> {
    const response = await this.client.get(
      `/api/v1/conversations/${conversationId}/context`
    );
    return response.data; // Returns {Context, TotalTokens, RetrievalLatencyMs}
  }

  // Forget memory
  async forgetMessage(
    conversationId: string,
    messageId: string
  ): Promise<void> {
    await this.client.delete(
      `/api/v1/conversations/${conversationId}/messages/${messageId}`
    );
  }

  // Consolidate
  async consolidate(conversationId: string, force = false): Promise<any> {
    const response = await this.client.post(
      `/api/v1/conversations/${conversationId}/consolidate`,
      { Force: force }
    );
    return response.data; // Returns consolidation stats
  }
}
