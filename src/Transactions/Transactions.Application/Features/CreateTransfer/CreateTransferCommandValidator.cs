using FluentValidation;

namespace Transactions.Application.Features.CreateTransfer;

public sealed class CreateTransferCommandValidator : AbstractValidator<CreateTransferCommand>
{
    public CreateTransferCommandValidator()
    {
        RuleFor(x => x.SenderUserId)
            .NotEmpty().WithMessage("SenderUserId is required.");

        RuleFor(x => x.ReceiverUserId)
            .NotEmpty().WithMessage("ReceiverUserId is required.")
            .NotEqual(x => x.SenderUserId).WithMessage("Sender and receiver must be different users.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.")
            .LessThanOrEqualTo(1_000_000).WithMessage("Amount cannot exceed 1,000,000.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");
    }
}
