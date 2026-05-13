using Microsoft.AspNetCore.Identity;

namespace Payroll.Infrastructure.Persistence;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public Guid? TenantId { get; set; }
    public Guid? EmployeeId { get; set; }
    public bool IsSuperAdmin { get; set; }
}

public sealed class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole() { }
    public ApplicationRole(string roleName) : base(roleName) { }
}
