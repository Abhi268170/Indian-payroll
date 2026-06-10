using Payroll.Domain.Enums;

namespace Payroll.Application.Services;

// PT slabs in gender-split states (e.g. Maharashtra) match on "Male"/"Female".
// Gender.Other maps to null so the engine falls back to the broader Male schedule.
public static class EngineGenderMapper
{
    public static string? ToEngineGender(Gender gender) => gender switch
    {
        Gender.Male => "Male",
        Gender.Female => "Female",
        _ => null,
    };
}
