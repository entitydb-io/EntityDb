﻿using EntityDb.Common.Entities;
using System;
using EntityDb.Abstractions.Reducers;
using EntityDb.Abstractions.ValueObjects;
using EntityDb.Common.Snapshots;

namespace EntityDb.Common.Tests.Implementations.Entities;

public record TransactionEntity
(
    VersionNumber VersionNumber = default
)
: IEntity<TransactionEntity>, ISnapshot<TransactionEntity>
{
    public const string MongoCollectionName = "Test";
    public const string RedisKeyNamespace = "test";

    public static TransactionEntity Construct(Id entityId)
    {
        return new TransactionEntity();
    }

    public VersionNumber GetVersionNumber()
    {
        return VersionNumber;
    }

    public TransactionEntity Reduce(object[] commands)
    {
        var newEntity = this;

        foreach (var command in commands)
        {
            if (command is not IReducer<TransactionEntity> reducer)
            {
                throw new NotImplementedException();
            }
            
            newEntity = reducer.Reduce(newEntity);
        }

        return newEntity;
    }
    
    public bool ShouldReplace(TransactionEntity? previousSnapshot)
    {
        return true;
    }
}