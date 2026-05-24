using MediatR;
using Payroll.Application.DTOs;
using Payroll.Domain.Interfaces;
using OrgProfileEntity = Payroll.Domain.Entities.OrgProfile;

namespace Payroll.Application.Queries.OrgProfile;

public record GetTaxDetailsQuery : IRequest<TaxDetailsDto>;

public sealed class GetTaxDetailsHandler(IOrgProfileRepository repo)
    : IRequestHandler<GetTaxDetailsQuery, TaxDetailsDto>
{
    public async Task<TaxDetailsDto> Handle(GetTaxDetailsQuery _, CancellationToken ct)
    {
        OrgProfileEntity? profile = await repo.GetAsync(ct);
        return new TaxDetailsDto(
            profile?.Pan,
            profile?.Tan,
            profile?.AoAreaCode,
            profile?.AoType,
            profile?.AoRangeCode,
            profile?.AoNumber,
            profile?.DeductorType,
            profile?.DeductorName,
            profile?.DeductorFathersName,
            profile?.DeductorDesignation,
            profile?.DeductorEmployeeId);
    }
}
