using MediatR;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.Employees;

public record GetEmployeeDocumentsQuery(Guid EmployeeId) : IRequest<IReadOnlyList<EmployeeDocumentDto>>;

public sealed record EmployeeDocumentDto(
    Guid Id,
    string DocumentType,
    string FileName,
    DateTimeOffset CreatedAt);

internal sealed class GetEmployeeDocumentsHandler(IEmployeeDocumentRepository repo)
    : IRequestHandler<GetEmployeeDocumentsQuery, IReadOnlyList<EmployeeDocumentDto>>
{
    public async Task<IReadOnlyList<EmployeeDocumentDto>> Handle(GetEmployeeDocumentsQuery req, CancellationToken ct)
    {
        IReadOnlyList<EmployeeDocument> docs = await repo.GetByEmployeeAsync(req.EmployeeId, ct);
        return docs.Select(d => new EmployeeDocumentDto(d.Id, d.DocumentType, d.FileName, d.CreatedAt)).ToList();
    }
}
