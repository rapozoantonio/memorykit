# MemoryKit API Reference

## Base URL

```
https://api.memorykit.example.com/api/v1
```

## Authentication

All API endpoints require authentication using Bearer tokens:

```http
Authorization: Bearer {your_token}
```

## Endpoints

### Conversations

#### Create Conversation

Creates a new conversation context.

```http
POST /conversations
```

**Request Body:**

```json
{
  "userId": "string",
  "metadata": {
    "topic": "string",
    "tags": ["string"]
  }
}
```

**Response:**

```json
{
  "conversationId": "string",
  "userId": "string",
  "createdAt": "2025-11-16T23:14:00Z",
  "metadata": {}
}
```

#### Get Conversation

Retrieves a specific conversation.

```http
GET /conversations/{conversationId}
```

**Response:**

```json
{
  "conversationId": "string",
  "userId": "string",
  "messages": [],
  "createdAt": "2025-11-16T23:14:00Z",
  "updatedAt": "2025-11-16T23:14:00Z"
}
```

#### Add Message

Adds a message to a conversation.

```http
POST /conversations/{conversationId}/messages
```

**Request Body:**

```json
{
  "content": "string",
  "role": "user|assistant|system",
  "metadata": {}
}
```

**Response:**

```json
{
  "messageId": "string",
  "conversationId": "string",
  "content": "string",
  "role": "user",
  "timestamp": "2025-11-16T23:14:00Z",
  "importanceScore": 0.85
}
```

### Memories

#### Query Memory

Queries all memory layers based on context.

```http
POST /memories/query
```

**Request Body:**

```json
{
  "query": "string",
  "conversationId": "string",
  "maxResults": 10,
  "memoryLayers": ["working", "episodic", "semantic", "procedural"]
}
```

**Response:**

```json
{
  "queryId": "string",
  "results": [
    {
      "memoryLayer": "episodic",
      "content": "string",
      "relevanceScore": 0.92,
      "timestamp": "2025-11-16T23:14:00Z",
      "metadata": {}
    }
  ],
  "queryPlan": {
    "type": "hybrid",
    "layers": ["working", "episodic"]
  }
}
```

#### Get Context

Retrieves relevant context for a conversation.

```http
GET /memories/context/{conversationId}
```

**Query Parameters:**

- `maxTokens` (optional): Maximum context size
- `includePatterns` (optional): Include procedural patterns

**Response:**

```json
{
  "conversationId": "string",
  "workingMemory": [],
  "episodicContext": [],
  "semanticFacts": [],
  "proceduralPatterns": [],
  "totalTokens": 1500
}
```

### Patterns

#### Get Patterns

Retrieves learned procedural patterns.

```http
GET /patterns
```

**Query Parameters:**

- `userId` (optional): Filter by user
- `topic` (optional): Filter by topic
- `minConfidence` (optional): Minimum confidence score

**Response:**

```json
{
  "patterns": [
    {
      "patternId": "string",
      "type": "string",
      "trigger": "string",
      "actions": [],
      "confidence": 0.88,
      "usageCount": 42
    }
  ]
}
```

## Error Responses

All endpoints use standard HTTP status codes:

- `200 OK`: Success
- `201 Created`: Resource created
- `400 Bad Request`: Invalid input
- `401 Unauthorized`: Missing or invalid authentication
- `403 Forbidden`: Insufficient permissions
- `404 Not Found`: Resource not found
- `429 Too Many Requests`: Rate limit exceeded
- `500 Internal Server Error`: Server error

**Error Response Format:**

```json
{
  "error": {
    "code": "string",
    "message": "string",
    "details": {}
  }
}
```

## Rate Limiting

API requests are limited to:

- 100 requests per minute per user
- 1000 requests per hour per user

Rate limit headers:

```http
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1700000000
```

## Webhooks

MemoryKit supports webhooks for async notifications:

- `memory.consolidated`: Memory consolidation completed
- `pattern.learned`: New pattern identified
- `importance.threshold`: High-importance event detected

## SDK Examples

### C# / .NET

```csharp
var client = new MemoryKitClient("your_api_key");

// Add message
var message = await client.AddMessageAsync(
    conversationId,
    "Hello, world!",
    MessageRole.User
);

// Query memory
var results = await client.QueryMemoryAsync(
    "What did we discuss about AI?",
    conversationId
);
```

### Python

```python
from memorykit import MemoryKitClient

client = MemoryKitClient(api_key="your_api_key")

# Add message
message = client.add_message(
    conversation_id,
    "Hello, world!",
    role="user"
)

# Query memory
results = client.query_memory(
    "What did we discuss about AI?",
    conversation_id=conversation_id
)
```

## Pagination

List endpoints support pagination:

```http
GET /memories?page=1&pageSize=50
```

**Response:**

```json
{
  "data": [],
  "pagination": {
    "currentPage": 1,
    "pageSize": 50,
    "totalPages": 10,
    "totalItems": 500
  }
}
```

## Versioning

The API uses URL versioning. Current version: `v1`

Future versions will be available at `/api/v2`, etc.
