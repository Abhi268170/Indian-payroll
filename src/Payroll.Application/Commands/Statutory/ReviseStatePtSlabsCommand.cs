using FluentValidation;
using MediatR;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.Statutory;

public sealed record PtSlabInput(
    decimal MinGross,
    decimal? MaxGross,
    decimal PtAmount,
    string? Gender,
    bool IsFebruarySurcharge);

public sealed record ReviseStatePtSlabsCommand(
    string StateCode,
    DateOnly EffectiveDate,
    string Frequency,
    IReadOnlyList<PtSlabInput> Slabs,
    Guid ActorId) : IRequest;

public sealed class ReviseStatePtSlabsCommandValidator : AbstractValidator<ReviseStatePtSlabsCommand>
{
    public ReviseStatePtSlabsCommandValidator()
    {
        RuleFor(x => x.StateCode).NotEmpty().Length(2);
        RuleFor(x => x.EffectiveDate).NotEmpty();
        RuleFor(x => x.Frequency).NotEmpty().Must(f => f is "Monthly" or "HalfYearly" or "Annual")
            .WithMessage("Frequency must be Monthly, HalfYearly, or Annual.");
        RuleFor(x => x.Slabs).NotEmpty().WithMessage("At least one slab is required.");
        RuleForEach(x => x.Slabs).ChildRules(slab =>
        {
            slab.RuleFor(s => s.MinGross).GreaterThanOrEqualTo(0);
            slab.RuleFor(s => s.PtAmount).GreaterThanOrEqualTo(0);
            slab.RuleFor(s => s.MaxGross)
                .GreaterThan(s => s.MinGross)
                .When(s => s.MaxGross.HasValue)
                .WithMessage("MaxGross must be greater than MinGross.");
        });
    }
}

internal sealed class ReviseStatePtSlabsHandler(IStatutoryConfigRepository repo, IUnitOfWork uow)
    : IRequestHandler<ReviseStatePtSlabsCommand>
{
    public async Task Handle(ReviseStatePtSlabsCommand cmd, CancellationToken ct)
    {
        IEnumerable<ProfessionalTaxSlab> slabs = cmd.Slabs.Select(s =>
            ProfessionalTaxSlab.Create(
                cmd.StateCode,
                cmd.EffectiveDate,
                cmd.Frequency,
                s.Gender,
                s.MinGross,
                s.MaxGross,
                s.PtAmount,
                s.IsFebruarySurcharge,
                cmd.ActorId));

        await repo.AddPtSlabsAsync(slabs, ct);
        await uow.SaveChangesAsync(ct);
    }
}
