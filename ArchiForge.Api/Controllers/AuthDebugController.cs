using ArchiForge.Api.Auth.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArchiForge.Api.Controllers;

[Authorize(Policy = ArchiForgePolicies.ReadAuthority)]
[ApiController]
[Route("api/auth")]
public sealed class AuthDebugController : ControllerBase
{
    [HttpGet("me")]
    public IActionResult Me()
    {
        return Ok(new
        {
            User.Identity?.Name,
            Claims = User.Claims.Select(x => new
            {
                x.Type,
                x.Value
            }).ToList()
        });
    }
}
