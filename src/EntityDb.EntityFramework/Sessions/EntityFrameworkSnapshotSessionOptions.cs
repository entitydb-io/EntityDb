using EntityDb.Abstractions.Snapshots;
using EntityDb.EntityFramework.Snapshots;
using System.Diagnostics.CodeAnalysis;

namespace EntityDb.EntityFramework.Sessions;

/// <summary>
///     Configuration options for the EntityFramework implementation of <see cref="ISnapshotRepository{TSnapshot}"/>.
/// </summary>
public sealed class EntityFrameworkSnapshotSessionOptions
{
    /// <summary>
    ///     This property is not used by the package. It only provides a convenient way to access
    ///     the connection string using IOptions, which does not appear to be a convenient thing
    ///     to do in vanilla Entity Framework.
    /// </summary>
    public string ConnectionString { get; set; } = default!;

    /// <summary>
    ///     If <c>true</c>, indicates the agent only intends to execute queries.
    /// </summary>
    public bool ReadOnly { get; set; }

    /// <summary>
    ///     If <c>false</c>, a snapshot will be deleted if there are no <see cref="SnapshotReference{TSnapshot}"/>
    ///     records pointing to the snapshot record.
    /// </summary>
    /// <remarks>
    ///     You may consider setting this to <c>true</c> if there are other records which reference a specific snapshot.
    /// </remarks>
    public bool KeepSnapshotsWithoutSnapshotReferences { get; set; }

    /// <inheritdoc/>
    [ExcludeFromCodeCoverage(Justification = "This is only overridden to make test names better.")]
    public override string ToString()
    {
        return $"{nameof(EntityFrameworkSnapshotSessionOptions)}";
    }
}
