﻿using EntityDb.Abstractions.Entities;
using EntityDb.Abstractions.Strategies;
using EntityDb.Abstractions.Transactions;
using EntityDb.Common.Extensions;
using EntityDb.Common.Tests.Implementations.Commands;
using EntityDb.Common.Tests.Implementations.Entities;
using EntityDb.Common.Transactions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Shouldly;
using System;
using System.Threading.Tasks;
using Xunit;

namespace EntityDb.Common.Tests.Entities
{
    public abstract class EntityTestsBase<TStartup> : TestsBase<TStartup>
        where TStartup : IStartup, new()
    {
        protected EntityTestsBase(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        private static ITransaction<TransactionEntity> BuildTransaction
        (
            IServiceScope serviceScope,
            Guid entityId,
            ulong from,
            ulong to,
            TransactionEntity? entity = null
        )
        {
            var transactionBuilder = serviceScope.ServiceProvider
                .GetRequiredService<TransactionBuilder<TransactionEntity>>()
                .ForSingleEntity(entityId);

            if (entity != null)
            {
                transactionBuilder.Load(entity);
            }
            else
            {
                transactionBuilder.Create(new DoNothing());
            }

            for (var i = from; i < to; i++)
            {
                transactionBuilder.Append(new DoNothing());
            }

            return transactionBuilder.Build(default!, Guid.NewGuid());
        }

        [Theory]
        [InlineData(10, 20)]
        public async Task
            GivenSnapshottingOnNthVersion_WhenPuttingTransactionWithNthVersion_ThenSnapshotExistsAtNthVersion(
                ulong expectedSnapshotVersion, ulong expectedCurrentVersion)
        {
            // ARRANGE 1

            var snapshottingStrategyMock = new Mock<ISnapshottingStrategy<TransactionEntity>>(MockBehavior.Strict);

            snapshottingStrategyMock
                .Setup(strategy =>
                    strategy.ShouldPutSnapshot(It.IsAny<TransactionEntity?>(), It.IsAny<TransactionEntity>()))
                .Returns((TransactionEntity? _, TransactionEntity nextEntity) =>
                    nextEntity.VersionNumber == expectedSnapshotVersion);

            using var serviceScope = CreateServiceScope(serviceCollection =>
            {
                serviceCollection.RemoveAll(typeof(ISnapshottingStrategy<TransactionEntity>));

                serviceCollection.AddSingleton(_ => snapshottingStrategyMock.Object);
            });

            var entityId = Guid.NewGuid();

            await using var entityRepository = await serviceScope.ServiceProvider
                .GetRequiredService<IEntityRepositoryFactory<TransactionEntity>>()
                    .CreateRepository(TestSessionOptions.Write,
                        TestSessionOptions.Write);

            var firstTransaction = BuildTransaction(serviceScope, entityId, 1, expectedSnapshotVersion);

            var firstTransactionInserted = await entityRepository.PutTransaction(firstTransaction);

            // ARRANGE 1 ASSERTIONS

            firstTransactionInserted.ShouldBeTrue();

            // ARRANGE 2

            var entity = await entityRepository.GetCurrent(entityId);

            var secondTransaction = BuildTransaction(serviceScope, entityId, expectedSnapshotVersion,
                expectedCurrentVersion, entity);

            var secondTransactionInserted = await entityRepository.PutTransaction(secondTransaction);

            // ARRANGE 2 ASSERTIONS

            secondTransactionInserted.ShouldBeTrue();

            // ACT

            var current = await entityRepository.GetCurrent(entityId);

            var snapshot = await entityRepository.SnapshotRepository.GetSnapshotOrDefault(entityId);

            // ASSERT

            snapshot.ShouldNotBeNull();
            snapshot.VersionNumber.ShouldBe(expectedSnapshotVersion);
            current.VersionNumber.ShouldBe(expectedCurrentVersion);
        }

        [Theory]
        [InlineData(10, 5)]
        public async Task GivenEntityWithNVersions_WhenGettingAtVersionM_ThenReturnAtVersionM(ulong versionNumberN, ulong versionNumberM)
        {
            // ARRANGE

            using var serviceScope = CreateServiceScope();

            var entityId = Guid.NewGuid();

            await using var entityRepository = await serviceScope.ServiceProvider
                .GetRequiredService<IEntityRepositoryFactory<TransactionEntity>>()
                    .CreateRepository(TestSessionOptions.Write,
                        TestSessionOptions.Write);

            var transaction = BuildTransaction(serviceScope, entityId, 1, versionNumberN);

            var transactionInserted = await entityRepository.PutTransaction(transaction);

            // ARRANGE ASSERTIONS

            transactionInserted.ShouldBeTrue();

            // ACT

            var entityAtVersionM = await entityRepository.GetAtVersion(entityId, versionNumberM);

            // ASSERT

            entityAtVersionM.VersionNumber.ShouldBe(versionNumberM);
        }
    }
}
