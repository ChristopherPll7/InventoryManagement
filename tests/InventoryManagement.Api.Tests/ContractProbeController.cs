using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.Api.Tests;

[ApiController]
[Route("contract-probe")]
public sealed class ContractProbeController : ControllerBase
{
    [HttpGet("forbidden")]
    [Authorize(Roles = "inventory-admin")]
    public IActionResult ForbiddenProbe() => Ok();

    [HttpGet("failure")]
    public IActionResult FailureProbe() => throw new InvalidOperationException("Internal details must not be returned.");
}
