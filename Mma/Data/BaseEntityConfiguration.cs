using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Mma.Data.ValueGenerator;
using Mma.Helpers;

namespace Mma.Data;

public class BaseEntityConfiguration<TEntity,TID>: IEntityTypeConfiguration<TEntity> where TEntity : BaseEntity<TID>
{
    private readonly string _schema;
    public BaseEntityConfiguration(string schema = "dbo")
    {
        _schema = schema;
    }
    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.ToTable(BuildHelper.GetSetName(typeof(TEntity).Name), _schema);

        if (typeof(TID) == typeof(Guid))
        {
            builder.Property(e => e.Id)
             .ValueGeneratedOnAdd()
             .HasValueGenerator<GuidV7ValueGenerator>();
        }

        builder.HasQueryFilter(e => e.IsDeleted != true);
        builder.Property(e => e.IsDeleted).IsRequired()
            .HasDefaultValueSql("((0))");

        builder.Property(e => e.CreatedDate)
            .HasColumnType("datetime")
            .ValueGeneratedOnAdd()
            .HasValueGenerator<CreatedDateTimeValueGenerator>();

        builder.Property(e => e.ModifiedDate)
             .HasColumnType("datetime");


        builder.HasIndex(e => e.IsDeleted);
        builder.Property(e => e.DeletedDate).HasColumnType("datetime");
    }
}
