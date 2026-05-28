using FluentValidation;
using Payroll.Domain.Enums;

namespace Payroll.Application.Commands.OrgProfile;

internal sealed class UpdateOrgProfileCommandValidator : AbstractValidator<UpdateOrgProfileCommand>
{
    public UpdateOrgProfileCommandValidator()
    {
        RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LegalName).MaximumLength(200).When(x => x.LegalName is not null);
        // PAN is mandatory: Form 24Q quarterly returns and Form 16 issuance both require the
        // deductor's PAN. A nullable PAN at the org level would let an operator run payroll
        // they cannot legally file. Tighten at command level so the gap surfaces on save.
        RuleFor(x => x.Pan)
            .NotEmpty()
            .WithMessage("Company PAN is required for tax filings (Form 24Q, Form 16).")
            .Matches(@"^[A-Z]{5}[0-9]{4}[A-Z]$")
            .When(x => !string.IsNullOrEmpty(x.Pan))
            .WithMessage("PAN must be in format AAAAA9999A.");
        RuleFor(x => x.Gstin)
            .Matches(@"^\d{2}[A-Z]{5}\d{4}[A-Z]{1}\d[Z]{1}[A-Z\d]{1}$")
            .When(x => !string.IsNullOrEmpty(x.Gstin))
            .WithMessage("GSTIN format is invalid.");
        RuleFor(x => x.Website).MaximumLength(500).When(x => x.Website is not null);
        RuleFor(x => x.Industry).MaximumLength(150).When(x => x.Industry is not null);
        RuleFor(x => x.State)
            .Must(s => s is null || Enum.TryParse<IndianState>(s, out _))
            .WithMessage("State must be a valid Indian state or union territory.");
        RuleFor(x => x.PinCode)
            .Matches(@"^\d{6}$")
            .When(x => !string.IsNullOrEmpty(x.PinCode))
            .WithMessage("PinCode must be exactly 6 digits.");
    }
}
