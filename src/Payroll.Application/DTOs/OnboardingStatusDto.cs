namespace Payroll.Application.DTOs;

public sealed record OnboardingStepDto(
    string Id,
    bool Complete,
    bool Required,
    bool Skippable,
    IReadOnlyDictionary<string, object>? Details = null);

public sealed record NavGateDto(bool Enabled, IReadOnlyList<string> Missing);

public sealed record OnboardingStatusDto(
    bool SetupComplete,
    IReadOnlyList<OnboardingStepDto> Steps,
    IReadOnlyDictionary<string, NavGateDto> NavGates);

public sealed record PreflightBlockerDto(string Code, string Message, string FixUrl, int? Count = null);

public sealed record PreflightWarningDto(string Code, string Message, string FixUrl);

public sealed record PayrollRunPreflightDto(
    bool Ready,
    IReadOnlyList<PreflightBlockerDto> Blockers,
    IReadOnlyList<PreflightWarningDto> Warnings);
