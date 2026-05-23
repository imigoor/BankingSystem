using FluentValidation;

namespace Clients.Application.Features.UpdateClient;

public sealed class UpdateClientCommandValidator : AbstractValidator<UpdateClientCommand>
{
    public UpdateClientCommandValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty();

        When(x => x.Email is not null, () =>
        {
            RuleFor(x => x.Email!)
                .EmailAddress().WithMessage("A valid email address is required.");
        });

        When(x => x.Name is not null, () =>
        {
            RuleFor(x => x.Name!)
                .MinimumLength(2).WithMessage("Name must have at least 2 characters.")
                .MaximumLength(200).WithMessage("Name cannot exceed 200 characters.");
        });

        When(x => x.BankingDetails is not null, () =>
        {
            RuleFor(x => x.BankingDetails!.Agency)
                .NotEmpty().WithMessage("Agency is required.")
                .MaximumLength(10);

            RuleFor(x => x.BankingDetails!.AccountNumber)
                .NotEmpty().WithMessage("Account number is required.")
                .MaximumLength(20);
        });
    }
}
