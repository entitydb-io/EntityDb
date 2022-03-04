﻿using EntityDb.Abstractions.Snapshots;
using EntityDb.Abstractions.ValueObjects;
using EntityDb.Common.Disposables;
using EntityDb.Common.Envelopes;
using EntityDb.Common.Extensions;
using EntityDb.Redis.Sessions;
using StackExchange.Redis;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace EntityDb.Redis.Snapshots;

internal class RedisSnapshotRepository<TSnapshot> : DisposableResourceBaseClass, ISnapshotRepository<TSnapshot>
{
    private readonly IEnvelopeService<JsonElement> _envelopeService;
    private readonly string _keyNamespace;
    private readonly IRedisSession _redisSession;

    public RedisSnapshotRepository
    (
        IEnvelopeService<JsonElement> envelopeService,
        string keyNamespace,
        IRedisSession redisSession
    )
    {
        _envelopeService = envelopeService;
        _keyNamespace = keyNamespace;
        _redisSession = redisSession;
    }

    private RedisKey GetSnapshotKey(Id snapshotId)
    {
        return $"{_keyNamespace}#{snapshotId}";
    }

    public async Task<bool> PutSnapshot(Id snapshotId, TSnapshot snapshot)
    {
        var snapshotKey = GetSnapshotKey(snapshotId);

        var snapshotValue = _envelopeService
            .DeconstructAndSerialize(snapshot);

        return await _redisSession.Insert(snapshotKey, snapshotValue);
    }

    public async Task<TSnapshot?> GetSnapshot(Id snapshotId)
    {
        var snapshotKey = GetSnapshotKey(snapshotId);
        var snapshotValue = await _redisSession.Find(snapshotKey);

        if (!snapshotValue.HasValue)
        {
            return default;
        }

        return _envelopeService
            .DeserializeAndReconstruct<JsonElement, TSnapshot>(snapshotValue);
    }

    public async Task<bool> DeleteSnapshots(Id[] snapshotIds)
    {
        var snapshotKeys = snapshotIds.Select(GetSnapshotKey);

        return await _redisSession.Delete(snapshotKeys);
    }

    public override async ValueTask DisposeAsync()
    {
        await _redisSession.DisposeAsync();
    }
}
