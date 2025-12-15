import { Server } from '@modelcontextprotocol/sdk/server/index.js';
import { CallToolRequestSchema, ListToolsRequestSchema } from '@modelcontextprotocol/sdk/types.js';
import { MemoryKitApiClient } from '../api-client.js';

export function registerTools(server: Server, apiClient: MemoryKitApiClient) {
  // List available tools
  server.setRequestHandler(ListToolsRequestSchema, async () => ({
    tools: [
      {
        name: 'store_memory',
        description: 'Store a new message in conversation memory',
        inputSchema: {
          type: 'object',
          properties: {
            conversation_id: { type: 'string', description: 'Conversation ID' },
            role: { type: 'string', enum: ['user', 'assistant'] },
            content: { type: 'string', description: 'Message content' },
            tags: { type: 'array', items: { type: 'string' }, description: 'Optional tags' }
          },
          required: ['conversation_id', 'role', 'content']
        }
      },
      {
        name: 'retrieve_memory',
        description: 'Retrieve messages from conversation memory',
        inputSchema: {
          type: 'object',
          properties: {
            conversation_id: { type: 'string' },
            limit: { type: 'number', description: 'Max messages to return' },
            layer: { type: 'string', enum: ['working', 'semantic', 'episodic'] }
          },
          required: ['conversation_id']
        }
      },
      {
        name: 'search_memory',
        description: 'Search conversation memory by semantic similarity',
        inputSchema: {
          type: 'object',
          properties: {
            conversation_id: { type: 'string' },
            query: { type: 'string', description: 'Search query' }
          },
          required: ['conversation_id', 'query']
        }
      },
      {
        name: 'forget_memory',
        description: 'Delete a specific message from memory',
        inputSchema: {
          type: 'object',
          properties: {
            conversation_id: { type: 'string' },
            message_id: { type: 'string' }
          },
          required: ['conversation_id', 'message_id']
        }
      },
      {
        name: 'consolidate',
        description: 'Trigger memory consolidation between layers',
        inputSchema: {
          type: 'object',
          properties: {
            conversation_id: { type: 'string' },
            force: { type: 'boolean', description: 'Force consolidation even if threshold not met' }
          },
          required: ['conversation_id']
        }
      },
      {
        name: 'get_context',
        description: 'Get formatted memory context for conversation',
        inputSchema: {
          type: 'object',
          properties: {
            conversation_id: { type: 'string' }
          },
          required: ['conversation_id']
        }
      }
    ]
  }));

  // Handle tool calls
  server.setRequestHandler(CallToolRequestSchema, async (request) => {
    const { name, arguments: args } = request.params;

    if (!args) {
      return {
        content: [{
          type: 'text',
          text: 'Error: No arguments provided'
        }],
        isError: true
      };
    }

    try {
      switch (name) {
        case 'store_memory':
          await apiClient.storeMessage(args.conversation_id as string, {
            role: args.role as 'user' | 'assistant',
            content: args.content as string,
            tags: args.tags as string[] | undefined
          });
          return {
            content: [{ type: 'text', text: 'Message stored successfully' }]
          };

        case 'retrieve_memory':
          const messages = await apiClient.retrieveMessages(
            args.conversation_id as string,
            args.limit as number | undefined,
            args.layer as string | undefined
          );
          return {
            content: [{
              type: 'text',
              text: JSON.stringify(messages, null, 2)
            }]
          };

        case 'search_memory':
          const results = await apiClient.searchMemory(
            args.conversation_id as string,
            args.query as string
          );
          return {
            content: [{
              type: 'text',
              text: JSON.stringify(results, null, 2)
            }]
          };

        case 'forget_memory':
          await apiClient.forgetMessage(args.conversation_id as string, args.message_id as string);
          return {
            content: [{ type: 'text', text: 'Message deleted successfully' }]
          };

        case 'consolidate':
          const stats = await apiClient.consolidate(
            args.conversation_id as string,
            (args.force as boolean) || false
          );
          return {
            content: [{
              type: 'text',
              text: `Consolidation complete: ${JSON.stringify(stats, null, 2)}`
            }]
          };

        case 'get_context':
          const context = await apiClient.getContext(args.conversation_id as string);
          return {
            content: [{ type: 'text', text: context }]
          };

        default:
          throw new Error(`Unknown tool: ${name}`);
      }
    } catch (error: any) {
      return {
        content: [{
          type: 'text',
          text: `Error: ${error.message}`
        }],
        isError: true
      };
    }
  });
}
