namespace Payroll.Application.Interfaces;

// Marker interface for commands that execute in the platform context (SuperAdmin only),
// where ITenantContext is intentionally unresolved. Must reside in
// Payroll.Application.Commands.Platform — enforced by NetArchTest in ArchitectureTests.
public interface IAllowWithoutTenant;
