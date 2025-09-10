using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

using Mma.Data.Services;

namespace Mma.Data.Interceptors;

public class SoftDeleteInterceptor : SaveChangesInterceptor, IInterceptor
{
    private readonly CurrentUserService _currentUserService;

    public SoftDeleteInterceptor(CurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
            return new ValueTask<InterceptionResult<int>>(result);

        foreach (var entry in eventData.Context.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged))
        {
            switch (entry)
            {
                case { State: EntityState.Deleted, Entity: IAuditEntity delete }:
                    entry.State = EntityState.Modified;
                    delete.IsDeleted = true;
                    delete.DeletedDate = DateTime.UtcNow;
                    delete.DeletedBy = _currentUserService.GetCurrentUser();
                    break;
                case { State: EntityState.Modified, Entity: IAuditEntity { IsDeleted: true } update }:
                    update.IsDeleted = false;
                    update.DeletedDate = null;
                    update.ModifiedDate = DateTime.UtcNow;
                    update.ModifiedBy = _currentUserService.GetCurrentUser();
                    break;
            }
        }

        return new ValueTask<InterceptionResult<int>>(result);
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is null)
            return result;

        foreach (var entry in eventData.Context.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged))
        {
            switch (entry)
            {
                case { State: EntityState.Deleted, Entity: IAuditEntity delete }:
                    entry.State = EntityState.Modified;
                    delete.IsDeleted = true;
                    delete.DeletedDate = DateTime.UtcNow;
                    delete.DeletedBy = _currentUserService.GetCurrentUser();
                    break;
                case { State: EntityState.Modified, Entity: IAuditEntity { IsDeleted: true } update }:
                    update.IsDeleted = false;
                    update.DeletedDate = null;
                    update.ModifiedBy = _currentUserService.GetCurrentUser();
                    break;
            }
        }

        return result;

    }
}