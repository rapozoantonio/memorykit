using FluentValidation;
using MemoryKit.Application.DTOs;

namespace MemoryKit.Application.Validators;

/// <summary>
/// Validator for CreateMessageRequest.
/// </summary>
public class CreateMessageRequestValidator : AbstractValidator<CreateMessageRequest>
{
    public CreateMessageRequestValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Message content is required")
            .MaximumLength(10000).WithMessage("Message content cannot exceed 10,000 characters");

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Invalid message role");
    }
}

/// <summary>
/// Validator for CreateConversationRequest.
/// </summary>
public class CreateConversationRequestValidator : AbstractValidator<CreateConversationRequest>
{
    public CreateConversationRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Conversation title is required")
            .MaximumLength(500).WithMessage("Title cannot exceed 500 characters");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description cannot exceed 2,000 characters");

        RuleFor(x => x.Tags)
            .Must(tags => tags == null || tags.Length <= 10)
            .WithMessage("Cannot have more than 10 tags");
    }
}

/// <summary>
/// Validator for QueryMemoryRequest.
/// </summary>
public class QueryMemoryRequestValidator : AbstractValidator<QueryMemoryRequest>
{
    public QueryMemoryRequestValidator()
    {
        RuleFor(x => x.Question)
            .NotEmpty().WithMessage("Question is required")
            .MaximumLength(5000).WithMessage("Question cannot exceed 5,000 characters");

        RuleFor(x => x.MaxTokens)
            .GreaterThan(0).WithMessage("MaxTokens must be greater than 0")
            .LessThanOrEqualTo(10000).WithMessage("MaxTokens cannot exceed 10,000");
    }
}
