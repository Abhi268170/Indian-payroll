using OpenIddict.Abstractions;
using Payroll.Domain.Interfaces;

namespace Payroll.Infrastructure.Services;

internal sealed class TokenRevocationService(IOpenIddictTokenManager tokenManager) : ITokenRevocationService
{
    public async Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await foreach (object token in tokenManager.FindBySubjectAsync(userId.ToString(), cancellationToken))
        {
            await tokenManager.TryRevokeAsync(token, cancellationToken);
        }
    }
}
