using MediatR;
using Microsoft.AspNetCore.Mvc;
using MemoryKit.Application.DTOs;
using MemoryKit.Application.UseCases.AddMessage;
using MemoryKit.Application.UseCases.CreateConversation;
using MemoryKit.Application.UseCases.GetContext;
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
}
