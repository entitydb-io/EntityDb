using System;
using System.Threading;
using EntityDb.Abstractions.ValueObjects;
using EntityDb.Common.Snapshots;

namespace EntityDb.Common.Tests.Implementations.Snapshots;

public interface ISnapshotWithTestMethods<TSnapshot> : ISnapshot<TSnapshot>
{
    TSnapshot WithVersionNumber(VersionNumber versionNumber);
    static abstract AsyncLocal<Func<TSnapshot, bool>?> ShouldRecordLogic { get; }
    static abstract AsyncLocal<Func<TSnapshot, TSnapshot?, bool>?> ShouldRecordAsMostRecentLogic { get; }
}