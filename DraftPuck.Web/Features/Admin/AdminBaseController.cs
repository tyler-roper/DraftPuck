using Microsoft.AspNetCore.Authorization;

namespace DraftPuck.Web.Features.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/[controller]")]
public abstract class AdminBaseController() : ControllerBase {}