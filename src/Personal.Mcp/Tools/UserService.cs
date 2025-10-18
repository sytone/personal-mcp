using Microsoft.AspNetCore.Http;

namespace Personal.Mcp.Tools;

public class UserService(IHttpContextAccessor httpContextAccessor)
{
    public string? UserName => httpContextAccessor.HttpContext?.User.Identity?.Name;
}
