using MediatR;
using Payroll.Application.DTOs;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.WorkLocations;

public record GetWorkLocationQuery(Guid Id) : IRequest<WorkLocationDto>;

public sealed class GetWorkLocationHandler(IWorkLocationRepository repo)
    : IRequestHandler<GetWorkLocationQuery, WorkLocationDto>
{
    public async Task<WorkLocationDto> Handle(
        GetWorkLocationQuery request,
        CancellationToken cancellationToken)
    {
        WorkLocation? location = await repo.GetByIdAsync(request.Id, cancellationToken);
        if (location is null)
            throw new NotFoundException($"WorkLocation '{request.Id}' not found.");

        int employeeCount = await repo.GetEmployeeCountAsync(location.Id, cancellationToken);

        return new WorkLocationDto(
            location.Id,
            location.Name,
            location.AddressLine1,
            location.AddressLine2,
            location.State.ToString(),
            location.City,
            location.PinCode,
            location.PtRegistrationNumber,
            location.IsActive,
            employeeCount);
    }
}
