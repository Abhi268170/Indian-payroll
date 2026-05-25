namespace Payroll.Domain.Statutory;

/// Reference data shared between the tenant provisioner (which seeds these states)
/// and downstream consumers (onboarding readiness, engine input builders) so a
/// single source of truth answers "does LWF apply in this state?". When the
/// provisioner adds a new LWF state, update this list — and the readiness check
/// will start enforcing the new state automatically.
public static class StatutoryReference
{
    /// States where LWF is a regulated deduction and the tenant provisioner
    /// inserts an LwfStateConfig row. Used by onboarding readiness to flag
    /// drift — if a tenant has a work location in one of these states but the
    /// expected config row is missing, surface it as incomplete rather than
    /// silently passing.
    public static readonly IReadOnlySet<string> LwfApplicableStates = new HashSet<string>(
        StringComparer.OrdinalIgnoreCase)
    {
        "MH", // Maharashtra
        "KA", // Karnataka
        "AP", // Andhra Pradesh
        "TS", // Telangana
        "WB", // West Bengal
        "GJ", // Gujarat
        "MP", // Madhya Pradesh
        "CH", // Chandigarh
        "HR", // Haryana
        "KL", // Kerala
    };
}
