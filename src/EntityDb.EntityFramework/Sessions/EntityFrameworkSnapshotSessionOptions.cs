﻿using EntityDb.Abstractions.Snapshots;

namespace EntityDb.EntityFramework.Sessions;

/// <summary>
///     Configuration options for the Redis implementation of <see cref="ISnapshotRepository{TSnapshot}"/>.
/// </summary>
public class EntityFrameworkSnapshotSessionOptions
{
    /// <summary>
    ///     If <c>true</c>, indicates the agent only intends to execute queries.
    /// </summary>
    public bool ReadOnly { get; set; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{nameof(EntityFrameworkSnapshotSessionOptions)}";
    }
}
