using MediatR;
using Payroll.Application.Interfaces;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.Employees;

public record GetEmployeeDocumentContentQuery(Guid EmployeeId, Guid DocumentId)
    : IRequest<EmployeeDocumentContentDto>;

public sealed record EmployeeDocumentContentDto(byte[] Content, string FileName, string ContentType);

internal sealed class GetEmployeeDocumentContentHandler(
    IEmployeeDocumentRepository repo,
    IFileStorageService fileStorage)
    : IRequestHandler<GetEmployeeDocumentContentQuery, EmployeeDocumentContentDto>
{
    public async Task<EmployeeDocumentContentDto> Handle(GetEmployeeDocumentContentQuery req, CancellationToken ct)
    {
        EmployeeDocument doc = await repo.GetByIdAsync(req.DocumentId, ct)
            ?? throw new NotFoundException($"Document {req.DocumentId} not found.");
        if (doc.EmployeeId != req.EmployeeId)
            throw new NotFoundException("Document does not belong to this employee.");

        using Stream s = await fileStorage.GetAsync(doc.StorageKey, ct);
        using MemoryStream ms = new();
        await s.CopyToAsync(ms, ct);
        return new EmployeeDocumentContentDto(ms.ToArray(), doc.FileName, "application/pdf");
    }
}
