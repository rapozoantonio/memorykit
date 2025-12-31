using MediatR;
using Microsoft.AspNetCore.Mvc;
using MemoryKit.Application.DTOs;
using MemoryKit.Application.UseCases.AddMessage;
using MemoryKit.Application.UseCases.ConsolidateMemory;
using MemoryKit.Application.UseCases.CreateConversation;
using MemoryKit.Application.UseCases.ForgetMemory;
using MemoryKit.Application.UseCases.GetContext;
using MemoryKit.Application.UseCases.GetMessages;
using MemoryKit.Application.UseCases.QueryMemory;

namespace MemoryKit.API.Controllers;

/// <summary>
/// API controller for conversation management.
/// </summary>
[ApiController]
[Route("api/v1/conversations")]
public class ConversationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ConversationsController> _logger;

    public ConversationsController(IMediator mediator, ILogger<ConversationsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new conversation.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ConversationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateConversation(
        [FromBody] CreateConversationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value ?? throw new InvalidOperationException("User ID not found");

            var command = new CreateConversationCommand(userId, request);
            var result = await _mediator.Send(command, cancellationToken);

            return CreatedAtAction(nameof(CreateConversation), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating conversation");
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Adds a message to a conversation.
    /// </summary>
    [HttpPost("{conversationId}/messages")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddMessage(
        [FromRoute] string conversationId,
        [FromBody] CreateMessageRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value ?? throw new InvalidOperationException("User ID not found");

            var command = new AddMessageCommand(userId, conversationId, request);
            var result = await _mediator.Send(command, cancellationToken);

            return CreatedAtAction(nameof(AddMessage), new { conversationId }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding message");
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Retrieves messages from a conversation.
    /// </summary>
    [HttpGet("{conversationId}/messages")]
    [ProducesResponseType(typeof(GetMessagesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMessages(
        [FromRoute] string conversationId,
        [FromQuery] int? limit = null,
        [FromQuery] DateTime? before = null,
        [FromQuery] DateTime? after = null,
        [FromQuery] string? layer = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value ?? throw new InvalidOperationException("User ID not found");

            var query = new GetMessagesQuery(userId, conversationId, limit, before, after, layer);
            var result = await _mediator.Send(query, cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving messages");
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Queries memory and gets a response.
    /// </summary>
    [HttpPost("{conversationId}/query")]
    [ProducesResponseType(typeof(QueryMemoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> QueryMemory(
        [FromRoute] string conversationId,
        [FromBody] QueryMemoryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value ?? throw new InvalidOperationException("User ID not found");

            var query = new QueryMemoryQuery(userId, conversationId, request);
            var result = await _mediator.Send(query, cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying memory");
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Gets memory context without generating a response.
    /// </summary>
    [HttpGet("{conversationId}/context")]
    [ProducesResponseType(typeof(GetContextResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetContext(
        [FromRoute] string conversationId,
        [FromQuery] string? query = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value ?? throw new InvalidOperationException("User ID not found");

            var getContextQuery = new GetContextQuery(userId, conversationId, query ?? string.Empty);
            var result = await _mediator.Send(getContextQuery, cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting context");
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Deletes a specific message from all memory layers.
    /// </summary>
    [HttpDelete("{conversationId}/messages/{messageId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgetMessage(
        [FromRoute] string conversationId,
        [FromRoute] string messageId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value ?? throw new InvalidOperationException("User ID not found");

            var command = new ForgetMemoryCommand(userId, conversationId, messageId);
            var result = await _mediator.Send(command, cancellationToken);

            if (result)
            {
                return NoContent();
            }

            return NotFound(new { Message = $"Message {messageId} not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error forgetting message");
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Triggers memory consolidation for a conversation.
    /// </summary>
    [HttpPost("{conversationId}/consolidate")]
    [ProducesResponseType(typeof(ConsolidateMemoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Consolidate(
        [FromRoute] string conversationId,
        [FromBody] ConsolidateRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value ?? throw new InvalidOperationException("User ID not found");

            var command = new ConsolidateMemoryCommand(
                userId,
                conversationId,
                request?.Force ?? false,
                request?.TargetLayer);

            var result = await _mediator.Send(command, cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consolidating memory");
            return BadRequest(ex.Message);
        }
    }
}

/// <summary>
/// Request for consolidation endpoint.
/// </summary>
public record ConsolidateRequest
{
    /// <summary>
    /// Gets whether to force consolidation even if threshold not met.
    /// </summary>
    public bool Force { get; init; }

    /// <summary>
    /// Gets the target layer for consolidation.
    /// </summary>
    public string? TargetLayer { get; init; }
}
