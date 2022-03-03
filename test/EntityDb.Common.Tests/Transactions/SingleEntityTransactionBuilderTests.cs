﻿using EntityDb.Abstractions.Entities;
using EntityDb.Abstractions.Transactions.Steps;
using EntityDb.Common.Exceptions;
using EntityDb.Common.Leases;
using EntityDb.Common.Tests.Implementations.Commands;
using EntityDb.Common.Tests.Implementations.Entities;
using EntityDb.Common.Transactions.Builders;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EntityDb.Abstractions.ValueObjects;
using Xunit;

namespace EntityDb.Common.Tests.Transactions;

public class SingleEntityTransactionBuilderTests : TestsBase<Startup>
{
    public SingleEntityTransactionBuilderTests(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    [Fact]
    public void GivenEntityNotKnown_WhenGettingEntity_ThenThrow()
    {
        // ARRANGE

        using var serviceScope = CreateServiceScope();

        var transactionBuilder = serviceScope.ServiceProvider
            .GetRequiredService<TransactionBuilder<TransactionEntity>>()
            .ForSingleEntity(default);

        // ASSERT

        transactionBuilder.IsEntityKnown().ShouldBeFalse();

        Should.Throw<KeyNotFoundException>(() => transactionBuilder.GetEntity());
    }

    [Fact]
    public void GivenEntityKnown_WhenGettingEntity_ThenReturnExpectedEntity()
    {
        // ARRANGE

        using var serviceScope = CreateServiceScope();

        var expectedEntityId = Id.NewId();

        var expectedEntity = TransactionEntity
            .Construct(expectedEntityId);

        var transactionBuilder = serviceScope.ServiceProvider
            .GetRequiredService<TransactionBuilder<TransactionEntity>>()
            .ForSingleEntity(expectedEntityId);

        transactionBuilder.Load(expectedEntity);

        // ARRANGE ASSERTIONS

        transactionBuilder.IsEntityKnown().ShouldBeTrue();

        // ACT

        var actualEntityId = transactionBuilder.EntityId;
        var actualEntity = transactionBuilder.GetEntity();

        // ASSERT

        actualEntityId.ShouldBe(expectedEntityId);
        actualEntity.ShouldBe(expectedEntity);
    }

    [Fact]
    public void GivenLeasingStrategy_WhenBuildingNewEntityWithLease_ThenTransactionDoesInsertLeases()
    {
        // ARRANGE

        using var serviceScope = CreateServiceScope();

        var transactionBuilder = serviceScope.ServiceProvider
            .GetRequiredService<TransactionBuilder<TransactionEntity>>()
            .ForSingleEntity(default);

        // ACT

        var transaction = transactionBuilder
            .Add(new Lease(default!, default!, default!))
            .Build(default!, default);

        // ASSERT

        transaction.Steps.Length.ShouldBe(1);
            
        var leaseTransactionStep = transaction.Steps[0].ShouldBeAssignableTo<IAddLeasesTransactionStep>()!;

        leaseTransactionStep.Leases.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task GivenExistingEntityId_WhenUsingEntityIdForLoadTwice_ThenLoadThrows()
    {
        // ARRANGE

        var entityId = Id.NewId();

        using var serviceScope = CreateServiceScope(serviceCollection =>
        {
            serviceCollection.AddScoped(_ =>
                GetMockedTransactionRepositoryFactory(
                    new object[] { new DoNothing() }));
        });

        var transactionBuilder = serviceScope.ServiceProvider
            .GetRequiredService<TransactionBuilder<TransactionEntity>>()
            .ForSingleEntity(entityId);

        await using var entityRepository = await serviceScope.ServiceProvider
            .GetRequiredService<IEntityRepositoryFactory<TransactionEntity>>()
            .CreateRepository(default!);

        var entity = await entityRepository.GetCurrent(entityId);

        // ACT

        transactionBuilder.Load(entity);

        // ASSERT

        Should.Throw<EntityAlreadyKnownException>(() =>
        {
            transactionBuilder.Load(entity);
        });
    }

    [Fact]
    public void GivenNonExistingEntityId_WhenUsingValidVersioningStrategy_ThenVersionNumberAutoIncrements()
    {
        // ARRANGE

        var numberOfVersionsToTest = new VersionNumber(10);

        var entityId = Id.NewId();

        using var serviceScope = CreateServiceScope(serviceCollection =>
        {
            serviceCollection.AddScoped(_ =>
                GetMockedTransactionRepositoryFactory());
        });

        var transactionBuilder = serviceScope.ServiceProvider
            .GetRequiredService<TransactionBuilder<TransactionEntity>>()
            .ForSingleEntity(entityId);

        // ACT

        for (var i = new VersionNumber(1); i.Value <= numberOfVersionsToTest.Value; i = i.Next())
        {
            transactionBuilder.Append(new DoNothing());
        }

        var transaction = transactionBuilder.Build(default!, default);

        // ASSERT

        for (var v = new VersionNumber(1); v.Value <= numberOfVersionsToTest.Value; v = v.Next())
        {
            var index = (int)(v.Value - 1);

            var commandTransactionStep = transaction.Steps[index].ShouldBeAssignableTo<IAppendCommandTransactionStep>()!;

            commandTransactionStep.EntityVersionNumber.ShouldBe(v);
        }
    }

    [Fact]
    public async Task GivenExistingEntity_WhenAppendingNewCommand_ThenTransactionBuilds()
    {
        // ARRANGE

        var entityId = Id.NewId();

        using var serviceScope = CreateServiceScope(serviceCollection =>
        {
            serviceCollection.AddScoped(_ =>
                GetMockedTransactionRepositoryFactory(new object[] { new DoNothing() }));
        });

        var transactionBuilder = serviceScope.ServiceProvider
            .GetRequiredService<TransactionBuilder<TransactionEntity>>()
            .ForSingleEntity(entityId);

        await using var entityRepository = await serviceScope.ServiceProvider
            .GetRequiredService<IEntityRepositoryFactory<TransactionEntity>>()
            .CreateRepository(default!);

        var entity = await entityRepository.GetCurrent(entityId);

        // ACT

        var transaction = transactionBuilder
            .Load(entity)
            .Append(new DoNothing())
            .Build(default!, default);

        // ASSERT

        transaction.Steps.Length.ShouldBe(1);

        var commandTransactionStep = transaction.Steps[0].ShouldBeAssignableTo<IAppendCommandTransactionStep>()!;

        commandTransactionStep.Command.ShouldBeEquivalentTo(new DoNothing());
    }
}