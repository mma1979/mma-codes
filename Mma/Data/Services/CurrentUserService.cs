using Microsoft.AspNetCore.Http;
namespace Mma.Data.Services;

public class CurrentUserService
{
    private readonly IHttpContextAccessor _contextAccessor;
    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _contextAccessor = httpContextAccessor;
    }
    public Guid? GetCurrentUser()
    {
        var user = _contextAccessor.HttpContext?.User;
        var idClaim = user?.FindFirst("Id");
        if (idClaim != null && Guid.TryParse(idClaim.Value, out Guid userId))
        {
            return userId;
        }
        return null;
    }
}