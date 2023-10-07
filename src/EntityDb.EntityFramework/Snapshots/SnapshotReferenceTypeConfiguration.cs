using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EntityDb.EntityFramework.Snapshots;

/// <inheritdoc />
public class SnapshotReferenceTypeConfiguration<TSnapshot> : IEntityTypeConfiguration<SnapshotReference<TSnapshot>>
    where TSnapshot : class
{
    private readonly string _snapshotReferencesTableName;

    /// <summary>
    ///     Configure the snapshot Reference Type.
    /// </summary>
    /// <param name="snapshotReferencesTableName">The name of the table for snapshot references.</param>
    public SnapshotReferenceTypeConfiguration(string snapshotReferencesTableName)
    {
        _snapshotReferencesTableName = snapshotReferencesTableName;
    }

    /// <inheritdoc />
    public virtual void Configure(EntityTypeBuilder<SnapshotReference<TSnapshot>> snapshotReferenceBuilder)
    {
        snapshotReferenceBuilder.ToTable(_snapshotReferencesTableName);

        snapshotReferenceBuilder
            .HasKey(snapshotReference => snapshotReference.Id);

        snapshotReferenceBuilder
            .HasAlternateKey(snapshotReference => new
            {
                snapshotReference.PointerId,
                snapshotReference.PointerVersionNumber
            });

        snapshotReferenceBuilder
            .HasOne(snapshotReference => snapshotReference.Snapshot)
            .WithMany()
            .HasForeignKey(snapshotReference => new
            {
                snapshotReference.SnapshotId,
                snapshotReference.SnapshotVersionNumber,
            })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
