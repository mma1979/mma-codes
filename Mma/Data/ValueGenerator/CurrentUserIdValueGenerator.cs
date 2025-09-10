using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Mma.Data.ValueGenerator;

public class CurrentUserIdValueGenerator(IHttpContextAccessor accessor) : ValueGenerator<Guid>
{
    private readonly IHttpContextAccessor _accessor = accessor;

    public override Guid Next(EntityEntry entry)
    {
        return Guid.Parse(CurrentUserId());
    }

    public override bool GeneratesTemporaryValues => false;

    private string CurrentUserId()
    {
        return Ulid.NewUlid().ToGuid().ToString();
    }
}