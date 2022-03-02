﻿using EntityDb.Common.Tests;
using EntityDb.Common.Tests.Implementations.Entities;
using EntityDb.MongoDb.Provisioner.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace EntityDb.MongoDb.Tests;

public class Startup : StartupBase
{
    public override void AddServices(IServiceCollection serviceCollection)
    {
        base.AddServices(serviceCollection);

        // Transactions

        serviceCollection.AddAutoProvisionTestModeMongoDbTransactions
        (
            TransactionEntity.MongoCollectionName,
            _ => "mongodb://127.0.0.1:27017/?connect=direct&replicaSet=entitydb",
            true
        );
    }
}