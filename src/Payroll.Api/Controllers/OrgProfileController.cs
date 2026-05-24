using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Application.Commands.OrgProfile;
using Payroll.Application.DTOs;
using Payroll.Application.Queries.OrgProfile;
using Payroll.Domain.Common;

namespace Payroll.Api.Controllers;

[ApiController]
[Route("api/v1/org-profile")]
[Authorize]
public sealed class OrgProfileController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        OrgProfileDto profile = await sender.Send(new GetOrgProfileQuery(), cancellationToken);
        return Ok(profile);
    }

    [HttpPut]
    public async Task<IActionResult> Update(
        [FromBody] UpdateOrgProfileRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await sender.Send(new UpdateOrgProfileCommand(
                request.CompanyName,
                request.LegalName,
                request.Pan,
                request.Gstin,
                request.Website,
                request.Industry,
                request.IncorporationDate,
                request.AddressLine1,
                request.AddressLine2,
                request.City,
                request.State,
                request.PinCode,
                request.FilingAddressWorkLocationId,
                GetActorId()),
                cancellationToken);
            return NoContent();
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("logo")]
    [RequestSizeLimit(3 * 1024 * 1024)] // 3 MB hard cap
    public async Task<IActionResult> UploadLogo(IFormFile file, CancellationToken cancellationToken)
    {
        try
        {
            await sender.Send(new UploadOrgLogoCommand(
                file.OpenReadStream(),
                file.ContentType,
                file.Length,
                GetActorId()),
                cancellationToken);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("logo")]
    [AllowAnonymous] // logo is served inline; auth handled at org-profile level
    public async Task<IActionResult> GetLogo(CancellationToken cancellationToken)
    {
        try
        {
            Stream stream = await sender.Send(new GetOrgLogoStreamQuery(), cancellationToken);
            return File(stream, "image/jpeg"); // browser sniffs actual type
        }
        catch (NotFoundException)
        {
            return NoContent();
        }
    }

    [HttpDelete("logo")]
    public async Task<IActionResult> DeleteLogo(CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteOrgLogoCommand(GetActorId()), cancellationToken);
        return NoContent();
    }

    [HttpGet("tax-details")]
    public async Task<IActionResult> GetTaxDetails(CancellationToken cancellationToken)
    {
        TaxDetailsDto dto = await sender.Send(new GetTaxDetailsQuery(), cancellationToken);
        return Ok(dto);
    }

    [HttpPut("tax-details")]
    [Authorize(Policy = "OrgAdmin")]
    public async Task<IActionResult> UpdateTaxDetails(
        [FromBody] UpdateTaxDetailsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await sender.Send(new UpdateTaxDetailsCommand(
                request.Tan,
                request.AoAreaCode,
                request.AoType,
                request.AoRangeCode,
                request.AoNumber,
                request.DeductorType,
                request.DeductorName,
                request.DeductorFathersName,
                request.DeductorDesignation,
                GetActorId()),
                cancellationToken);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    private Guid GetActorId()
    {
        string? sub = User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out Guid id) ? id : Guid.Empty;
    }
}

public record UpdateTaxDetailsRequest(
    string? Tan,
    string? AoAreaCode,
    string? AoType,
    string? AoRangeCode,
    string? AoNumber,
    string? DeductorType,
    string? DeductorName,
    string? DeductorFathersName,
    string? DeductorDesignation);

public record UpdateOrgProfileRequest(
    string CompanyName,
    string? LegalName,
    string? Pan,
    string? Gstin,
    string? Website,
    string? Industry,
    DateOnly? IncorporationDate,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? State,
    string? PinCode,
    Guid? FilingAddressWorkLocationId);
