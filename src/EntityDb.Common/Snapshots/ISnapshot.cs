using EntityDb.Abstractions.ValueObjects;

namespace EntityDb.Common.Snapshots;

/// <summary>
///     Indicates that the snapshot is compatible with several EntityDb.Common implementations.
/// </summary>
/// <typeparam name="TSnapshot">The type of the snapshot.</typeparam>
public interface ISnapshot<TSnapshot>
{
    /// <summary>
    ///     Creates a new instance of a <typeparamref name="TSnapshot" />.
    /// </summary>
    /// <param name="snapshotId">The id of the snapshot.</param>
    /// <returns>A new instance of <typeparamref name="TSnapshot" />.</returns>
    static abstract TSnapshot Construct(Id snapshotId);

    /// <summary>
    ///     Creates a copy of a <typeparamref name="TSnapshot" />
    /// </summary>
    /// <returns>A copy of a <typeparamref name="TSnapshot" /></returns>
    TSnapshot Copy();

    /// <summary>
    ///     Returns the id of this snapshot.
    /// </summary>
    /// <returns>The id of this snapshot.</returns>
    Id GetId();

    /// <summary>
    ///     Returns the version number of this snapshot.
    /// </summary>
    /// <returns>The version number of this snapshot.</returns>
    VersionNumber GetVersionNumber();

    /// <summary>
    ///     Indicates if this snapshot instance version should be recorded (independent of the latest snapshot).
    /// </summary>
    /// <returns><c>true</c> if this snapshot instance should be recorded, or else <c>false</c>.</returns>
    /// <remarks>
    ///     You would use this if you intent to fetch a snapshot at multiple version numbers and don't want to hit
    ///     the transaction database when it can be avoided.
    /// </remarks>
    bool ShouldRecord();

    /// <summary>
    ///     Indicates if this snapshot instance should be recorded as the latest snapshot.
    /// </summary>
    /// <param name="previousLatestSnapshot">The previous instance of the latest snapshot.</param>
    /// <returns><c>true</c> if this snapshot instance should be recorded as the latest snapshot, or else <c>false</c>.</returns>
    bool ShouldRecordAsLatest(TSnapshot? previousLatestSnapshot);
}
