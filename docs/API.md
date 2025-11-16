# API Documentation

## Base URL

```
https://api.memorykit.dev/v1
```

## Authentication

All endpoints require authentication via Bearer token (JWT) or API Key.

```
Authorization: Bearer <token>
```

## Endpoints

### Conversations

#### Create Conversation
```
POST /conversations

Request:
{
  "title": "Technical Discussion",
  "description": "Discussing .NET architecture",
  "tags": ["architecture", "dotnet"]
}

Response (201 Created):
{
  "id": "conv_abc123",
  "userId": "user_123",
  "title": "Technical Discussion",
  "messageCount": 0,
  "lastActivityAt": "2025-11-16T10:30:00Z"
}
```

#### Add Message
```
POST /conversations/{conversationId}/messages

Request:
{
  "role": "user",
  "content": "How do I implement caching in .NET?",
  "tags": ["caching", "performance"]
}

Response (201 Created):
{
  "id": "msg_xyz789",
  "conversationId": "conv_abc123",
  "role": "user",
  "content": "How do I implement caching in .NET?",
  "timestamp": "2025-11-16T10:31:00Z",
  "importanceScore": 0.75
}
```

#### Query Memory
```
POST /conversations/{conversationId}/query

Request:
{
  "question": "What caching strategies did we discuss?",
  "maxTokens": 2000,
  "includeDebugInfo": true
}

Response (200 OK):
{
  "answer": "We discussed three caching strategies...",
  "sources": [
    {
      "type": "SemanticMemory",
      "content": "Technology: .NET, Caching",
      "relevanceScore": 0.92
    }
  ],
  "debugInfo": {
    "queryType": "FactRetrieval",
    "layersUsed": ["WorkingMemory", "SemanticMemory"],
    "tokensUsed": 450,
    "retrievalTimeMs": 85
  }
}
```

#### Get Context
```
GET /conversations/{conversationId}/context?query=caching

Response (200 OK):
{
  "context": "=== Recent Conversation ===\nuser: How do I implement caching in .NET?\n...",
  "totalTokens": 1250,
  "retrievalLatencyMs": 120
}
```

### Memory

#### Health Check
```
GET /memory/health

Response (200 OK):
{
  "status": "healthy",
  "timestamp": "2025-11-16T10:35:00Z"
}
```

#### Get Statistics
```
GET /memory/statistics

Response (200 OK):
{
  "userId": "user_123",
  "conversationCount": 15,
  "messageCount": 342,
  "factCount": 128,
  "patternCount": 8
}
```

### Patterns

#### List Patterns
```
GET /patterns

Response (200 OK):
[
  {
    "id": "pattern_123",
    "name": "Code Formatting Preference",
    "description": "User prefers Python code with type hints",
    "triggers": [
      {
        "type": "Keyword",
        "pattern": "write code"
      }
    ],
    "usageCount": 25,
    "lastUsed": "2025-11-15T14:20:00Z"
  }
]
```

#### Delete Pattern
```
DELETE /patterns/{patternId}

Response (204 No Content)
```

## Error Handling

### Error Response Format
```json
{
  "error": "error_code",
  "message": "Human-readable error message",
  "details": {
    "field": ["validation error"]
  }
}
```

### Common Error Codes
- `BAD_REQUEST` (400): Invalid input
- `UNAUTHORIZED` (401): Missing or invalid authentication
- `FORBIDDEN` (403): Insufficient permissions
- `NOT_FOUND` (404): Resource not found
- `RATE_LIMITED` (429): Too many requests
- `INTERNAL_ERROR` (500): Server error

## Rate Limiting

- Standard tier: 100 requests/minute
- Premium tier: 1000 requests/minute

Rate limit headers:
```
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 87
X-RateLimit-Reset: 1637062860
```

## Pagination

List endpoints support pagination:

```
GET /patterns?page=1&pageSize=10

Response:
{
  "items": [...],
  "page": 1,
  "pageSize": 10,
  "totalCount": 42
}
```

## SDK Usage

### .NET
```csharp
using MemoryKit.Client;

var client = new MemoryKitClient("your-api-key");

var conversation = await client.Conversations.CreateAsync("Technical Discussion");
await client.Conversations.AddMessageAsync(
    conversation.Id,
    MessageRole.User,
    "What caching approach should I use?");

var response = await client.Conversations.QueryAsync(
    conversation.Id,
    "What was recommended?");

Console.WriteLine(response.Answer);
```

## Webhooks

Subscribe to events:

```
POST /webhooks

Request:
{
  "url": "https://example.com/callback",
  "events": ["message.created", "pattern.detected"]
}
```

Events will be POSTed with signature verification.

## Rate Limits & Quotas

| Tier | Requests/Min | Conversations | Messages | Storage |
|------|-------------|---------------|----------|---------|
| Free | 10 | 5 | 1,000 | 10MB |
| Pro | 100 | Unlimited | Unlimited | 1GB |
| Enterprise | Custom | Custom | Custom | Custom |

## Changelog

See `CHANGELOG.md` for API version history and breaking changes.

