﻿using EntityDb.Common.Exceptions;
using EntityDb.Common.Tests;
using EntityDb.Redis.Sessions;
using Shouldly;
using System;
using System.Threading.Tasks;
using EntityDb.Common.Snapshots;
using Xunit;

namespace EntityDb.Redis.Tests.Sessions;

public class RedisSessionTests : TestsBase<Startup>
{
    public RedisSessionTests(IServiceProvider startupServiceProvider) : base(startupServiceProvider)
    {
    }

    [Fact]
    public async Task WhenExecutingWriteMethods_ThenThrow()
    {
        // ARRANGE

        var readOnlyRedisSession = new RedisSession(default!, default!, new SnapshotSessionOptions
        {
            ReadOnly = true
        });

        // ASSERT

        await Should.ThrowAsync<CannotWriteInReadOnlyModeException>(() => readOnlyRedisSession.Insert(default!, default!));

        await Should.ThrowAsync<CannotWriteInReadOnlyModeException>(() => readOnlyRedisSession.Delete(default!));
    }
}