using MediatR;
using Payroll.Application.DTOs;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.WorkLocations;

public record ListWorkLocationsQuery : IRequest<IReadOnlyList<WorkLocationDto>>;

public sealed class ListWorkLocationsHandler(IWorkLocationRepository repo)
    : IRequestHandler<ListWorkLocationsQuery, IReadOnlyList<WorkLocationDto>>
{
    public async Task<IReadOnlyList<WorkLocationDto>> Handle(
        ListWorkLocationsQuery request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<WorkLocation> locations = await repo.ListAsync(cancellationToken);

        List<WorkLocationDto> result = new(locations.Count);
        foreach (WorkLocation location in locations)
        {
            int employeeCount = await repo.GetEmployeeCountAsync(location.Id, cancellationToken);
            result.Add(new WorkLocationDto(
                location.Id,
                location.Name,
                location.AddressLine1,
                location.AddressLine2,
                location.State.ToString(),
                location.City,
                location.PinCode,
                location.PtRegistrationNumber,
                location.IsActive,
                employeeCount));
        }

        return result;
    }
}
