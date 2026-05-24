using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Application.Commands.Statutory;
using Payroll.Application.DTOs;
using Payroll.Application.Queries.Statutory;
using Payroll.Domain.Common;

namespace Payroll.Api.Controllers;

[ApiController]
[Route("api/v1/statutory")]
[Authorize]
public sealed class StatutoryComponentsController(ISender sender) : ControllerBase
{
    [HttpGet("config")]
    public async Task<IActionResult> GetConfig(CancellationToken ct)
    {
        StatutoryConfigDto? config = await sender.Send(new GetStatutoryConfigQuery(), ct);
        // After Phase A every tenant — new and backfilled — has a real StatutoryOrgConfig row.
        // Returning the old 200 fallback DTO would mask a genuine misconfiguration; surface as 404.
        if (config is null)
            return NotFound(new { error = "Statutory configuration not found for this tenant. Re-run tenant provisioning or contact support." });
        return Ok(config);
    }

    [HttpPut("epf")]
    [Authorize(Policy = "OrgAdmin")]
    public async Task<IActionResult> ConfigureEpf(
        [FromBody] ConfigureEpfRequest request, CancellationToken ct)
    {
        try
        {
            await sender.Send(new ConfigureEpfCommand(
                request.Enabled,
                request.EstablishmentCode,
                request.EmployeeContributionRate,
                request.EmployerContributionRate,
                request.IncludeEmployerInCtc,
                request.OverrideAtEmployeeLevel,
                request.ProRateRestrictedPfWage,
                request.ConsiderSalaryOnLop,
                GetActorId()), ct);
            return NoContent();
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
    }

    [HttpPut("esi")]
    [Authorize(Policy = "OrgAdmin")]
    public async Task<IActionResult> ConfigureEsi(
        [FromBody] ConfigureEsiRequest request, CancellationToken ct)
    {
        try
        {
            await sender.Send(new ConfigureEsiCommand(
                request.Enabled,
                request.EstablishmentCode,
                request.NotifiedArea,
                GetActorId()), ct);
            return NoContent();
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
    }

    [HttpPut("statutory-bonus")]
    [Authorize(Policy = "OrgAdmin")]
    public async Task<IActionResult> ConfigureStatutoryBonus(
        [FromBody] ConfigureStatutoryBonusRequest request, CancellationToken ct)
    {
        await sender.Send(new ConfigureStatutoryBonusCommand(
            request.Enabled,
            request.BonusRate,
            request.BonusMode,
            request.BonusPayoutMonth,
            GetActorId()), ct);
        return NoContent();
    }

    [HttpGet("pt-slabs/{stateCode}")]
    public async Task<IActionResult> GetPtSlabs(
        string stateCode, [FromQuery] DateOnly? asOf, CancellationToken ct)
    {
        IReadOnlyList<PtSlabDto> slabs = await sender.Send(
            new GetPtSlabsQuery(stateCode, asOf ?? DateOnly.FromDateTime(DateTime.UtcNow)), ct);
        return Ok(slabs);
    }

    [HttpPost("pt-slabs/{stateCode}")]
    [Authorize(Policy = "OrgAdmin")]
    public async Task<IActionResult> RevisePtSlabs(
        string stateCode, [FromBody] RevisePtSlabsRequest request, CancellationToken ct)
    {
        try
        {
            await sender.Send(new ReviseStatePtSlabsCommand(
                stateCode,
                request.EffectiveDate,
                request.Frequency,
                request.DeductionMonthsCsv,
                request.Slabs.Select(s => new PtSlabInput(s.MinGross, s.MaxGross, s.PtAmount, s.Gender, s.IsFebruarySurcharge)).ToList(),
                GetActorId()), ct);
            return NoContent();
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
    }

    [HttpGet("lwf-configs")]
    public async Task<IActionResult> GetLwfConfigs(
        [FromQuery] string[] states, CancellationToken ct)
    {
        IReadOnlyList<LwfStateConfigDto> configs = await sender.Send(new GetLwfConfigsQuery(states), ct);
        return Ok(configs);
    }

    [HttpPut("lwf-configs/{stateCode}/toggle")]
    [Authorize(Policy = "OrgAdmin")]
    public async Task<IActionResult> ToggleLwfState(
        string stateCode, [FromBody] ToggleLwfRequest request, CancellationToken ct)
    {
        try
        {
            await sender.Send(new ToggleLwfStateCommand(stateCode, request.Enable, GetActorId()), ct);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpGet("pt-registrations")]
    public async Task<IActionResult> GetPtRegistrations(CancellationToken ct)
    {
        IReadOnlyList<PtRegistrationDto> registrations = await sender.Send(new GetPtRegistrationsQuery(), ct);
        return Ok(registrations);
    }

    [HttpPut("pt-registrations/{stateCode}")]
    [Authorize(Policy = "OrgAdmin")]
    public async Task<IActionResult> UpsertPtRegistration(
        string stateCode, [FromBody] UpsertPtRegistrationRequest request, CancellationToken ct)
    {
        await sender.Send(new UpsertPtRegistrationCommand(stateCode, request.RegistrationNumber, GetActorId()), ct);
        return NoContent();
    }

    private Guid GetActorId()
    {
        string? sub = User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out Guid id) ? id : Guid.Empty;
    }
}

public record ConfigureEpfRequest(
    bool Enabled,
    string? EstablishmentCode,
    string EmployeeContributionRate,
    string EmployerContributionRate,
    bool IncludeEmployerInCtc,
    bool OverrideAtEmployeeLevel,
    bool ProRateRestrictedPfWage,
    bool ConsiderSalaryOnLop);

public record ConfigureEsiRequest(
    bool Enabled,
    string? EstablishmentCode,
    bool NotifiedArea);

public record ConfigureStatutoryBonusRequest(bool Enabled, decimal BonusRate, string BonusMode, int? BonusPayoutMonth);

public record ToggleLwfRequest(bool Enable);

public record UpsertPtRegistrationRequest(string RegistrationNumber);

public record RevisePtSlabsRequest(
    DateOnly EffectiveDate,
    string Frequency,
    string? DeductionMonthsCsv,
    IReadOnlyList<PtSlabRowRequest> Slabs);

public record PtSlabRowRequest(
    decimal MinGross,
    decimal? MaxGross,
    decimal PtAmount,
    string? Gender,
    bool IsFebruarySurcharge);
