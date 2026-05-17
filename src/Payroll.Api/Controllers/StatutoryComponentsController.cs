using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Application.Commands.Statutory;
using Payroll.Application.DTOs;
using Payroll.Application.Queries.Statutory;

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
        if (config is null)
            return Ok(new StatutoryConfigDto(
                false, null, "ActualPfWage12", "ActualPfWage12",
                true, false, false, false, false, true,
                false, null, true,
                false, 8.33m, "Yearly", null));
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
                request.IncludeEdliInCtc,
                request.IncludeAdminInCtc,
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
    bool IncludeEdliInCtc,
    bool IncludeAdminInCtc,
    bool OverrideAtEmployeeLevel,
    bool ProRateRestrictedPfWage,
    bool ConsiderSalaryOnLop);

public record ConfigureEsiRequest(
    bool Enabled,
    string? EstablishmentCode,
    bool NotifiedArea);

public record ConfigureStatutoryBonusRequest(bool Enabled, decimal BonusRate, string BonusMode, int? BonusPayoutMonth);

public record RevisePtSlabsRequest(
    DateOnly EffectiveDate,
    string Frequency,
    IReadOnlyList<PtSlabRowRequest> Slabs);

public record PtSlabRowRequest(
    decimal MinGross,
    decimal? MaxGross,
    decimal PtAmount,
    string? Gender,
    bool IsFebruarySurcharge);
