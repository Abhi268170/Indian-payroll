using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Application.Queries.Payslips;
using Payroll.Domain.Common;

namespace Payroll.Api.Controllers;

[ApiController]
[Route("api/v1/payslips")]
[Authorize]
public sealed class PayslipsController(ISender sender) : ControllerBase
{
    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        try
        {
            (Stream stream, string fileName) = await sender.Send(new DownloadPayslipQuery(id), ct);
            return File(stream, "application/pdf", fileName);
        }
        catch (NotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return UnprocessableEntity(new { error = ex.Message }); }
    }
}
