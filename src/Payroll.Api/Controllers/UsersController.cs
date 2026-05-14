using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Application.Commands.Users;
using Payroll.Domain.Common;

namespace Payroll.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Policy = "OrgAdmin")]
public sealed class UsersController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            Guid actorId = GetActorId();
            Guid id = await sender.Send(
                new CreateUserCommand(request.Email, request.Password, request.Role, actorId),
                cancellationToken);
            return Created($"/api/users/{id}", new { id });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private Guid GetActorId()
    {
        string? sub = User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out Guid id) ? id : Guid.Empty;
    }
}

public record CreateUserRequest(string Email, string Password, string Role);
