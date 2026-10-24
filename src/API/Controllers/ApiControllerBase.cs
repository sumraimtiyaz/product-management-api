using Microsoft.AspNetCore.Mvc;

namespace ProductManagement.Api.Controllers;

/// <summary>
/// Shared conventions for every controller. Deliberately has no <c>[Produces]</c> attribute: it would force
/// every response, including RFC 7807 problem responses, to <c>application/json</c>.
/// </summary>
[ApiController]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
public abstract class ApiControllerBase : ControllerBase;
