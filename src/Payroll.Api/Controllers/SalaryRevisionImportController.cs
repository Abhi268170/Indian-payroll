using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Application.Commands.SalaryRevisions;
using Payroll.Application.Interfaces;
using Payroll.Domain.Common;

namespace Payroll.Api.Controllers;

[ApiController]
[Route("api/v1/salary-revisions/import")]
[Authorize(Policy = "PayrollManager")]
public sealed class SalaryRevisionImportController(
    ISender sender,
    ISalaryRevisionImportTemplateGenerator templateGenerator) : ControllerBase
{
    [HttpGet("template")]
    public IActionResult DownloadTemplate()
    {
        byte[] bytes = templateGenerator.Generate();
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "salary-revision-import-template.xlsx");
    }

    [HttpPost("validate")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Validate(
        IFormFile file, [FromForm] bool overwriteExisting, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No file provided." });
        try
        {
            using Stream stream = file.OpenReadStream();
            SalaryRevisionImportValidationResult result =
                await sender.Send(new ValidateSalaryRevisionImportCommand(stream, overwriteExisting), ct);
            return Ok(result);
        }
        catch (ImportFormatException ex) { return BadRequest(new { error = ex.Message }); }
        catch (DomainException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("commit")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Commit(
        IFormFile file, [FromForm] bool overwriteExisting, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No file provided." });
        try
        {
            using Stream stream = file.OpenReadStream();
            SalaryRevisionImportCommitResult result =
                await sender.Send(new CommitSalaryRevisionImportCommand(stream, overwriteExisting, GetActorId()), ct);
            return Ok(result);
        }
        catch (ImportFormatException ex) { return BadRequest(new { error = ex.Message }); }
        catch (DomainException ex) { return BadRequest(new { error = ex.Message }); }
    }

    private Guid GetActorId()
    {
        string? sub = User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out Guid id) ? id : Guid.Empty;
    }
}
